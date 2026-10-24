using System;
using Avalonia.Controls.Platform;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Moq;
using Xunit;

namespace Avalonia.Controls.UnitTests;

public class MacOSPropertiesTests : ScopedTestBase
{
    private readonly Mock<IWindowImpl> _windowImpl;
    private readonly Mock<IMacOSOptionsTopLevelImpl> _macOS;
    private readonly IDisposable _app;

    public MacOSPropertiesTests()
    {
        _windowImpl = MockWindowingPlatform.CreateWindowMock();
        _macOS = _windowImpl.As<IMacOSOptionsTopLevelImpl>();
        _macOS.Setup(x => x.TrafficLightSize).Returns(new Size(54, 16));
        _macOS.SetupProperty(x => x.TrafficLightSizeChanged);
        _app = UnitTestApplication.Start(TestServices.StyledWindow.With(
            windowingPlatform: new MockWindowingPlatform(() => _windowImpl.Object)));
    }

    public override void Dispose()
    {
        _app.Dispose();
        base.Dispose();
    }

    [Fact]
    public void Window_Should_Report_Traffic_Light_Size()
    {
        var window = new Window();

        Assert.Equal(new Size(54, 16), MacOSProperties.GetTrafficLightSize(window));
    }

    [Fact]
    public void Traffic_Light_Size_Should_Follow_Platform_Changes()
    {
        var window = new Window();

        _macOS.Object.TrafficLightSizeChanged!(new Size(60, 20));

        Assert.Equal(new Size(60, 20), MacOSProperties.GetTrafficLightSize(window));
    }

    [Fact]
    public void Host_Without_Size_Should_Fit_Traffic_Lights()
    {
        var host = new Border();
        var window = CreateWindow(new Canvas { Children = { host } });
        MacOSProperties.SetIsTrafficLightHost(host, true);

        window.Show();

        Assert.Equal(new Size(54, 16), host.Bounds.Size);
    }

    [Fact]
    public void Host_Should_Follow_Traffic_Light_Size_Changes()
    {
        var host = new Border();
        var window = CreateWindow(new Canvas { Children = { host } });
        MacOSProperties.SetIsTrafficLightHost(host, true);
        window.Show();

        _macOS.Object.TrafficLightSizeChanged!(new Size(60, 20));
        window.LayoutManager.ExecuteLayoutPass();

        Assert.Equal(new Size(60, 20), host.Bounds.Size);
    }

    [Fact]
    public void Local_MinWidth_Should_Replace_Traffic_Light_Width()
    {
        var host = new Border { MinWidth = 0, Width = 20 };
        var window = CreateWindow(new Canvas { Children = { host } });
        MacOSProperties.SetIsTrafficLightHost(host, true);

        window.Show();

        Assert.Equal(20, host.Bounds.Width);
    }

    [Fact]
    public void Style_Should_Not_Replace_Traffic_Light_Width()
    {
        var host = new Border { Width = 20 };
        var window = CreateWindow(new Canvas { Children = { host } });
        window.Styles.Add(new Style(x => x.OfType<Border>())
        {
            Setters = { new Setter(Layoutable.MinWidthProperty, 0d) }
        });
        MacOSProperties.SetIsTrafficLightHost(host, true);

        window.Show();

        Assert.Equal(54, host.Bounds.Width);
    }

    [Theory]
    [InlineData(100, 40, 33, 32)]
    [InlineData(100, 16, 33, 20)]
    [InlineData(54, 40, 10, 32)]
    [InlineData(54, 16, 10, 20)]
    public void Traffic_Lights_Should_Be_Centered_In_Host(double width, double height, double x, double y)
    {
        var host = new Border { Width = width, Height = height };
        var window = CreateWindow(CreateCanvas(host, 10, 20));
        MacOSProperties.SetIsTrafficLightHost(host, true);

        window.Show();

        _macOS.Verify(m => m.SetTrafficLightPosition(new Point(x, y)));
    }

    [Fact]
    public void Traffic_Lights_Should_Align_To_Top_Leading_Corner_Of_Smaller_Host()
    {
        var host = new Border { MinWidth = 0, MinHeight = 0, Width = 20, Height = 10 };
        var window = CreateWindow(CreateCanvas(host, 10, 20));
        MacOSProperties.SetIsTrafficLightHost(host, true);

        window.Show();

        _macOS.Verify(m => m.SetTrafficLightPosition(new Point(10, 20)));
    }

    [Fact]
    public void Traffic_Lights_Should_Follow_Host_Position()
    {
        var host = new Border { Width = 100, Height = 40 };
        var window = CreateWindow(CreateCanvas(host, 10, 20));
        MacOSProperties.SetIsTrafficLightHost(host, true);
        window.Show();

        Canvas.SetLeft(host, 50);
        window.LayoutManager.ExecuteLayoutPass();

        _macOS.Verify(m => m.SetTrafficLightPosition(new Point(73, 32)));
    }

