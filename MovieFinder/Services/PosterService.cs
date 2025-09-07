using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Newtonsoft.Json.Linq;

namespace MovieFinder.Services;

public class PosterService
{
    private readonly HttpClient _httpClient;
    private readonly IAppLogger _logger;
    private const string CacheDir = "Cache/Posters";
    private const string CacheIndexPath = "Cache/Posters/files.json";

    public PosterService(IAppLogger logger)
    {
        _httpClient = new HttpClient();
        _logger = logger;
        Directory.CreateDirectory(CacheDir);
        if (!File.Exists(CacheIndexPath))
        {
            File.WriteAllText(CacheIndexPath, "{\"files\":[]}");
        }
    }

    public async Task<Bitmap?> GetPosterAsync(string imdbId, string posterUrl)
    {
        var cachedPath = await GetCachedPosterPathAsync(imdbId);
        if (cachedPath != null && File.Exists(cachedPath))
        {
            _logger.Event($"Poster found in cache: {cachedPath}");
            return new Bitmap(cachedPath);
        }

        _logger.Event($"Poster not found in cache for {imdbId}. Downloading...");

        if (string.IsNullOrEmpty(posterUrl))
        {
            return null;
        }

        try
        {
            var response = await _httpClient.GetAsync(posterUrl);
            response.EnsureSuccessStatusCode();
            var imageBytes = await response.Content.ReadAsByteArrayAsync();
            
            var fileExtension = Path.GetExtension(posterUrl.Split('?')[0]);
            if (string.IsNullOrEmpty(fileExtension))
            {
                fileExtension = ".jpg";
            }
            var imagePath = Path.Combine(CacheDir, $"{imdbId}{fileExtension}");

            await File.WriteAllBytesAsync(imagePath, imageBytes);
            await AddToCacheIndexAsync(imdbId, imagePath);
            _logger.Event($"Poster downloaded and cached: {imagePath}");

            return new Bitmap(new MemoryStream(imageBytes));
        }
        catch (HttpRequestException e)
        {
            _logger.Error($"Failed to download poster: {e.Message}");
            return null;
        }
    }

    private async Task<string?> GetCachedPosterPathAsync(string imdbId)
    {
        var json = await File.ReadAllTextAsync(CacheIndexPath);
        var cacheIndex = JObject.Parse(json);
        var files = cacheIndex["files"] as JArray;
        if (files == null) return null;

        foreach (var file in files)
        {
            if (file["imdbId"]?.ToString() == imdbId)
            {
                return file["filePath"]?.ToString();
            }
        }
        return null;
    }

    private async Task AddToCacheIndexAsync(string imdbId, string filePath)
    {
        var json = await File.ReadAllTextAsync(CacheIndexPath);
        var cacheIndex = JObject.Parse(json);
        var files = cacheIndex["files"] as JArray;
        if (files == null)
        {
            files = new JArray();
            cacheIndex["files"] = files;
        }

        var newEntry = new JObject
        {
            ["imdbId"] = imdbId,
            ["filePath"] = filePath
        };
        files.Add(newEntry);

        await File.WriteAllTextAsync(CacheIndexPath, cacheIndex.ToString());
    }
}
