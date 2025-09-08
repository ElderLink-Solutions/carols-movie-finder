using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MovieFinder.Services;
using MovieFinder.ViewModels;
using MovieFinder.Views;
using System;
using System.IO;

namespace MovieFinder;

public partial class App : Application
{
    public static IServiceProvider? Services { get; private set; }
    public static Window? CurrentMainWindow { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();

        var dbPath = configuration["DB_STORAGE"];
        if (string.IsNullOrEmpty(dbPath))
        {
            throw new Exception("DB_STORAGE not configured in appsettings.json");
        }

        var dbDirectory = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(dbDirectory))
        {
            Directory.CreateDirectory(dbDirectory);
        }

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<IConfiguration>(configuration);
        serviceCollection.AddSingleton<IAppLogger, AppLogger>();
        serviceCollection.AddSingleton<IShutdownService, ShutdownService>();
        serviceCollection.AddSingleton<Database>(sp => new Database(dbPath, sp.GetRequiredService<IAppLogger>()));

        // Add logging services
        serviceCollection.AddLogging(configure => configure.AddConsole());

        // Determine BarcodeService based on ENVIRONMENT setting
        var mode = configuration["MODE"];
        Console.WriteLine("mode: " + mode);
        if (mode?.ToLower() == "libusb")
        {
            serviceCollection.AddSingleton<IBarcodeService, BarcodeService>();
            Console.WriteLine("Using LibUsbBarcodeService mode.");
        }
        else if (mode?.ToLower() == "keyboardwedge")
        {
            serviceCollection.AddSingleton<IBarcodeService, KeyboardWedgeBarcodeService>();
            Console.WriteLine("Using KeyboardWedgeBarcodeService mode.");
        }
        else
        {
            // Fallback or throw an error if ENVIRONMENT is not recognized
            serviceCollection.AddSingleton<IBarcodeService, BarcodeService>();
            Console.WriteLine("Using LibUsbBarcodeService mode by default.");
        }

        serviceCollection.AddSingleton<MovieService>(sp =>
            new MovieService(
                sp.GetRequiredService<IConfiguration>(),
                sp.GetRequiredService<IAppLogger>()
            )
        );
        serviceCollection.AddSingleton<PosterService>(sp =>
            new PosterService(
                sp.GetRequiredService<IAppLogger>()
            )
        );
        serviceCollection.AddTransient<MainWindowViewModel>(sp =>
            new MainWindowViewModel(
                sp.GetRequiredService<Database>(),
                sp.GetRequiredService<IBarcodeService>(),
                sp.GetRequiredService<MovieService>(),
                sp.GetRequiredService<IAppLogger>(),
                sp.GetRequiredService<PosterService>()
            )
        );

        Services = serviceCollection.BuildServiceProvider();

        var logger = Services.GetRequiredService<IAppLogger>();
        // The following line is no longer needed as AddConsole() handles console output for ILogger
        // if (logger is AppLogger appLogger)
        // {
        //     appLogger.Initialize(Console.WriteLine);
        // }

        logger.Log("=== MovieFinder Program.cs: Main starting ===");

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainWindowViewModel>()
            };
            CurrentMainWindow = desktop.MainWindow;

            desktop.Exit += async (sender, e) =>
            {
                var shutdownService = Services.GetRequiredService<IShutdownService>();
                var logger = Services.GetRequiredService<IAppLogger>();

                logger.Log("Desktop exit event raised.");

                logger.Log("Sending shutdown signal");
                shutdownService.RequestShutdown();

                await shutdownService.WaitForShutdownAsync(TimeSpan.FromSeconds(5));

                logger.Log("=== MovieFinder Program.cs: Main completed successfully ===");
                (Services.GetRequiredService<IAppLogger>() as IDisposable)?.Dispose();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
