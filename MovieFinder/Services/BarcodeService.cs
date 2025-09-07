using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LibUsbDotNet;
using LibUsbDotNet.Main;
using Microsoft.Extensions.Configuration;
using MovieFinder.Models;

namespace MovieFinder.Services;

public class BarcodeService : IDisposable
{
    private readonly IAppLogger _logger;
    private readonly IConfiguration _configuration;
    private readonly IShutdownService _shutdownService;
    private Task? _barcodeReaderTask;
    private CancellationTokenSource? _readingCts;
    private readonly StringBuilder _barcode = new();

    public UsbDevice? MyUsbDevice;

    public BarcodeService(IAppLogger logger, IConfiguration configuration, IShutdownService shutdownService)
    {
        _logger = logger;
        _configuration = configuration;
        _shutdownService = shutdownService;
    }

    private int VendorId
    {
        get
        {
            var vendorStr = _configuration["IDVENDOR"];
            if (!string.IsNullOrWhiteSpace(vendorStr) && int.TryParse(vendorStr, System.Globalization.NumberStyles.HexNumber, null, out var value))
                return value;
            // fallback to default if not set
            return 0x28e9;
        }
    }

    private int ProductId
    {
        get
        {
            var productStr = _configuration["IDPRODUCT"];
            if (!string.IsNullOrWhiteSpace(productStr) && int.TryParse(productStr, System.Globalization.NumberStyles.HexNumber, null, out var value))
                return value;
            // fallback to default if not set
            return 0x03da;
        }
    }

    public string GetScannerStatus()
    {
        string result = IsScannerConnected() ? "CONNECTED" : "DISCONNECTED";
        _logger.Log($"GetScannerStatus: {result}");
        return result;
    }

