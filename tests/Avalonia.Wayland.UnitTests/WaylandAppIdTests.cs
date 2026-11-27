using System;
using Xunit;

namespace Avalonia.Wayland.UnitTests;

[CollectionDefinition("Wayland environment", DisableParallelization = true)]
public class WaylandEnvironmentCollection;

[Collection("Wayland environment")]
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

    [Fact]
    public void UbuntuTouch_Uses_Click_Identity_Only_When_AppId_Is_Not_Set()
    {
        var previous = Environment.GetEnvironmentVariable("APP_ID");
        try
        {
            Environment.SetEnvironmentVariable("APP_ID", "avalonia.inputdemo_inputdemo_1.0");
            var automatic = new WaylandPlatformOptions { UseUbuntuTouch = true };
            var explicitId = new WaylandPlatformOptions { UseUbuntuTouch = true, AppId = "org.avaloniaui.test" };
            var disabled = new WaylandPlatformOptions { UseUbuntuTouch = true, AppId = null };
            var desktop = new WaylandPlatformOptions { UseUbuntuTouch = false };

            Assert.Equal("avalonia.inputdemo_inputdemo_1.0", automatic.EffectiveAppId);
            Assert.Equal("org.avaloniaui.test", explicitId.EffectiveAppId);
            Assert.Null(disabled.EffectiveAppId);
            Assert.Equal(desktop.AppId, desktop.EffectiveAppId);
            Assert.NotEqual(automatic.EffectiveAppId, desktop.EffectiveAppId);

            Environment.SetEnvironmentVariable("APP_ID", null);
            Assert.Equal(automatic.AppId, automatic.EffectiveAppId);
        }
        finally
        {
            Environment.SetEnvironmentVariable("APP_ID", previous);
        }
    }
}
