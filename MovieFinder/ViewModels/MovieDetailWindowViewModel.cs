using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MovieFinder.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace MovieFinder.ViewModels;

public partial class MovieDetailWindowViewModel : ObservableObject
{
    public Action? CloseRequested { get; set; }
    public Action<string>? OpenFullJsonDetailsRequested { get; set; }
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

    public MovieDetailWindowViewModel(Movie movie, JObject? fullOmdbJson = null)
    {
        Title = movie.Title;
        Year = movie.Year;
        Genre = movie.Genre;
        Director = movie.Director;
        Actors = movie.Actors;
        Plot = movie.Plot;
        ImdbRating = movie.ImdbRating;
        Runtime = movie.Runtime;

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
}
