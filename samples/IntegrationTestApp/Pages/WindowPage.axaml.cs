using System.Linq;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace IntegrationTestApp.Pages;

public partial class WindowPage : UserControl
{
    public WindowPage()
    {
        InitializeComponent();
    }

    private Window Window => TopLevel.GetTopLevel(this) as Window ??
        throw new AvaloniaInternalException("WindowPage is not attached to a Window.");

    private void ShowWindow_Click(object? sender, RoutedEventArgs e)
    {
        var size = !string.IsNullOrWhiteSpace(ShowWindowSize.Text) ? Size.Parse(ShowWindowSize.Text) : (Size?)null;
        var window = new ShowWindowTest
        {
            WindowStartupLocation = (WindowStartupLocation)ShowWindowLocation.SelectedIndex,
            CanResize = ShowWindowCanResize.IsChecked ?? false,
        };

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            // Make sure the windows have unique names and AutomationIds.
            var existing = lifetime.Windows.OfType<ShowWindowTest>().Count();
            if (existing > 0)
            {
                AutomationProperties.SetAutomationId(window, window.Name + (existing + 1));
                window.Title += $" {existing + 1}";
            }
        }

        if (size.HasValue)
        {
            window.Width = size.Value.Width;
            window.Height = size.Value.Height;
        }

        ShowWindowSize.Text = string.Empty;
        window.ExtendClientAreaToDecorationsHint = ShowWindowExtendClientAreaToDecorationsHint.IsChecked ?? false;
        window.SystemDecorations = (SystemDecorations)ShowWindowSystemDecorations.SelectedIndex;
        window.WindowState = (WindowState)ShowWindowState.SelectedIndex;

        switch (ShowWindowMode.SelectedIndex)
        {
            case 0:
                window.Show();
                break;
            case 1:
                window.Show(Window);
                break;
            case 2:
                window.ShowDialog(Window);
                break;
        }
    }

    private void ShowTransparentWindow_Click(object? sender, RoutedEventArgs e)
    {
        // Show a background window to make sure the color behind the transparent window is
        // a known color (green).
        var backgroundWindow = new Window
        {
            Title = "Transparent Window Background",
            Name = "TransparentWindowBackground",
            Width = 300,
            Height = 300,
            Background = Brushes.Green,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };

        // This is the transparent window with a red circle.
        var window = new Window
        {
            Title = "Transparent Window",
            Name = "TransparentWindow",
            SystemDecorations = SystemDecorations.None,
            Background = Brushes.Transparent,
            TransparencyLevelHint = new[] { WindowTransparencyLevel.Transparent },
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Width = 200,
            Height = 200,
            Content = new Border
            {
                Background = Brushes.Red,
                CornerRadius = new CornerRadius(100),
            }
        };

        window.PointerPressed += (_, _) =>
        {
            window.Close();
            backgroundWindow.Close();
        };

        backgroundWindow.Show(Window);
        window.Show(backgroundWindow);
    }

    private void ShowTransparentPopup_Click(object? sender, RoutedEventArgs e)
    {
        var popup = new Popup
        {
            WindowManagerAddShadowHint = false,
            Placement = PlacementMode.AnchorAndGravity,
            PlacementAnchor = PopupAnchor.Top,
            PlacementGravity = PopupGravity.Bottom,
            Width = 200,
            Height = 200,
            Child = new Border
            {
                Background = Brushes.Red,
                CornerRadius = new CornerRadius(100),
            }
        };

        // Show a background window to make sure the color behind the transparent window is
        // a known color (green).
        var backgroundWindow = new Window
        {
            Title = "Transparent Popup Background",
            Name = "TransparentPopupBackground",
            Width = 200,
            Height = 200,
            Background = Brushes.Green,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new Border
            {
                Name = "PopupContainer",
                Child = popup,
                [AutomationProperties.AccessibilityViewProperty] = AccessibilityView.Content,
            }
        };

        backgroundWindow.PointerPressed += (_, _) => backgroundWindow.Close();
        backgroundWindow.Show(Window);

        popup.Open();
    }

    private void SendToBack_Click(object? sender, RoutedEventArgs e)
    {
        var lifetime = (ClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!;

        foreach (var window in lifetime.Windows.ToArray())
        {
            window.Activate();
        }
    }

    private void EnterFullscreen_Click(object? sender, RoutedEventArgs e)
    {
        Window.WindowState = WindowState.FullScreen;
    }

    private void ExitFullscreen_Click(object? sender, RoutedEventArgs e)
    {
        Window.WindowState = WindowState.Normal;
    }

    private void RestoreAll_Click(object? sender, RoutedEventArgs e)
    {
        var lifetime = (ClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!;

        foreach (var window in lifetime.Windows.ToArray())
        {
            window.Show();
            if (window.WindowState == WindowState.Minimized)
                window.WindowState = WindowState.Normal;
        }
    }
}
