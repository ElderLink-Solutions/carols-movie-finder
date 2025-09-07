using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MovieFinder.Models;
using MovieFinder.Services;
using MovieFinder.Views;
using Avalonia.Threading;
using System.IO;
using Newtonsoft.Json.Linq;

namespace MovieFinder.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly Database? _database;
    private readonly BarcodeService? _barcodeService;
    private readonly MovieService? _movieService;
    private readonly IAppLogger? _logger;

    public BarcodeService? BarcodeService => _barcodeService;

    private string _searchQuery = string.Empty;
    public string SearchQuery
    {
        get => _searchQuery;
        set => SetProperty(ref _searchQuery, value);
    }

    private string _barcodeScannerStatus = "DISCONNECTED"; // Default status
    public string BarcodeScannerStatus
    {
        get => _barcodeScannerStatus;
        set => SetProperty(ref _barcodeScannerStatus, value);
    }

    private string _logFilePath = string.Empty;
    public string LogFilePath
    {
        get => _logFilePath;
        set => SetProperty(ref _logFilePath, value);
    }

    private string _csvFilePath = string.Empty;
    public string CsvFilePath
    {
        get => _csvFilePath;
        set => SetProperty(ref _csvFilePath, value);
    }

    public ObservableCollection<Movie> Movies { get; } = new();
    public ObservableCollection<string> LogMessages { get; } = new();
    public ObservableCollection<string> FilteredLogMessages { get; } = new();

    public int TotalMovies => Movies.Count;

    [ObservableProperty]
    private string _copiedNotification = string.Empty;

    [ObservableProperty]
    private bool _copiedNotificationVisible;

    [ObservableProperty]
    private Movie? _selectedMovie;

    partial void OnSelectedMovieChanged(Movie? value)
    {
        if (value != null)
        {
            // Open MovieDetailDisplayWindow
            _ = Dispatcher.UIThread.InvokeAsync(async () =>
            {
                if (value.ImdbID != null)
                {
                    var cachePath = $"Cache/OMDB/{value.ImdbID}.json";
                    // No need to read from cache or parse JObject here, RawOmdbJson is in the Movie object
                    var movieDetailDisplayViewModel = new MovieDetailWindowViewModel(value, _logger);
                    var movieDetailDisplayWindow = new MovieDetailDisplayWindow
                    {
                        DataContext = movieDetailDisplayViewModel
                    };
                    if (App.CurrentMainWindow != null)
                        await movieDetailDisplayWindow.ShowDialog(App.CurrentMainWindow);
                    else
                        await movieDetailDisplayWindow.ShowDialog(App.CurrentMainWindow!);
                }
            });
        }
    }

    private bool _showKeyEventsOnly = true;
    public bool ShowKeyEventsOnly
    {
        get => _showKeyEventsOnly;
        set
        {
            SetProperty(ref _showKeyEventsOnly, value);
            FilterLogs();
        }
    }

    // This constructor is used by the XAML designer.
    public MainWindowViewModel()
    {
        Movies.CollectionChanged += Movies_CollectionChanged;
    }

    public MainWindowViewModel(Database database, BarcodeService barcodeService, MovieService movieService, IAppLogger logger)
    {
        _database = database;
        _barcodeService = barcodeService;
        _movieService = movieService;
        _logger = logger;

        Movies.CollectionChanged += Movies_CollectionChanged;

        if (_logger is AppLogger appLogger)
        {
            appLogger.OnLogMessage += OnLogMessageReceived;
        }

        LoadMovies().ConfigureAwait(false);

        _logger?.Log("Starting application");

        // Subscribe to barcode scanned event
        if (_barcodeService != null)
            _barcodeService.BarcodeScanned += OnBarcodeScanned;

        BarcodeScannerStatus = _barcodeService?.GetScannerStatus() ?? "DISCONNECTED";
        _logger?.Log($"Looking for device: {BarcodeScannerStatus}");

        if (_barcodeService != null && _barcodeService.IsScannerConnected())
        {
            var deviceInfo = _barcodeService.GetDeviceInfo();
            _logger?.Log($"FOUND - SN: {deviceInfo?.SerialNumber}");
        }
        else
        {
            _logger?.Log("NOT FOUND");
        }

        // These paths would typically come from a configuration file
        LogFilePath = "/var/log/MovieFinder.log";
        CsvFilePath = "/var/log/MovieFinder.csv";
    }

    private void OnLogMessageReceived(string message)
    {
        LogMessages.Insert(0, message);
        FilterLogs();
    }

    private void Movies_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(TotalMovies));
    }

    private void FilterLogs()
    {
        FilteredLogMessages.Clear();
        var logs = ShowKeyEventsOnly ? LogMessages.Where(m => m.Contains("[EVENT]")) : LogMessages;
        foreach (var log in logs)
        {
            FilteredLogMessages.Add(log);
        }
    }

    private async void OnBarcodeScanned(string barcode)
    {
        _logger?.Event($"Barcode Scanned: {barcode}");
        if (_movieService == null)
        {
            _logger?.Log("MovieService is not initialized.");
            return;
        }

        var movie = await _movieService.FetchMovieDetailsFromBarcode(barcode);
        if (movie != null)
        {
            _logger?.Event($"Found movie: {movie.Title}");

            // Open the MovieDetailWindow
            _ = Dispatcher.UIThread.InvokeAsync(async () =>
            {
                Movies.Clear();
                Movies.Add(movie);

                var movieDetailViewModel = new MovieDetailWindowViewModel(movie, _logger);
                var movieDetailWindow = new MovieDetailDisplayWindow
                {
                    DataContext = movieDetailViewModel
                };

                // Show the window and wait for a result, only if owner is not null
                bool? result = null;
                if (App.CurrentMainWindow != null)
                {
                    result = await movieDetailWindow.ShowDialog<bool?>(App.CurrentMainWindow);
                }
                else
                {
                    result = await movieDetailWindow.ShowDialog<bool?>(App.CurrentMainWindow!);
                }

                if (result == true) // Save button was clicked
                {
                    // Update the database
                    if (_database != null)
                    {
                        movie = await _database.SaveMovieAsync(movie); // Need to implement SaveMovieAsync in Database.cs
                        _logger?.Event($"Database updated, ID: {movie.Id}");
                    }
                }
            });
        }
        else
        {
            _logger?.Log($"Could not find a movie for barcode {barcode}");
        }
    }

    [RelayCommand]
    private async Task SearchMovies()
    {
        _logger?.Log($"Search Movies button pressed. Query: {SearchQuery}");
        BarcodeScannerStatus = "Button Pressed: Search Movies";
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

    [RelayCommand]
    private void AddNewMovie()
    {
        _barcodeService?.StartReadingBarcodes();
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
