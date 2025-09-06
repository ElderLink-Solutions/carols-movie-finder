using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MovieFinder.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace MovieFinder.ViewModels;

public partial class MovieDetailWindowViewModel : ObservableObject
{
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
    private string? _fullJsonOutput;

    [ObservableProperty]
    private string _shelfLocation = string.Empty;

    [ObservableProperty]
    private string _borrowedBy = string.Empty;

    public MovieDetailWindowViewModel(Movie movie, JObject? fullOmdbJson = null)
    {
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
        }

        if (fullOmdbJson != null)
        {
            FullJsonOutput = fullOmdbJson.ToString(Formatting.Indented);
        }
    }

    [RelayCommand]
    private void OpenFullJsonDetails()
    {
        if (FullJsonOutput != null)
        {
            OpenFullJsonDetailsRequested?.Invoke(FullJsonOutput);
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
