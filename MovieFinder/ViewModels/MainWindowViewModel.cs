using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MovieFinder.Models;
using MovieFinder.Services;

namespace MovieFinder.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly Database? _database;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    public ObservableCollection<Movie> Movies { get; } = new();

    // This constructor is used by the XAML designer.
    public MainWindowViewModel() { }

    public MainWindowViewModel(Database database)
    {
        _database = database;
        LoadMovies().ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task SearchMovies()
    {
        if (_database is null) return;

        Movies.Clear();
        if (string.IsNullOrWhiteSpace(SearchQuery))
                {
            var allMovies = await _database.GetMoviesAsync();
            foreach (var movie in allMovies)
            {
                Movies.Add(movie);
            }
        }
        else
        {
            var movies = await _database.SearchMoviesAsync(SearchQuery);
            foreach (var movie in movies)
            {
                Movies.Add(movie);
            }
        }
    }

    private async Task LoadMovies()
    {
        if (_database is null) return;

        var movies = await _database.GetMoviesAsync();
        foreach (var movie in movies)
        {
            Movies.Add(movie);
        }
    }
}
