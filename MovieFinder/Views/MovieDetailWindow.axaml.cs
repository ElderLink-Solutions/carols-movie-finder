using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MovieFinder.ViewModels;

namespace MovieFinder.Views;

public partial class MovieDetailWindow : Window
{
    public MovieDetailWindow()
    {
        InitializeComponent();
        this.DataContextChanged += (sender, args) =>
        {
            if (DataContext is MovieDetailWindowViewModel vm)
            {
                vm.CloseRequested = (result) => Close(result);
            };
        };
    }
}
