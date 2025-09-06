using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using MovieFinder.ViewModels;
using System;
using Avalonia.Media.Imaging;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;

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
                        Title = "Raw Information",
                        Content = new ScrollViewer
                        {
                            Content = new TextBox
                            {
                                Text = json,
                                AcceptsReturn = true,
                                IsReadOnly = true
                            }
                        },
                        Width = 600,
                        Height = 400,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    };
                    jsonWindow.ShowDialog(this);
                };
                vm.CloseRequested = (result) => Close(result);
            }
        };
    }

    private void PosterImage_ImageFailed(object? sender, EventArgs e)
    {
        var errorText = this.FindControl<TextBlock>("PosterErrorText");
        if (errorText != null)
            errorText.IsVisible = true;
    }

    private void BeginMoveDrag(object? sender, PointerPressedEventArgs e)
    {
        BeginMoveDrag(e);
    }

    private void CloseWindow(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }
}
