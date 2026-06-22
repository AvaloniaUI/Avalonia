using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ControlCatalog.Pages;

public partial class AvaloniaFlixHomeView : UserControl
{
    public Action<string>? MovieSelected { get; set; }
    public Action? SearchRequested { get; set; }

    public AvaloniaFlixHomeView() => InitializeComponent();

    void OnMovieClick(object? sender, RoutedEventArgs e)
    {
        string title = "Cyber Dune";
        if (sender is Button btn && btn.Tag is string tag)
            title = tag;
        MovieSelected?.Invoke(title);
    }
}
