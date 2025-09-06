using Avalonia.Controls;
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

    private void OpenDebugWindow(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
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
}
