using Avalonia.Controls;
using Avalonia.Input;
using MovieFinder.ViewModels;

namespace MovieFinder.Views;

public partial class AddMovieFormWindow : Window
{
    public AddMovieFormWindow()
    {
        InitializeComponent();
        this.DataContextChanged += (sender, args) =>
        {
            if (DataContext is MovieAddMovieFormViewModel vm)
            {
                vm.CloseRequested = (result) => Close(result);
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
