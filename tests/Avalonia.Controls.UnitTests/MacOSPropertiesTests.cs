using System;
using Avalonia.Controls.Platform;
using Avalonia.UnitTests;
using Moq;
using Xunit;

namespace Avalonia.Controls.UnitTests;

public class MacOSPropertiesTests : ScopedTestBase
{
    [Fact]
    public void TrafficLightPosition_Defaults_To_System()
    {
        var window = new Window(MockWindowingPlatform.CreateWindowMock().Object);

        Assert.Null(MacOSProperties.GetTrafficLightPosition(window));
    }

    [Fact]
    public void TrafficLightPosition_Is_Forwarded_To_The_Platform()
    {
        var windowImpl = MockWindowingPlatform.CreateWindowMock();
        var macOSOptions = windowImpl.As<IMacOSOptionsTopLevelImpl>();
        var window = new Window(windowImpl.Object);
        var position = new Point(50, 40);

        MacOSProperties.SetTrafficLightPosition(window, position);
        MacOSProperties.SetTrafficLightPosition(window, null);

        macOSOptions.Verify(x => x.SetTrafficLightPosition(position), Times.Once);
        macOSOptions.Verify(x => x.SetTrafficLightPosition(null), Times.Once);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(double.NaN, 0)]
    [InlineData(0, double.PositiveInfinity)]
    public void TrafficLightPosition_Rejects_Invalid_Coordinates(double x, double y)
    {
        var window = new Window(MockWindowingPlatform.CreateWindowMock().Object);

        Assert.Throws<ArgumentException>(() => MacOSProperties.SetTrafficLightPosition(window, new Point(x, y)));
    }
}
