using System.Linq;
using Avalonia;
using Avalonia.Controls;

namespace IntegrationTestApp.Pages;

public partial class DesktopPage : UserControl
{
    private int _dockMenuItemCount;

    public DesktopPage()
    {
        InitializeComponent();
    }

    private void ToggleTrayIconVisible_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var icon = TrayIcon.GetIcons(Application.Current!)!.FirstOrDefault()!;
        icon.IsVisible = !icon.IsVisible;
    }

    private void AddDockMenuItem_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var app = (App)Application.Current!;
        _dockMenuItemCount++;
        app.AddDockMenuItem($"Dynamic Item {_dockMenuItemCount}");
        DockMenuItemCount.Text = app.GetDockMenuItemCount().ToString();
    }
}
