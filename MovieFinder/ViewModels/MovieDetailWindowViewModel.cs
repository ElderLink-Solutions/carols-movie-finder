using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MovieFinder.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using Avalonia.Media.Imaging; // Added
using System.Net.Http; // Added
using System.Threading.Tasks; // Added
using MovieFinder.Services; // Added

namespace MovieFinder.ViewModels;

public partial class MovieDetailWindowViewModel : ObservableObject
{
    private readonly IAppLogger? _logger; // Added

    public Action<bool>? CloseRequested { get; set; }
    public Action<string>? OpenFullJsonDetailsRequested { get; set; }
    public Movie Movie { get; }

    [ObservableProperty]
    private string? _title;

    [ObservableProperty]
    private string? _year;

    [ObservableProperty]
    private string? _genre;

    [ObservableProperty]
    private string? _director;

    [ObservableProperty]
    private string? _actors;

    [ObservableProperty]
    private string? _plot;

    [ObservableProperty]
    private string? _imdbRating;

    [ObservableProperty]
    private string? _runtime;

    [ObservableProperty]
    private string _poster = "Not Implemented"; // Placeholder for poster

    [ObservableProperty]
    private Bitmap? _posterImage; // Added for image display

    [ObservableProperty]
    private string? _fullJsonOutput;

    [ObservableProperty]
    private string _shelfLocation = string.Empty;

    [ObservableProperty]
    private string _borrowedBy = string.Empty;

    public MovieDetailWindowViewModel(Movie movie, IAppLogger? logger = null)
    {
        _logger = logger; // Initialize logger
        Movie = movie; // Assign the movie object
        Title = movie.Title;
        Year = movie.Year;
        Genre = movie.Genre;
        Director = movie.Director;
        Actors = movie.Actors;
        Plot = movie.Plot;
        ImdbRating = movie.ImdbRating;
        Runtime = movie.Runtime;
        ShelfLocation = movie.ShelfLocation ?? string.Empty; // Initialize from movie
        BorrowedBy = movie.BorrowedBy ?? string.Empty; // Initialize from movie

        if (!string.IsNullOrEmpty(movie.Poster))
        {
            Poster = movie.Poster;
            _ = LoadPosterImage(Poster); // Asynchronously load poster
        }
        else
        {
            _logger?.Log($"No poster URL found for movie: {movie.Title}");
        }

        if (!string.IsNullOrEmpty(movie.RawOmdbJson))
        {
            FullJsonOutput = JObject.Parse(movie.RawOmdbJson).ToString(Formatting.Indented);
        }
        else
        {
            _logger?.Log($"No raw OMDB JSON found in movie object for: {movie.Title}");
        }
    }

    private async Task LoadPosterImage(string url)
    {
        if (string.IsNullOrEmpty(url) || url == "N/A")
        {
            _logger?.Log($"Invalid poster URL: {url}");
            return;
        }

        try
        {
            using (var httpClient = new HttpClient())
            {
                var imageBytes = await httpClient.GetByteArrayAsync(url);
                using (var stream = new System.IO.MemoryStream(imageBytes))
                {
                    PosterImage = new Bitmap(stream);
                }
            }
            _logger?.Log($"Poster loaded successfully from {url}");
        }
        catch (Exception ex)
        {
            _logger?.Error($"Failed to load poster from {url}: {ex.Message}");
        }
    }

    [RelayCommand]
    private void OpenFullJsonDetails()
    {
        if (!string.IsNullOrEmpty(Movie.RawOmdbJson))
        {
            OpenFullJsonDetailsRequested?.Invoke(JObject.Parse(Movie.RawOmdbJson).ToString(Formatting.Indented));
        }
        else
        {
            _logger?.Log($"Attempted to open full JSON details, but RawOmdbJson is null or empty for movie: {Movie.Title}");
        }
    }

    [RelayCommand]
    private void Save()
    {
        Movie.ShelfLocation = ShelfLocation;
        Movie.BorrowedBy = BorrowedBy;
        CloseRequested?.Invoke(true);
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseRequested?.Invoke(false);
    }
}