    [Theory]
    [InlineData(FlowDirection.LeftToRight, FlowDirection.LeftToRight, 33)]
    [InlineData(FlowDirection.RightToLeft, FlowDirection.LeftToRight, 313)]
    [InlineData(FlowDirection.LeftToRight, FlowDirection.RightToLeft, 313)]
    [InlineData(FlowDirection.RightToLeft, FlowDirection.RightToLeft, 33)]
    public void Traffic_Lights_Should_Use_Titlebar_Direction_Independently_Of_Content_Direction(
        FlowDirection titlebarDirection, FlowDirection contentDirection, double x)
    {
        _macOS.Setup(m => m.TitlebarFlowDirection).Returns(titlebarDirection);
        var host = new Border { Width = 100, Height = 40 };
        var canvas = CreateCanvas(host, 10, 20);
        canvas.FlowDirection = contentDirection;
        var window = CreateWindow(canvas);
        MacOSProperties.SetIsTrafficLightHost(host, true);

        window.Show();

        _macOS.Verify(m => m.SetTrafficLightPosition(new Point(x, 32)));
    }

    [Fact]
    public void First_Visible_Host_Should_Receive_Traffic_Lights()
    {
        var hiddenHost = new Border { Width = 100, Height = 40, IsVisible = false };
        var visibleHost = new Border { Width = 100, Height = 40 };
        var canvas = CreateCanvas(hiddenHost, 10, 20);
        canvas.Children.Add(visibleHost);
        Canvas.SetLeft(visibleHost, 200);
        Canvas.SetTop(visibleHost, 20);
        var window = CreateWindow(canvas);
        MacOSProperties.SetIsTrafficLightHost(hiddenHost, true);
        MacOSProperties.SetIsTrafficLightHost(visibleHost, true);

        window.Show();

        _macOS.Verify(m => m.SetTrafficLightPosition(new Point(223, 32)));
    }

    [Fact]
    public void Hiding_Host_Should_Restore_System_Layout()
    {
        var host = new Border { Width = 100, Height = 40 };
        var window = CreateWindow(CreateCanvas(host, 10, 20));
        MacOSProperties.SetIsTrafficLightHost(host, true);
        window.Show();

        host.IsVisible = false;
        window.LayoutManager.ExecuteLayoutPass();

        _macOS.Verify(m => m.SetTrafficLightPosition(null));
    }

    [Fact]
    public void Removing_Host_Should_Restore_System_Layout()
    {
        var host = new Border { Width = 100, Height = 40 };
        var canvas = CreateCanvas(host, 10, 20);
        var window = CreateWindow(canvas);
        MacOSProperties.SetIsTrafficLightHost(host, true);
        window.Show();

        canvas.Children.Remove(host);

        _macOS.Verify(m => m.SetTrafficLightPosition(null));
    }

    [Fact]
    public void Clearing_IsTrafficLightHost_Should_Restore_System_Layout()
    {
        var host = new Border { Width = 100, Height = 40 };
        var window = CreateWindow(CreateCanvas(host, 10, 20));
        MacOSProperties.SetIsTrafficLightHost(host, true);
        window.Show();

        MacOSProperties.SetIsTrafficLightHost(host, false);

        _macOS.Verify(m => m.SetTrafficLightPosition(null));
    }

    [Fact]
    public void Clearing_IsTrafficLightHost_Should_Restore_Host_Size()
    {
        var host = new Border();
        var window = CreateWindow(new Canvas { Children = { host } });
        MacOSProperties.SetIsTrafficLightHost(host, true);
        window.Show();

        MacOSProperties.SetIsTrafficLightHost(host, false);
        window.LayoutManager.ExecuteLayoutPass();

        Assert.Equal(default, host.Bounds.Size);
    }

    [Fact]
    public void Zero_Traffic_Light_Size_Should_Restore_System_Layout()
    {
        var host = new Border { Width = 100, Height = 40 };
        var window = CreateWindow(CreateCanvas(host, 10, 20));
        MacOSProperties.SetIsTrafficLightHost(host, true);
        window.Show();

        _macOS.Object.TrafficLightSizeChanged!(default);

        _macOS.Verify(m => m.SetTrafficLightPosition(null));
    }

    [Fact]
    public void Unsupported_Platform_Should_Ignore_Host()
    {
        var host = new Border();
        var window = new Window(MockWindowingPlatform.CreateWindowMock().Object)
        {
            Content = new Canvas { Children = { host } }
        };
        MacOSProperties.SetIsTrafficLightHost(host, true);

        window.Show();

        Assert.Equal(default, host.Bounds.Size);
    }

    private static Window CreateWindow(Control content) => new()
    {
        Width = 400,
        Height = 200,
        ExtendClientAreaToDecorationsHint = true,
        Content = content
    };

    private static Canvas CreateCanvas(Control host, double left, double top)
    {
        Canvas.SetLeft(host, left);
        Canvas.SetTop(host, top);
        return new Canvas { Children = { host } };
    }
}
