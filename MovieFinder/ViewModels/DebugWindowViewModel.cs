using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MovieFinder.Models;
using MovieFinder.Services;
using System.Collections.ObjectModel; // Added for ObservableCollection
using System;

namespace MovieFinder.ViewModels;

public partial class DebugWindowViewModel : ObservableObject
{
    private readonly BarcodeService _barcodeService;
    private readonly IAppLogger? _logger;

    [ObservableProperty]
    private string _barcodeScannerStatus = string.Empty;

    [ObservableProperty]
    private UsbDeviceInfo? _deviceInfo;

    public ObservableCollection<string> LogMessages { get; } = new ObservableCollection<string>();

    public DebugWindowViewModel(BarcodeService barcodeService, IAppLogger? logger)
    {
        _barcodeService = barcodeService;
        _logger = logger;
        if (_logger is AppLogger appLogger)
        {
            appLogger.Initialize(message => LogMessages.Add($"[{DateTime.Now:HH:mm:ss}] {message}"));
        }
        CheckBarcodeScanner();
    }

    [RelayCommand]
    private void CheckBarcodeScanner()
    {
        BarcodeScannerStatus = _barcodeService.GetScannerStatus();
        if (_barcodeService.IsScannerConnected())
        {
            DeviceInfo = _barcodeService.GetDeviceInfo();
        }
        else
        {
            DeviceInfo = null;
        }
    }
}
