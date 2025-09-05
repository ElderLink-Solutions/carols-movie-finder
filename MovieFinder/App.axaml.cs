using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using MovieFinder.Services;
using MovieFinder.ViewModels;
using MovieFinder.Views;
using System;
using System.IO;

namespace MovieFinder;

public partial class App : Application
{
    public static Database? Database { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Get the folder for local application data.
        var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dbFolder = Path.Combine(appDataFolder, "MovieFinder");
        Directory.CreateDirectory(dbFolder);
        var dbPath = Path.Combine(dbFolder, "movies.db3");
        Database = new Database(dbPath);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(Database)
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}