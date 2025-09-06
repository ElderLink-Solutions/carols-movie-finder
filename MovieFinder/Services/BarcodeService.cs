using System;
using System.Linq;
using LibUsbDotNet;
using MovieFinder.Models;

namespace MovieFinder.Services;

public class BarcodeService
{
    // These values may need to be changed for the actual device.
    private const int VendorId = 0x28e9;
    private const int ProductId = 0x03da;

    public UsbDevice? MyUsbDevice;

    public string GetScannerStatus()
    {
        return IsScannerConnected() ? "CONNECTED" : "DISCONNECTED";
    }

    public bool IsScannerConnected()
    {
        Console.WriteLine($"Attempting to find USB device with Vendor ID: 0x{VendorId:X} and Product ID: 0x{ProductId:X}...");
        try
        {
            var allDevices = UsbDevice.AllDevices;
            var registry = allDevices.FirstOrDefault(d => d.Vid == VendorId && d.Pid == ProductId);
            if (registry != null)
            {
                Console.WriteLine($"Device found. Attempting to open device...");
                UsbDevice? device;
                bool success = registry.Open(out device);
                MyUsbDevice = device;
                if (success && MyUsbDevice != null)
                {
                    Console.WriteLine("Successfully opened USB device.");
                }
                else
                {
                    Console.WriteLine("Failed to open USB device. This might be a permissions issue or the device is in use.");
                }
                return success && MyUsbDevice != null;
            }
            MyUsbDevice = null;
            Console.WriteLine("USB device not found. It might be disconnected or the Vendor/Product ID is incorrect.");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return false;
        }
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
