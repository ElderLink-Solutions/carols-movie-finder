using Avalonia.Controls;
using Avalonia.Interactivity;
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
            var debugWindow = new DebugWindow
            {
                DataContext = new DebugWindowViewModel(vm.BarcodeService)
            };
            debugWindow.Show();
        }
    }
}
