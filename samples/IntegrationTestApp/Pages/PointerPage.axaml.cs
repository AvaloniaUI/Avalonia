using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace IntegrationTestApp.Pages;

public partial class PointerPage : UserControl
{
    public PointerPage()
    {
        InitializeComponent();
    }

    private void PointerPageShowDialog_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        void CaptureLost(object? sender, PointerCaptureLostEventArgs e)
        {
            PointerCaptureStatus.Text = "None";
            ((Control)sender!).PointerCaptureLost -= CaptureLost;
        }

        var window = TopLevel.GetTopLevel(this) as Window ??
            throw new AvaloniaInternalException("PointerPage is not attached to a Window.");
        var captured = e.Pointer.Captured as Control;

        if (captured is not null)
        {
            captured.PointerCaptureLost += CaptureLost;
        }

        PointerCaptureStatus.Text = captured?.ToString() ?? "None";

        var dialog = new Window
        {
            Width = 200,
            Height = 200,
        };

        dialog.Content = new Button
        {
            Content = "Close",
            Command = new DelegateCommand(() => dialog.Close()),
        };

        dialog.ShowDialog(window);
    }
}