    public bool IsScannerConnected()
    {
        if (MyUsbDevice != null && MyUsbDevice.IsOpen)
        {
            return true;
        }

        _logger.Log($"IsScannerConnected: Attempting to find USB device with Vendor ID: 0x{VendorId:X} and Product ID: 0x{ProductId:X}...");
        try
        {
            var allDevices = UsbDevice.AllDevices;
            var registry = allDevices.FirstOrDefault(d => d.Vid == VendorId && d.Pid == ProductId);
            if (registry != null)
            {
                _logger.Log("Device found. Attempting to open device...");
                UsbDevice? device;
                bool success = registry.Open(out device);
                MyUsbDevice = device;
                if (success && MyUsbDevice != null)
                {
                    _logger.Log("Successfully opened USB device.");
                }
                else
                {
                    _logger.Log("Failed to open USB device. This might be a permissions issue or the device is in use.");
                }
                return success && MyUsbDevice != null;
            }
            MyUsbDevice = null;
            _logger.Log("USB device not found or could not be opened. MyUsbDevice set to null.");
            _logger.Log("USB device not found. It might be disconnected or the Vendor/Product ID is incorrect.");
            return false;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.ToString());
            return false;
        }
    }

    public UsbDeviceInfo? GetDeviceInfo()
    {
        _logger.Log("Enter UsbDeviceInfo");
        if (MyUsbDevice == null)
        {
            _logger.Log("MyUsbDevice == null");
            return null;
        }

        var usbRegistry = UsbDevice.AllDevices.FirstOrDefault(d => d.Vid == VendorId && d.Pid == ProductId);
        if (usbRegistry == null)
            return null;

        string? serialNumber = null;
        // Try to get serial number from registry properties if available
        if (usbRegistry.DeviceProperties != null && usbRegistry.DeviceProperties.ContainsKey("SerialNumber"))
        {
            serialNumber = usbRegistry.DeviceProperties["SerialNumber"] as string ?? "";
        }

        var deviceInfo = new UsbDeviceInfo
        {
            VendorId = usbRegistry.Vid,
            ProductId = usbRegistry.Pid,
            SerialNumber = serialNumber
        };

        // MyUsbDevice.Close(); // Removed: Device should remain open for continuous reading
        return deviceInfo;
    }

    public event Action<string>? BarcodeScanned;



    public void StartReadingBarcodes()
    {
        if (_barcodeReaderTask != null && !_barcodeReaderTask.IsCompleted)
        {
            _logger.Warn("Barcode reader task is already running.");
            return;
        }

        _logger.Event("StartReadingBarcodes.");
        _logger.Information("Attempting to start barcode reading...");
        if (MyUsbDevice == null || !MyUsbDevice.IsOpen)
        {
            _logger.Warn("Barcode scanner not connected or not open. Cannot start reading.");
            return;
        }

        _readingCts = new CancellationTokenSource();
        var token = CancellationTokenSource.CreateLinkedTokenSource(_shutdownService.ShutdownToken, _readingCts.Token).Token;

        _barcodeReaderTask = Task.Run(() => ReadBarcodesLoop(token));
        _shutdownService.RegisterTask(_barcodeReaderTask, "BarcodeReader");
        _logger.Information($"Started listening for barcodes on thread {_barcodeReaderTask.Id}.");
    }

    public void StopReadingBarcodes()
    {
        if (_readingCts != null && !_readingCts.IsCancellationRequested)
        {
            _logger.Event("StopReadingBarcodes.");
            _readingCts.Cancel();
        }
    }



    private static readonly Dictionary<byte, char> HidScanCodeMap = new()
    {
        { 0x1E, '1' }, { 0x1F, '2' }, { 0x20, '3' }, { 0x21, '4' }, { 0x22, '5' },
        { 0x23, '6' }, { 0x24, '7' }, { 0x25, '8' }, { 0x26, '9' }, { 0x27, '0' }
    };

    private char HidCodeToChar(byte hidCode)
    {
        if (HidScanCodeMap.TryGetValue(hidCode, out char ch))
        {
            return ch;
        }
        return '\0';
    }

    private void ReadBarcodesLoop(CancellationToken cancellationToken)
    {
        UsbEndpointReader? reader = null;
        try
        {
            reader = MyUsbDevice?.OpenEndpointReader(ReadEndpointID.Ep01);
            if (reader == null)
            {
                return;
            }

            byte[] readBuffer = new byte[64];
            int bytesRead;
            ErrorCode errorCode;

            while (!cancellationToken.IsCancellationRequested)
            {
                errorCode = reader.Read(readBuffer, 1000, out bytesRead);

                if (errorCode == ErrorCode.None && bytesRead > 0)
                {
                    byte hidCode = readBuffer[2];
                    if (hidCode == 0x28) // Enter key
                    {
                        if (_barcode.Length > 0)
                        {
                            var barcode = _barcode.ToString();
                            _logger.Log($"Scanned barcode: {barcode}");
                            BarcodeScanned?.Invoke(barcode);
                            _barcode.Clear();
                        }
                    }
                    else
                    {
                        char c = HidCodeToChar(hidCode);
                        if (c != '\0')
                        {
                            _barcode.Append(c);
                        }
                    }
                }
                else if (errorCode != ErrorCode.IoTimedOut)
                {
                    _logger.Log($"USB Read Error: {errorCode}.");
                    break;
                }
            }

            if (_shutdownService.ShutdownToken.IsCancellationRequested)
            {
                _logger.Log("Shutdown service requested cancellation.");
            }
            if (_readingCts?.IsCancellationRequested == true)
            {
                _logger.Log("Reading cancellation requested.");
            }
        }
        catch (OperationCanceledException)
        {
            _logger.Log("Barcode reading loop canceled.");
        }
        catch (Exception ex)
        {
            _logger.Error($"Error in barcode reading loop: {ex}");
        }
        finally
        {
            reader?.Dispose();
        }
        _logger.Log("Barcode reader loop finished.");
    }

    public void Dispose()
    {
        StopReadingBarcodes();
        if (MyUsbDevice != null && MyUsbDevice.IsOpen)
        {
            MyUsbDevice.Close();
            _logger.Log("MyUsbDevice closed and disposed.");
        }
        else if (MyUsbDevice != null)
        {
            _logger.Log("MyUsbDevice was not open, but was not null. Disposing anyway.");
        }
        else
        {
            _logger.Log("MyUsbDevice was null. Nothing to dispose.");
        }
    }
}
