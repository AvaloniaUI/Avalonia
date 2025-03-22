using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ControlCatalog.Pages;

public class DataGridPage : UserControl
{
    private void OnLinkClicked(object? sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://github.com/AvaloniaUI/Avalonia.Controls.DataGrid",
            UseShellExecute = true
        });
    }
}
