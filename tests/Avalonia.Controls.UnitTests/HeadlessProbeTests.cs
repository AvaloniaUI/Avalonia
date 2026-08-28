using System;
using System.Linq;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Threading;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Controls.UnitTests;

public class HeadlessProbeTests : ScopedTestBase
{
    [Fact]
    public void Can_Show_Window_And_Lay_Out()
    {
        using var app = HeadlessUnitTestApplication.Start();

        var button = new Button { Width = 100, Height = 30, Content = "hi" };
        var window = new Window { Width = 400, Height = 300, Content = button };
        window.Show();
        app.RunJobs();

        Assert.Equal(new Size(100, 30), button.Bounds.Size);
        Assert.Equal(new Size(400, 300), window.ClientSize);
    }

    [Fact]
    public void Can_Route_Real_Input_Through_Headless_Toplevel()
    {
        using var app = HeadlessUnitTestApplication.Start();

        var clicked = false;
        var button = new Button { Width = 100, Height = 30, Content = "hi" };
        button.Click += (_, _) => clicked = true;
        var window = new Window { Width = 400, Height = 300, Content = button };
        window.Show();
        app.RunJobs();

        var pt = button.Bounds.Center;
        window.MouseDown(pt, MouseButton.Left);
        window.MouseUp(pt, MouseButton.Left);
        app.RunJobs();

        Assert.True(clicked);
    }

    [Fact]
    public void Keyboard_Input_Reaches_Focused_Control()
    {
        using var app = HeadlessUnitTestApplication.Start();

        var box = new TextBox { Width = 200, Height = 30 };
        var window = new Window { Width = 400, Height = 300, Content = box };
        window.Show();
        app.RunJobs();

        box.Focus();
        app.RunJobs();
        window.KeyTextInput("abc");
        app.RunJobs();

        Assert.Equal("abc", box.Text);
    }

    [Fact]
    public void Popup_Opens_As_Real_PopupRoot()
    {
        using var app = HeadlessUnitTestApplication.Start();

        var popup = new Popup { Placement = PlacementMode.Pointer, Child = new Border { Width = 50, Height = 50 } };
        var window = new Window { Width = 400, Height = 300, Content = new Panel { Children = { popup } } };
        window.Show();
        app.RunJobs();

        popup.Open();
        app.RunJobs();

        Assert.IsType<PopupRoot>(popup.Host);
    }

    [Fact]
    public void Popup_Uses_OverlayPopupHost_When_OverlayPopups_Enabled()
    {
        using var app = HeadlessUnitTestApplication.Start(new AvaloniaHeadlessPlatformOptions { OverlayPopups = true });

        var popup = new Popup { Placement = PlacementMode.Pointer, Child = new Border { Width = 50, Height = 50 } };
        var window = new Window { Width = 400, Height = 300, Content = new Panel { Children = { popup } } };
        window.Show();
        app.RunJobs();

        popup.Open();
        app.RunJobs();

        Assert.IsType<OverlayPopupHost>(popup.Host);
    }

    [Fact]
    public void Overlay_Popup_Requires_An_Applied_Window_Template()
    {
        using var app = HeadlessUnitTestApplication.Start(new AvaloniaHeadlessPlatformOptions { OverlayPopups = true });

        // The overlay layer is looked up through the visual tree, so a window that was
        // never shown has nothing to find.
        var untemplated = new Popup { PlacementTarget = new Window() };
        var ex = Assert.Throws<InvalidOperationException>(() => untemplated.Open());
        Assert.Contains("no overlay layer is found", ex.Message);

        var window = new Window();
        window.Show();
        app.RunJobs();
        Assert.NotNull(window.GetVisualDescendants().OfType<VisualLayerManager>().FirstOrDefault());

        var templated = new Popup { PlacementTarget = window };
        templated.Open();
        Assert.IsType<OverlayPopupHost>(templated.Host);
    }

    [Fact]
    public void Platform_Services_Are_Registered_Before_The_First_Window_And_Stay_Stable()
    {
        using var app = HeadlessUnitTestApplication.Start();

        object?[] Resolve() =>
        [
            AvaloniaLocator.Current.GetService<IKeyboardDevice>(),
            AvaloniaLocator.Current.GetService<IPlatformSettings>(),
            AvaloniaLocator.Current.GetService<ICursorFactory>(),
            AvaloniaLocator.Current.GetService<PlatformHotkeyConfiguration>(),
            AvaloniaLocator.Current.GetService<IClipboard>(),
            AvaloniaLocator.Current.GetService<IRenderLoop>(),
            AvaloniaLocator.Current.GetService<IPlatformIconLoader>(),
            AvaloniaLocator.Current.GetService<KeyGestureFormatInfo>(),
        ];

        var before = Resolve();
        Assert.All(before, Assert.NotNull);

        new Window().Show();
        app.RunJobs();

        // Initializing the platform lazily used to replace these mid-test, leaving anything that
        // resolved early holding a different instance than the window does.
        Assert.Equal(before, Resolve(), System.Collections.Generic.ReferenceEqualityComparer.Instance);
    }
}
