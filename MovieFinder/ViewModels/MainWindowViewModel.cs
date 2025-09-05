using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MovieFinder.Models;
using MovieFinder.Services;

namespace MovieFinder.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly Database? _database;
    private readonly BarcodeService _barcodeService;

    public BarcodeService BarcodeService => _barcodeService;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private string _barcodeScannerStatus = "DISCONNECTED"; // Default status

    [ObservableProperty]
    private string _logFilePath = string.Empty;

    [ObservableProperty]
    private string _csvFilePath = string.Empty;

    [ObservableProperty]
    private string _applicationLog = string.Empty;

    public ObservableCollection<Movie> Movies { get; } = new();

    [RelayCommand]
    private void OpenDebugWindow()
    {
        // This will be implemented in the MainWindow code-behind to open the debug window.
        // The command is exposed for binding in XAML.
    }

    // This constructor is used by the XAML designer.
    public MainWindowViewModel() 
    {
        _barcodeService = new BarcodeService();
    }

    public MainWindowViewModel(Database database, BarcodeService barcodeService)
    {
        _database = database;
        _barcodeService = barcodeService;
        LoadMovies().ConfigureAwait(false);

        var logBuilder = new StringBuilder();
        logBuilder.AppendLine("Starting application");

        BarcodeScannerStatus = _barcodeService.GetScannerStatus();
        logBuilder.AppendLine($"Looking for device: {BarcodeScannerStatus}");

        if (_barcodeService.IsScannerConnected())
        {
            var deviceInfo = _barcodeService.GetDeviceInfo();
            logBuilder.AppendLine($"FOUND - SN: {deviceInfo?.SerialNumber}");
        }
        else
        {
            logBuilder.AppendLine("NOT FOUND");
        }

        ApplicationLog = logBuilder.ToString();

        // These paths would typically come from a configuration file
        LogFilePath = "/var/log/MovieFinder.log";
        CsvFilePath = "/var/log/MovieFinder.csv";
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

    [RelayCommand]
    private void AddNewMovie()
    {
        // TODO: Implement logic to add a new movie (show dialog, etc.)
        // For now, just a placeholder
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
