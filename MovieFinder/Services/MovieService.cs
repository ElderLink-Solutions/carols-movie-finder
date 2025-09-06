using System.Net.Http;
using System.Threading.Tasks;
using MovieFinder.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Configuration;
using MovieFinder.Services;

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

        string? url = null;
        if (movieIdentifier?["imdb_id"]?.ToString() is string imdbId)
        {
            url = $"http://www.omdbapi.com/?apikey={_omdbApiKey}&i={imdbId}";
        }
        else if (movieIdentifier?["title"]?.ToString() is string title)
        {
            url = $"http://www.omdbapi.com/?apikey={_omdbApiKey}&t={title}";
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

            if (movie != null && !string.IsNullOrEmpty(movie.Title))
            {
                movie.Poster = $"http://img.omdbapi.com/?apikey={_omdbApiKey}&i={movie.ImdbID}";
                return movie;
            }
        }
        catch (HttpRequestException e)
        {
            _logger.Error($"OMDb API Error: {e.Message}");
        }
        return null;
    }
}
