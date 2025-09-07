using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using MovieFinder.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Configuration;
using MovieFinder.Services;
using System.Net;
using System.Linq;

namespace MovieFinder.Services;

public class MovieService
{
    private readonly HttpClient _httpClient;
    private readonly string _omdbApiKey;
    private readonly string _upcItemDbApiKey;
    private readonly IAppLogger _logger;

    public MovieService(IConfiguration configuration, IAppLogger logger)
    {
        _httpClient = new HttpClient();
        _omdbApiKey = configuration["OMDBAPIKEY"] ?? "";
        _upcItemDbApiKey = configuration["UPCITEMDBAPIKEY"] ?? "";
        _logger = logger;
    }

    public async Task<Movie?> FetchMovieDetailsFromBarcode(string barcode)
    {
        // 1. Look up barcode to get title or IMDb ID
        var movieIdentifier = await GetMovieIdentifierFromUpc(barcode);

        if (movieIdentifier != null)
        {
            // 2. Fetch details from OMDb
            var movie = await GetMovieDetailsFromOmdb(movieIdentifier);
            return movie;
        }

        return null;
    }

    private async Task<JObject?> GetMovieIdentifierFromUpc(string barcode)
    {
        var cachePath = $"Cache/Barcodes/{barcode}.json";
        if (File.Exists(cachePath))
        {
            var cachedResponse = await File.ReadAllTextAsync(cachePath);
            _logger.Event($"Barcode found in cache: {barcode}.json");
            var data = JObject.Parse(cachedResponse);
            if (data?["item_attributes"]?["title"]?.ToString() is string title && !string.IsNullOrEmpty(title))
            {
                var identifier = new JObject();
                identifier["title"] = title;
                return identifier;
            }
            return null;
        }

        if (string.IsNullOrEmpty(_upcItemDbApiKey))
        {
            _logger.Warn("BarcodeSpider API key is missing.");
            return null;
        }

        var url = $"https://api.barcodespider.com/v1/lookup?token={_upcItemDbApiKey}&upc={barcode}";
        _logger.Event($"Barcode fetch submitted: {url}");

        try
        {
            var response = await _httpClient.GetStringAsync(url);
            _logger.Event($"Barcode fetch results: {response}");
            var data = JObject.Parse(response);

            if (data?["item_attributes"]?["title"]?.ToString() is string title && !string.IsNullOrEmpty(title))
            {
                Directory.CreateDirectory("Cache/Barcodes");
                await File.WriteAllTextAsync(cachePath, response);
                var identifier = new JObject();
                identifier["title"] = title;
                return identifier;
            }
        }
        catch (HttpRequestException e)
        {
            _logger.Error($"BarcodeSpider API Error: {e.Message}");
        }

        return null;
    }

    private async Task<Movie?> GetMovieDetailsFromOmdb(JObject movieIdentifier)
    {
        if (string.IsNullOrEmpty(_omdbApiKey))
        {
            _logger.Warn("OMDb API key is missing.");
            return null;
        }

        string? imdbId = movieIdentifier?["imdb_id"]?.ToString();
        string? title = movieIdentifier?["title"]?.ToString();

        if (title != null)
        {
            var blacklistedTerms = new[] { "[Double Sided]", "[Blu-ray]" };
            foreach (var term in blacklistedTerms)
            {
                title = title.Replace(term, "");
            }
            title = title.Trim();
        }

        if (imdbId != null)
        {
            var cachePath = $"Cache/OMDB/{imdbId}.json";
            if (File.Exists(cachePath))
            {
                var cachedResponse = await File.ReadAllTextAsync(cachePath);
                _logger.Event($"OMDb movie found in cache: {imdbId}.json");
                return JsonConvert.DeserializeObject<Movie>(cachedResponse);
            }
        }

        if (title != null)
        {
            _logger.Event($"Looking up title '{title}' in Cache/OMDB/files.json");
            var filesJson = await File.ReadAllTextAsync("Cache/OMDB/files.json");
            var filesData = JObject.Parse(filesJson);
            var fileEntry = filesData?["files"]?.FirstOrDefault(f => WebUtility.UrlDecode(f["t"]?.ToString()) == title);
            if (fileEntry != null)
            {
                imdbId = fileEntry["imdbId"]?.ToString();
                _logger.Event($"Found entry in files.json: imdbId='{imdbId}'");
                if (imdbId != null)
                {
                    var cachePath = $"Cache/OMDB/{imdbId}.json";
                    if (File.Exists(cachePath))
                    {
                        var cachedResponse = await File.ReadAllTextAsync(cachePath);
                        _logger.Event($"OMDb movie found in cache: {imdbId}.json");
                        return JsonConvert.DeserializeObject<Movie>(cachedResponse);
                    }
                }
            }
            else
            {
                _logger.Event($"No entry found for title '{title}' in Cache/OMDB/files.json");
            }
        }

        string? url = null;
        if (imdbId != null)
        {
            url = $"http://www.omdbapi.com/?apikey={_omdbApiKey}&i={imdbId}";
        }
        else if (title != null)
        {
            var blacklistedTerms = new[] { "[Double Sided]" };
            foreach (var term in blacklistedTerms)
            {
                title = title.Replace(term, "");
            }
            title = title.Trim();
            var encodedTitle = WebUtility.UrlEncode(title);
            url = $"http://www.omdbapi.com/?apikey={_omdbApiKey}&t={encodedTitle}";
        }

        if (url is null)
        {
            return null;
        }

        _logger.Event($"OMDb API fetch submitted: {url}");
        try
        {
            var response = await _httpClient.GetStringAsync(url);
            _logger.Event($"OMDb API fetch results: {response}");
            var movie = JsonConvert.DeserializeObject<Movie>(response);

            if (movie != null && !string.IsNullOrEmpty(movie.Title) && !string.IsNullOrEmpty(movie.ImdbID))
            {
                movie.RawOmdbJson = response; // Store raw JSON in the Movie object

                Directory.CreateDirectory("Cache/OMDB");
                var cachePath = $"Cache/OMDB/{movie.ImdbID}.json";
                await File.WriteAllTextAsync(cachePath, response);

                var filesJson = await File.ReadAllTextAsync("Cache/OMDB/files.json");
                var filesData = JObject.Parse(filesJson);
                var filesArray = filesData["files"] as JArray;
                if (filesArray != null && !filesArray.Any(f => f["imdbId"]?.ToString() == movie.ImdbID))
                {
                    var newFileEntry = new JObject();
                    newFileEntry["t"] = WebUtility.UrlEncode(title);
                    newFileEntry["imdbId"] = movie.ImdbID;
                    filesArray.Add(newFileEntry);
                    await File.WriteAllTextAsync("Cache/OMDB/files.json", filesData.ToString());
                }

                movie.Poster = $"http://img.omdbapi.com/?apikey={_omdbApiKey}&i={movie.ImdbID}";
                return movie; // Return only the Movie object
            }
        }
        catch (HttpRequestException e)
        {
            _logger.Error($"OMDb API Error: {e.Message}");
        }
        return null;
    }
}
