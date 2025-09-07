using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Microsoft.Extensions.DependencyInjection;
using MovieFinder.Services;
using MovieFinder.ViewModels;
using System;

namespace MovieFinder.Views;

public partial class MainWindow : Window
{
    private Image? _splashImage;

    public MainWindow()
    {
        InitializeComponent();

        // Set the window icon to favicon.ico
        try
        {
            this.Icon = new WindowIcon("favicon.ico");
        }
        catch (Exception ex)
        {
            var logger = App.Services?.GetRequiredService<IAppLogger>();
            logger?.Log($"Failed to set window icon: {ex.Message}");
        }

        var logger2 = App.Services?.GetRequiredService<IAppLogger>();
        logger2?.Log("MainWindow created.");

        this.Closing += (s, e) => logger2?.Log("MainWindow closing.");
        this.Closed += (s, e) => logger2?.Log("MainWindow closed.");

        // Hide splash image after window is loaded
        this.Opened += (_, __) =>
        {
            _splashImage = this.FindControl<Image>("SplashImage");
            if (_splashImage != null)
            {
                _splashImage.IsVisible = false;
            }
        };
    }

    public async void LogItem_PointerReleased(object? sender, Avalonia.Input.PointerReleasedEventArgs e)
    {
        if (sender is Avalonia.Controls.ListBox listBox && listBox.SelectedItem is string logMessage)
        {
            if (this.Clipboard != null)
            {
                await this.Clipboard.SetTextAsync(logMessage);
            }
        }
    }

    public void BeginMoveDrag(object? sender, PointerPressedEventArgs e)
    {
        BeginMoveDrag(e);
    }

    public void MinimizeWindow(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    public void MaximizeWindow(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    public void CloseWindow(object? sender, RoutedEventArgs e)
    {
        var logger = App.Services?.GetRequiredService<IAppLogger>();
        logger?.Log("Closing Application.");
        Close();
    }
}
