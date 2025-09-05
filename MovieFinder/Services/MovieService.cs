using System.Net.Http;
using System.Threading.Tasks;
using MovieFinder.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MovieFinder.Services;

public class MovieService
{
    private readonly HttpClient _httpClient;
    private const string OmdbApiKey = "YOUR_OMDB_API_KEY"; // <-- PASTE YOUR OMDb API KEY HERE
    private const string UpcItemDbApiKey = "YOUR_UPCITEMDB_API_KEY"; // Optional, but recommended

    public MovieService()
    {
        _httpClient = new HttpClient();
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
        // Note: UPCitemdb has a free tier that doesn't require a key, but it's less reliable.
        var url = $"https://api.upcitemdb.com/prod/v1/lookup?upc={barcode}";
        
        try
        {
            var response = await _httpClient.GetStringAsync(url);
            var data = JObject.Parse(response);

            if (data?["items"] is JArray items && items.Count > 0)
            {
                var item = items[0];
                var identifier = new JObject();

                if (item?["imdb_id"]?.ToString() is string imdbId && !string.IsNullOrEmpty(imdbId))
                {
                    identifier["imdb_id"] = imdbId;
                    return identifier;
                }
                if (item?["title"]?.ToString() is string title && !string.IsNullOrEmpty(title))
                {
                    identifier["title"] = title;
                    return identifier;
                }
            }
        }
        catch (HttpRequestException e)
        {
            // Handle exceptions (e.g., network errors, invalid JSON)
            System.Diagnostics.Debug.WriteLine($"UPCitemdb API Error: {e.Message}");
        }
        
        return null;
    }

    private async Task<Movie?> GetMovieDetailsFromOmdb(JObject movieIdentifier)
    {
        if (string.IsNullOrEmpty(OmdbApiKey) || OmdbApiKey == "YOUR_OMDB_API_KEY")
        {
            System.Diagnostics.Debug.WriteLine("OMDb API key is missing.");
            return null;
        }

        string? url = null;
        if (movieIdentifier?["imdb_id"]?.ToString() is string imdbId)
        {
            url = $"http://www.omdbapi.com/?apikey={OmdbApiKey}&i={imdbId}";
        }
        else if (movieIdentifier?["title"]?.ToString() is string title)
        {
            url = $"http://www.omdbapi.com/?apikey={OmdbApiKey}&t={title}";
        }
        
        if (url is null)
        {
            return null;
        }

        try
        {
            var response = await _httpClient.GetStringAsync(url);
            var movie = JsonConvert.DeserializeObject<Movie>(response);

            if (movie != null && !string.IsNullOrEmpty(movie.Title))
            {
                return movie;
            }
        }
        catch (HttpRequestException e)
        {
            System.Diagnostics.Debug.WriteLine($"OMDb API Error: {e.Message}");
        }

        return null;
    }
}
