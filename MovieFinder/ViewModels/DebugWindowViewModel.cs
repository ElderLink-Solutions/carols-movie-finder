using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MovieFinder.Models;
using MovieFinder.Services;

namespace MovieFinder.ViewModels;

public partial class DebugWindowViewModel : ObservableObject
{
    private readonly BarcodeService _barcodeService;

    [ObservableProperty]
    private string _barcodeScannerStatus = string.Empty;

    [ObservableProperty]
    private UsbDeviceInfo? _deviceInfo;

    public DebugWindowViewModel(BarcodeService barcodeService)
    {
        _barcodeService = barcodeService;
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
