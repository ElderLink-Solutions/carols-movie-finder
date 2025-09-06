using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MovieFinder.Services;
using MovieFinder.ViewModels;
using MovieFinder.Views;
using System;
using System.IO;

namespace MovieFinder;

public partial class App : Application
{
    public static IServiceProvider? Services { get; private set; }

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
        serviceCollection.AddSingleton<Database>(new Database(dbPath));
        serviceCollection.AddSingleton<BarcodeService>(sp =>
            new BarcodeService(
                sp.GetRequiredService<IAppLogger>(),
                sp.GetRequiredService<IConfiguration>()
            )
        );
        serviceCollection.AddSingleton<MovieService>();
        serviceCollection.AddTransient<MainWindowViewModel>(sp => 
            new MainWindowViewModel(
                sp.GetRequiredService<Database>(), 
                sp.GetRequiredService<BarcodeService>(),
                sp.GetRequiredService<MovieService>(),
                sp.GetRequiredService<IAppLogger>()
            )
        );

        Services = serviceCollection.BuildServiceProvider();

        var logger = Services.GetRequiredService<IAppLogger>();
        if (logger is AppLogger appLogger)
        {
            appLogger.Initialize(Console.WriteLine);
        }

        logger.Log("=== MovieFinder Program.cs: Main starting ===");

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainWindowViewModel>()
            };

            desktop.Exit += async (sender, e) =>
            {
                var barcodeService = Services.GetRequiredService<BarcodeService>();
                await barcodeService.StopReadingBarcodesAsync();
                logger.Log("=== MovieFinder Program.cs: Main completed successfully ===");
                (Services.GetRequiredService<IAppLogger>() as IDisposable)?.Dispose();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
