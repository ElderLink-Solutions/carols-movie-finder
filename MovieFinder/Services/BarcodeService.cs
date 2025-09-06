using System;
using System.Linq;
using System.Text; // Added for Encoding
using System.Threading; // Added for CancellationToken
using System.Threading.Tasks; // Added for Task.Run
using LibUsbDotNet;
using LibUsbDotNet.Main; // Added for UsbError, ReadEndpointID
using MovieFinder.Models;

namespace MovieFinder.Services;

public class BarcodeService
{
    private readonly IAppLogger _logger;
    private Task? _barcodeReaderTask;

    // These values may need to be changed for the actual device.
    private const int VendorId = 0x28e9;
    private const int ProductId = 0x03da;

    public UsbDevice? MyUsbDevice;

    public BarcodeService(IAppLogger logger)
    {
        _logger = logger;
    }

    public string GetScannerStatus()
    {
        string result = IsScannerConnected() ? "CONNECTED" : "DISCONNECTED";
        _logger.Log($"GetScannerStatus: {result}");
        return result;
    }

    public bool IsScannerConnected()
    {
        _logger.Log($"IsScannerConnected: Attempting to find USB device with Vendor ID: 0x{VendorId:X} and Product ID: 0x{ProductId:X}...");
        try
        {
            var allDevices = UsbDevice.AllDevices;
            var registry = allDevices.FirstOrDefault(d => d.Vid == VendorId && d.Pid == ProductId);
            if (registry != null)
            {
                _logger.Log($"Device found. Attempting to open device...");
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
        _logger.Log($"Enter UsbDeviceInfo");
        if (MyUsbDevice == null)
        {
            _logger.Log($"MyUsbDevice == null");
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

    private CancellationTokenSource? _cancellationTokenSource;

    public void StartReadingBarcodes()
    {
        _logger.Log("Attempting to start barcode reading...");
        if (MyUsbDevice == null || !MyUsbDevice.IsOpen)
        {
            _logger.Log("Barcode scanner not connected or not open. Cannot start reading.");
            return;
        }

        _cancellationTokenSource = new CancellationTokenSource();
        _barcodeReaderTask = Task.Run(() => ReadBarcodesLoop(_cancellationTokenSource.Token));
        _logger.Log($"Started listening for barcodes on thread {_barcodeReaderTask.Id}.");
    }

    public void StopReadingBarcodes()
    {
        if (_cancellationTokenSource != null)
        {
            _logger.Log($"Sending shutdown signal to barcode reader thread {_barcodeReaderTask?.Id}.");
            _cancellationTokenSource.Cancel();
            _barcodeReaderTask?.Wait();
            _logger.Log($"Barcode reader thread {_barcodeReaderTask?.Id} has gracefully shut down.");
        }
    }

    private void ReadBarcodesLoop(CancellationToken cancellationToken)
    {
        _logger.Log("Entering ReadBarcodesLoop."); // Log entry into the method
        UsbEndpointReader? reader = null;
        try
        {
            // Find the IN endpoint (usually for reading data from device)
            // Assuming the barcode scanner uses an Interrupt IN endpoint
            // You might need to adjust the endpoint address based on your specific scanner's descriptor
            _logger.Log("Attempting to open endpoint reader...");
            reader = MyUsbDevice?.OpenEndpointReader(ReadEndpointID.Ep01); // Common for HID devices

            if (reader == null)
            {
                _logger.Log("Could not open endpoint reader for barcode scanner. Reader is null.");
                return;
            }
            _logger.Log("Endpoint reader opened successfully.");

            byte[] readBuffer = new byte[64]; // Typical buffer size for HID reports
            int bytesRead;
            ErrorCode errorCode; // Changed from UsbError to ErrorCode

            _logger.Log("Barcode reading loop started.");

            while (!cancellationToken.IsCancellationRequested)
            {
                _logger.Log("Attempting to read from USB device..."); // Log each read attempt
                errorCode = reader.Read(readBuffer, 1000, out bytesRead); // 1000ms timeout

                if (errorCode == ErrorCode.None) // Check for success first
                {
                    if (bytesRead > 0)
                    {
                        // Log raw bytes
                        string rawDataHex = BitConverter.ToString(readBuffer, 0, bytesRead).Replace("-", "");
                        _logger.Log($"Raw USB data received (Hex): {rawDataHex}");

                        // Assuming the barcode data is ASCII encoded
                        string barcode = Encoding.ASCII.GetString(readBuffer, 0, bytesRead).TrimEnd('\0');
                        if (!string.IsNullOrWhiteSpace(barcode))
                        {
                            _logger.Log($"Barcode Scanned: {barcode}");
                            BarcodeScanned?.Invoke(barcode);
                        }
                    }
                    else
                    {
                        _logger.Log("USB Read: No bytes read (timeout or empty data).");
                    }
                }
                else if (errorCode == ErrorCode.IoTimedOut)
                {
                    _logger.Log("USB Read: Timeout."); // Re-enabled for troubleshooting
                }
                else // Other errors
                {
                    _logger.Log($"USB Read Error: {errorCode}. Bytes read: {bytesRead}");
                    _logger.Log("Exiting barcode reading loop due to error."); // Log exit reason
                    break; // Exit loop on critical error
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Error in barcode reading loop: {ex.ToString()}");
        }
        finally
        {
            reader?.Dispose(); // Dispose the reader when done
            _logger.Log("Barcode reading loop stopped.");
        }
    }
}
