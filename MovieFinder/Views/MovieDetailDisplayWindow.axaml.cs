using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using MovieFinder.ViewModels;
using System; 
using Avalonia.Media; // Add this

namespace MovieFinder.Views;

public partial class MovieDetailDisplayWindow : Window
{
    public MovieDetailDisplayWindow()
    {
        InitializeComponent();
        this.DataContextChanged += (sender, args) =>
        {
            if (DataContext is MovieDetailWindowViewModel vm)
            {
                vm.OpenFullJsonDetailsRequested = (json) =>
                {
                    var jsonWindow = new Window
                    {
                        Title = "Full JSON Output",
                        Content = new ScrollViewer
                        {
                            Content = new TextBlock { Text = json, TextWrapping = TextWrapping.Wrap }
                        },
                        Width = 600,
                        Height = 400,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    };
                    jsonWindow.ShowDialog(this);
                };
                vm.CloseRequested = () => Close(); 
            }
        };
    }

    // Allows the window to be dragged from the title bar area
    private void BeginMoveDrag(object? sender, PointerPressedEventArgs e)
    {
        BeginMoveDrag(e);
    }
    
    // The Close button in the custom title bar can just call the window's Close method
    private void CloseWindow(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }
}