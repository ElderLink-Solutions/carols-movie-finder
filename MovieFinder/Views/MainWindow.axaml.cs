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