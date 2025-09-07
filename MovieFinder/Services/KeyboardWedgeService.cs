using System;
using System.Text;
using MovieFinder.Models;

namespace MovieFinder.Services
{
    public interface IBarcodeService
    {
        event Action<string> BarcodeScanned;
        void StartReadingBarcodes();
        void StopReadingBarcodes();
        string GetScannerStatus();
        bool IsScannerConnected();
        UsbDeviceInfo? GetDeviceInfo();
    }

    public class KeyboardWedgeBarcodeService : IBarcodeService
    {
        // This service requires a connection to the main window's TextInput event.
        // The MainWindow should call the HandleTextInput method of this service.

        private readonly StringBuilder _barcodeBuffer = new();
        private DateTime _lastKeystroke = DateTime.Now;
        private bool _isListening;

        public event Action<string>? BarcodeScanned;

        public void StartReadingBarcodes()
        {
            _isListening = true;
        }

        public void StopReadingBarcodes()
        {
            _isListening = false;
        }

        public string GetScannerStatus()
        {
            return _isListening ? "LISTENING" : "IDLE";
        }

        public bool IsScannerConnected()
        {
            return true; // Always connected, as it's the keyboard
        }

        public UsbDeviceInfo? GetDeviceInfo()
        {
            return new UsbDeviceInfo
            {
                VendorId = 0,
                ProductId = 0,
                SerialNumber = "KeyboardWedge"
            };
        }

        public void HandleTextInput(string text)
        {
            if (!_isListening)
            {
                return;
            }

            if (DateTime.Now - _lastKeystroke > TimeSpan.FromMilliseconds(100))
            {
                _barcodeBuffer.Clear();
            }

            _lastKeystroke = DateTime.Now;

            if (text == "\r" || text == "\n")
            {
                if (_barcodeBuffer.Length > 0)
                {
                    var barcode = _barcodeBuffer.ToString();
                    BarcodeScanned?.Invoke(barcode);
                    _barcodeBuffer.Clear();
                }
            }
            else
            {
                _barcodeBuffer.Append(text);
            }
        }
    }
}