using System;
using MovieFinder.Models;

namespace MovieFinder.Services
{
    public interface IBarcodeService
    {
        event Action<string> BarcodeScanned;
        event Action<string> ScannerStatusChanged;
        void StartReadingBarcodes();
        void StopReadingBarcodes();
        string GetScannerStatus();
        bool IsScannerConnected();
        UsbDeviceInfo? GetDeviceInfo();
    }
}
