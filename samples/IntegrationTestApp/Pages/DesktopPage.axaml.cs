using System.Linq;
using Avalonia;
using Avalonia.Controls;

namespace IntegrationTestApp.Pages;

public partial class DesktopPage : UserControl
{
    public DesktopPage()
    {
        InitializeComponent();
    }

    private void ToggleTrayIconVisible_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var icon = TrayIcon.GetIcons(Application.Current!)!.FirstOrDefault()!;
        icon.IsVisible = !icon.IsVisible;
    }
}
