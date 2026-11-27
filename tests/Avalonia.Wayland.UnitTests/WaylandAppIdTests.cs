using Avalonia;
using Xunit;

namespace Avalonia.Wayland.UnitTests;

public class WaylandAppIdTests
{
    [Fact]
    public void WaylandPlatformOptions_CanSetAppId()
    {
        var options = new WaylandPlatformOptions
        {
            AppId = "org.avaloniaui.test"
        };

        Assert.Equal("org.avaloniaui.test", options.AppId);
    }
}
