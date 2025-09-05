using System.Linq;
using LibUsbDotNet;
using LibUsbDotNet.Main;
using MovieFinder.Models;

namespace MovieFinder.Services;

public class BarcodeService
{
    // These values may need to be changed for the actual device.
    private const int VendorId = 0x0525; // Example Vendor ID
    private const int ProductId = 0xa4a5; // Example Product ID

    public UsbDevice? MyUsbDevice;

    public string GetScannerStatus()
    {
        return IsScannerConnected() ? "CONNECTED" : "DISCONNECTED";
    }

    public bool IsScannerConnected()
    {
        var allDevices = UsbDevice.AllDevices;
        var registry = allDevices.FirstOrDefault(d => d.Vid == VendorId && d.Pid == ProductId);
        if (registry != null)
        {
            UsbDevice? device;
            bool success = registry.Open(out device);
            MyUsbDevice = device;
            return success && MyUsbDevice != null;
        }
        MyUsbDevice = null;
        return false;
    }

    public UsbDeviceInfo? GetDeviceInfo()
    {
        if (MyUsbDevice == null)
        {
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

        MyUsbDevice.Close();
        return deviceInfo;
    }
}
