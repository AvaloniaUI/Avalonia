using System;
using Avalonia.Input;
using Avalonia.Platform;
using Avalonia.UnitTests;
using Moq;
using Xunit;

namespace Avalonia.Controls.UnitTests.Platform;

public class TitleBarDoubleClickHelperTests : ScopedTestBase
{
    [Theory]
    [InlineData(250, 10, 10, true)]
    [InlineData(300, 10, 10, true)]
    [InlineData(301, 10, 10, false)]
    [InlineData(250, 11, 12, true)]
    [InlineData(250, 13, 10, false)]
    [InlineData(250, 10, 15, false)]
    public void IsDoubleClick_Respects_Platform_Time_And_Distance(
        int timestamp, double x, double y, bool expected)
    {
        var settings = new Mock<IPlatformSettings>();
        settings.Setup(s => s.GetDoubleTapTime(PointerType.Mouse)).Returns(TimeSpan.FromMilliseconds(200));
        settings.Setup(s => s.GetDoubleTapSize(PointerType.Mouse)).Returns(new Size(4, 8));
        AvaloniaLocator.CurrentMutable.Bind<IPlatformSettings>().ToConstant(settings.Object);
        var helper = new TitleBarDoubleClickHelper();

        Assert.False(helper.IsDoubleClick(100, new Point(10, 10)));
        Assert.Equal(expected, helper.IsDoubleClick((ulong)timestamp, new Point(x, y)));
    }

    [Fact]
    public void IsDoubleClick_Third_Click_Is_Not_A_Double_Click()
    {
        var helper = new TitleBarDoubleClickHelper();
        var point = new Point(10, 10);

        Assert.False(helper.IsDoubleClick(100, point));
        Assert.True(helper.IsDoubleClick(200, point));
        Assert.False(helper.IsDoubleClick(300, point));
    }

    [Fact]
    public void IsDoubleClick_After_Reset_Starts_Over()
    {
        var helper = new TitleBarDoubleClickHelper();
        var point = new Point(10, 10);

        Assert.False(helper.IsDoubleClick(100, point));
        helper.Reset();
        Assert.False(helper.IsDoubleClick(200, point));
        Assert.True(helper.IsDoubleClick(300, point));
    }

    [Theory]
    [InlineData(WindowState.Normal, true, true, WindowState.Maximized)]
    [InlineData(WindowState.Normal, true, false, WindowState.Maximized)]
    [InlineData(WindowState.Normal, false, true, null)]
    [InlineData(WindowState.Normal, false, false, null)]
    [InlineData(WindowState.Maximized, true, true, WindowState.Normal)]
    [InlineData(WindowState.Maximized, false, true, WindowState.Normal)]
    [InlineData(WindowState.Maximized, true, false, null)]
    [InlineData(WindowState.FullScreen, true, true, WindowState.Normal)]
    [InlineData(WindowState.FullScreen, true, false, null)]
    [InlineData(WindowState.Minimized, true, true, null)]
    public void GetStateAfterDoubleClick_Toggles_Only_Allowed_States(
        WindowState current, bool canMaximize, bool canResize, WindowState? expected)
    {
        Assert.Equal(expected, TitleBarDoubleClickHelper.GetStateAfterDoubleClick(current, canMaximize, canResize));
    }
}
