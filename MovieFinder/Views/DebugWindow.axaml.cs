using Avalonia.Controls;
// Removed using MovieFinder.Services; // AppLogger is no longer initialized here
// Removed using MovieFinder.ViewModels; // DebugWindowViewModel is no longer directly used for AppLogger init here

namespace MovieFinder.Views;

public partial class DebugWindow : Window
{
    public DebugWindow()
    {
        InitializeComponent();
        // AppLogger initialization moved to MainWindow.axaml.cs
    }
}