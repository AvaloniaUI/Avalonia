using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;

namespace IntegrationTestApp.Pages;

public partial class WindowDecorationsPage : UserControl
{
    public WindowDecorationsPage()
    {
        InitializeComponent();
    }

    private void SetWindowDecorations(Window window)
    {
        window.ExtendClientAreaToDecorationsHint = WindowExtendClientAreaToDecorationsHint.IsChecked!.Value;
        window.ExtendClientAreaTitleBarHeightHint =
            int.TryParse(WindowTitleBarHeightHint.Text, out var val) ? val / window.DesktopScaling : -1;
        window.ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.NoChrome
            | (WindowForceSystemChrome.IsChecked == true ? ExtendClientAreaChromeHints.SystemChrome : 0)
            | (WindowPreferSystemChrome.IsChecked == true ? ExtendClientAreaChromeHints.PreferSystemChrome : 0)
            | (WindowMacThickSystemChrome.IsChecked == true ? ExtendClientAreaChromeHints.OSXThickTitleBar : 0);
        AdjustOffsets(window);

        window.Background = Brushes.Transparent;
        window.PropertyChanged += WindowOnPropertyChanged;

        void WindowOnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            var window = (Window)sender!;
            if (e.Property == Window.OffScreenMarginProperty || e.Property == Window.WindowDecorationMarginProperty)
            {
                AdjustOffsets(window);
            }
        }

        void AdjustOffsets(Window window)
        {
            var scaling = window.DesktopScaling;

            window.Padding = window.OffScreenMargin;
            ((Control)window.Content!).Margin = window.WindowDecorationMargin;

            WindowDecorationProperties.Text =
                $"{window.OffScreenMargin.Top * scaling} {window.WindowDecorationMargin.Top * scaling} {window.IsExtendedIntoWindowDecorations}";
        }
    }

    private void ApplyWindowDecorations_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var window = TopLevel.GetTopLevel(this) as Window ??
            throw new AvaloniaInternalException("WindowDecorationsPage is not attached to a Window.");
        SetWindowDecorations(window);
    }

    private void ShowNewWindowDecorations_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var window = new ShowWindowTest();
        SetWindowDecorations(window);
        window.Show();
    }
}
