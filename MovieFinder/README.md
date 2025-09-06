MovieFinder/README.md
# MovieFinder

MovieFinder is a cross-platform desktop application designed to help users manage and search a personal movie collection. Built with C# and Avalonia, it provides a modern, responsive user interface and leverages robust .NET technologies for data management and extensibility.

## Purpose

The purpose of MovieFinder is to offer an easy-to-use tool for cataloging, searching, and managing movie information locally. It is ideal for movie enthusiasts who want to keep track of their collections, including details such as titles, barcodes, and other metadata.

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
