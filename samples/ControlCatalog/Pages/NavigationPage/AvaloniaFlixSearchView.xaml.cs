using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace ControlCatalog.Pages;

public partial class AvaloniaFlixSearchView : UserControl
{
    public Action? CloseRequested { get; set; }
    public Action<string>? MovieSelected { get; set; }

    public AvaloniaFlixSearchView() => InitializeComponent();

    void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        if (CloseRequested != null)
        {
            CloseRequested();
        }
        else
        {
            var nav = this.FindAncestorOfType<NavigationPage>();
            _ = nav?.PopModalAsync() ?? Task.CompletedTask;
        }
    }

    void OnMovieClick(object? sender, RoutedEventArgs e)
    {
        string title = "Neon Horizon";
        if (sender is Button btn && btn.Tag is string tag)
            title = tag;
        MovieSelected?.Invoke(title);
    }
}
