using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
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
    private readonly PosterService? _posterService;

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
        _logger?.Log($"OnSelectedMovieChanged called with movie: {value?.Title}");
        if (value != null)
        {
            // Open MovieDetailDisplayWindow
            _ = Dispatcher.UIThread.InvokeAsync(async () =>
            {
                if (value.ImdbID != null)
                {
                    var cachePath = $"Cache/OMDB/{value.ImdbID}.json";
                    // No need to read from cache or parse JObject here, RawOmdbJson is in the Movie object
                    var movieDetailDisplayViewModel = new MovieDetailWindowViewModel(value, _logger, _posterService);
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

    public MainWindowViewModel(Database database, BarcodeService barcodeService, MovieService movieService, IAppLogger logger, PosterService posterService)
    {
        _database = database;
        _barcodeService = barcodeService;
        _movieService = movieService;
        _logger = logger;
        _posterService = posterService;

        Movies.CollectionChanged += Movies_CollectionChanged;

        if (_logger is AppLogger appLogger)
        {
            appLogger.OnLogMessage += OnLogMessageReceived;
        }

        _ = LoadMovies().ContinueWith(t => 
        {
            if (t.IsFaulted)
            {
                _logger?.Error($"Error loading movies: {t.Exception}");
            }
        });

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
        _logger?.Log($"OnBarcodeScanned started for barcode: {barcode}");
        try
        {
            _logger?.Event($"Barcode Scanned: {barcode}");
            if (_movieService == null)
            {
                _logger?.Log("MovieService is not initialized.");
                return;
            }

            var movie = await _movieService.FetchMovieDetailsFromBarcode(barcode);

            if (movie == null && barcode.Length == 13 && barcode.StartsWith("8"))
            {
                var correctedBarcode = barcode.Substring(1);
                _logger?.Log($"Failed to find movie for {barcode}. Trying again with corrected barcode: {correctedBarcode}");
                movie = await _movieService.FetchMovieDetailsFromBarcode(correctedBarcode);
            }

            if (movie != null)
            {
                _logger?.Event($"Found movie: {movie.Title}");

                // Open the MovieDetailWindow
                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    try
                    {
                        _logger?.Log("Creating and showing MovieDetailDisplayWindow.");
                        var movieDetailViewModel = new MovieDetailWindowViewModel(movie, _logger, _posterService);
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
                        _barcodeService?.StopReadingBarcodes();

                        if (result == true) // Save button was clicked
                        {
                            _logger?.Log("Save button was clicked. Updating database.");
                            // Update the database
                            if (_database != null)
                            {
                                _logger?.Log("Calling SaveMovieAsync.");
                                movie = await _database.SaveMovieAsync(movie); // Need to implement SaveMovieAsync in Database.cs
                                _logger?.Log("SaveMovieAsync completed.");
                                _logger?.Event($"Database updated, ID: {movie.Id}");
                                _logger?.Log("Calling LoadMovies.");
                                await LoadMovies(); // Reload movies to reflect changes
                                _logger?.Log("LoadMovies completed.");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.Error($"Error in barcode scanned UI thread: {ex.Message}\n{ex.StackTrace}");
                    }
                });
            }
            else
            {
                _logger?.Log($"Could not find a movie for barcode {barcode}");
            }
        }
        catch (Exception ex)
        {
            _logger?.Error($"Error in OnBarcodeScanned: {ex.Message}\n{ex.StackTrace}");
        }
        _logger?.Log($"OnBarcodeScanned finished for barcode: {barcode}");
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
        _logger?.Log($"Loaded {movies.Count} movies from database.");

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            Movies.Clear();
            foreach (var movie in movies)
            {
                _logger?.Log($"  - Movie: {movie.Title}, ID: {movie.Id}");
                Movies.Add(movie);
            }
        });
    }
}
