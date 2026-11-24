using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ControlCatalog.Pages;

public partial class PointersPage : ContentPage
{
    public PointersPage()
    {
        InitializeComponent();
    }

    private void Clear_Click(object? sender, RoutedEventArgs e) => Canvas.Clear();
}
