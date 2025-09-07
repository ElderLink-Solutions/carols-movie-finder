using System;
using System.Text;
using MovieFinder.Models;
using Microsoft.Extensions.Logging;

namespace MovieFinder.Services
{
    public class KeyboardWedgeBarcodeService : IBarcodeService
    {
        // This service requires a connection to the main window's TextInput event.
        // The MainWindow should call the HandleTextInput method of this service.

        private readonly ILogger<KeyboardWedgeBarcodeService> _logger;
        private readonly StringBuilder _barcodeBuffer = new();
        private DateTime _lastKeystroke = DateTime.Now;
        private bool _isListening;

        public event Action<string>? BarcodeScanned;
        public event Action<string>? ScannerStatusChanged;

        public KeyboardWedgeBarcodeService(ILogger<KeyboardWedgeBarcodeService> logger)
        {
            _logger = logger;
            _logger.LogDebug("Initializing KeyboardWedgeBarcodeService...");
        }


        public void StartReadingBarcodes()
        {
            _logger.LogInformation("Starting keyboard wedge barcode service...");
            _isListening = true;
        }

        public void StopReadingBarcodes()
        {
            _logger.LogInformation("Stopping keyboard wedge barcode service...");
            _isListening = false;
        }

        public string GetScannerStatus()
        {
            _logger.LogDebug("Getting scanner status...");
            var status = _isListening ? "LISTENING" : "IDLE";
            _logger.LogDebug("Scanner status: {Status}", status);
            return status;
        }

        public bool IsScannerConnected()
        {
            _logger.LogDebug("Checking if scanner is connected...");
            _logger.LogDebug("Scanner connected: True");
            return true; // Always connected, as it's the keyboard
        }

        public UsbDeviceInfo? GetDeviceInfo()
        {
            _logger.LogDebug("Getting device info...");
            var deviceInfo = new UsbDeviceInfo
            {
                VendorId = 0,
                ProductId = 0,
                SerialNumber = "KeyboardWedge"
            };
            _logger.LogDebug("Device info: VendorId={VendorId}, ProductId={ProductId}, SerialNumber={SerialNumber}", deviceInfo.VendorId, deviceInfo.ProductId, deviceInfo.SerialNumber);
            return deviceInfo;
        }

        public void HandleTextInput(string text)
        {
            _logger.LogDebug("HandleTextInput called with text: {Text}", text);

            if (!_isListening)
            {
                _logger.LogDebug("Not listening for barcodes. Ignoring text input.");
                return;
            }

            if (DateTime.Now - _lastKeystroke > TimeSpan.FromMilliseconds(100))
            {
                _logger.LogDebug("Keystroke timeout. Clearing barcode buffer.");
                _barcodeBuffer.Clear();
            }

            _lastKeystroke = DateTime.Now;

            if (text == "" || text == "\n")
            {
                _logger.LogDebug("Newline or empty text detected.");
                if (_barcodeBuffer.Length > 0)
                {
                    var barcode = _barcodeBuffer.ToString();
                    _logger.LogInformation("Barcode scanned: {Barcode}", barcode);
                    BarcodeScanned?.Invoke(barcode);
                    _barcodeBuffer.Clear();
                }
                else
                {
                    _logger.LogDebug("Barcode buffer is empty. No barcode to process.");
                }
            }
            else
            {
                _logger.LogDebug("Appending text to barcode buffer: {Text}", text);
                _barcodeBuffer.Append(text);
            }
        }
    }
}