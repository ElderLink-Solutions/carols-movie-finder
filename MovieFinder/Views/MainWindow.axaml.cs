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
    public MainWindow()
    {
        InitializeComponent();
    }

    // Handler for clicking on a movie item to copy its info and show notification
    public async void MovieItem_PointerReleased(object? sender, Avalonia.Input.PointerReleasedEventArgs e)
    {
        if (DataContext is MovieFinder.ViewModels.MainWindowViewModel vm && sender is Avalonia.Controls.ListBox listBox)
        {
            if (listBox.SelectedItem is MovieFinder.Models.Movie movie)
            {
                // Copy movie title (or customize as needed)
                var textToCopy = $"{movie.Title} ({movie.Year})";
                if (this.Clipboard != null)
                {
                    await this.Clipboard.SetTextAsync(textToCopy);
                }

                // Show notification
                vm.CopiedNotification = "Copied!";
                vm.CopiedNotificationVisible = true;
                await System.Threading.Tasks.Task.Delay(1200);
                vm.CopiedNotification = "";
                vm.CopiedNotificationVisible = false;
            }
        }
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

    private void OpenDebugWindow(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm && vm.BarcodeService is not null)
        {
            var logger = App.Services?.GetRequiredService<IAppLogger>();
            var debugWindowViewModel = new DebugWindowViewModel(vm.BarcodeService, logger);
            var debugWindow = new DebugWindow
            {
                DataContext = debugWindowViewModel
            };
            logger?.Log("Debug Window Initialized.");
            debugWindow.Show();
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
        Close();
    }
}
