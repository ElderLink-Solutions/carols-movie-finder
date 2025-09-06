using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MovieFinder.Models;
using System;

namespace MovieFinder.ViewModels;

public partial class MovieAddMovieFormViewModel : ObservableObject
{
    public Action<bool>? CloseRequested { get; set; }
    public Movie Movie { get; }

    [ObservableProperty]
    private string _shelfLocation = string.Empty;

    [ObservableProperty]
    private string _borrowedBy = string.Empty;

    public MovieAddMovieFormViewModel(Movie movie)
    {
        Movie = movie;
        ShelfLocation = movie.ShelfLocation ?? string.Empty;
        BorrowedBy = movie.BorrowedBy ?? string.Empty;
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