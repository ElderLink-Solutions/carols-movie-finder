MovieFinder/README.md
# MovieFinder

MovieFinder is a cross-platform desktop application designed to help users manage and search a personal movie collection. Built with C# and Avalonia, it provides a modern, responsive user interface and leverages robust .NET technologies for data management and extensibility.

## Purpose

The purpose of MovieFinder is to offer an easy-to-use tool for cataloging, searching, and managing movie information locally. It is ideal for movie enthusiasts who want to keep track of their collections, including details such as titles, barcodes, and other metadata.

## How it works

When a barcode is scanned using the connected barcode device, MovieFinder submits a request to [barcodespider.com](https://www.barcodespider.com/) to look up information about the scanned barcode. The returned information (such as title or identifiers) is then used to make one or more requests to [omdbapi.com](https://www.omdbapi.com/) to retrieve detailed movie information and a second request to fetch the movie poster. Both the movie details and the poster are then stored in the local database for fast, offline access and future reference.

## Key Features

- **Add, search, and manage movies** in a local database
- **Barcode scanning support** for quick entry (if hardware is available)
- **Logging and output options** configurable via `appsettings.json`
- **Cross-platform UI** (Windows, Linux, macOS)

## Key Technologies

- **C#**: The primary programming language, chosen for its performance, safety, and rich ecosystem.
- **Avalonia**: A cross-platform UI framework for .NET, enabling the application to run on Windows, Linux, and macOS with a native look and feel.
- **Microsoft.Extensions.DependencyInjection**: Provides dependency injection for better code modularity and testability.
- **Microsoft.Extensions.Configuration**: Used to load and manage application settings from `appsettings.json`.
- **sqlite-net-pcl**: Lightweight ORM for SQLite, used for local data storage.
- **CommunityToolkit.Mvvm**: Implements the MVVM pattern for clean separation of UI and logic.

## Getting Started

### Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download) or later

---

### Setup Linux

**Step 1:** With the device unattached, run the following command in a terminal:
```sh
sudo dmesg -w
```
Plug in your barcode scanner device and look for lines similar to:
```
new full-speed USB device number 34 using xhci_hcd
New USB device found, idVendor=28e9, idProduct=03da, bcdDevice= 1.00
New USB device strings: Mfr=1, Product=2, SerialNumber=3
```
In this example, the values are `idVendor=28e9` and `idProduct=03da`.

**Step 2:** Add these values to your `appsettings.json` if needed.

**Step 3:** Create a udev rules file at `/etc/udev/rules.d/99-barcode-scanner.rules` with the following content:
```
# This rule sets permissions for the raw USB device.
SUBSYSTEM=="usb", ATTRS{idVendor}=="28e9", ATTRS{idProduct}=="03da", MODE="0666", GROUP="plugdev"

# This rule tells the kernel's usbhid driver to unbind (detach) from this device when it's plugged in.
ACTION=="add", SUBSYSTEM=="usb", ATTRS{idVendor}=="28e9", ATTRS{idProduct}=="03da", DRIVER=="usbhid", RUN+="/bin/sh -c 'echo -n $kernel > /sys/bus/usb/drivers/usbhid/unbind'"
```

**Step 4:** Add your user to the `plugdev` group:
```sh
sudo usermod -a -G plugdev $USER
```
Log out and log back in for group changes to take effect.

---

### Setup Windows

**Step 1:** Plug in your barcode scanner device.

**Step 2:** Open Device Manager and locate your device under "Universal Serial Bus devices" or similar. Right-click and select "Properties" to view the device's Vendor ID and Product ID.

**Step 3:** If needed, add the Vendor ID and Product ID to your `appsettings.json`.

**Step 4:** Ensure you have the correct drivers installed for your barcode scanner. Most devices will work with the default Windows HID drivers, but some may require manufacturer-specific drivers.

**Step 5:** Run the application as an administrator if you encounter permission issues accessing the USB device.

May be required:

1. **Download Zadig:**
   Get the tool from its official site: https://zadig.akeo.ie/

2. **Run Zadig as Administrator.**

3. **Go to the menu `Options -> List All Devices` and make sure it is checked.**
   This is a critical step.

4. **In the dropdown list, find your scanner.**
   It will likely be named "HID Keyboard Device" or something similar. Critically, select the entry and verify that the USB ID matches `28E9 03DA`.

5. **You will see the current driver on the left (it will probably say `kbdhid.sys` or similar). On the right side, you need to select the driver to install. Choose `WinUSB`.**

6. **Click the "Replace Driver" or "Install Driver" button.**

7. **After it completes, unplug and replug your scanner.**
   The "Keyboard" entry in Device Manager should now be gone, and you might see a new "Universal Serial Bus devices" entry.

---

### Setup Linux

### Running the Application

1. **Clone the repository**
   ```
   git clone <repository-url>
   cd carols-movie-finder/MovieFinder
   ```

2. **Restore dependencies**
   ```
   dotnet restore
   ```

3. **Build the project**
   ```
   dotnet build
   ```

4. **Run the application**
   ```
   dotnet run
   ```

### Configuration

The application uses an `appsettings.json` file for configuration. Key settings include:

- `DB_STORAGE`: Path to the SQLite database file.
- `LOGFILE_LOCATION`: Path to the log file.
- `OUTPUT_LOCATION`: Path for output files (e.g., CSV exports).

You can edit these values in `appsettings.json` to customize where data and logs are stored.

## Project Structure

- `App.axaml` / `App.axaml.cs`: Application entry point and initialization logic.
- `Models/`: Data models for movies and related entities.
- `Services/`: Core services such as database access and logging.
- `ViewModels/`: MVVM view models for UI logic.
- `Views/`: Avalonia XAML views for the user interface.

## Contributing

Contributions are welcome! Please open issues or submit pull requests for improvements or bug fixes.

## License

This project is licensed under the MIT License.

---

**MovieFinder** — Manage your movie collection with ease, on any desktop platform.
