using System;
using System.Globalization;
using Avalonia.Platform;
using Xunit;

namespace Avalonia.IntegrationTests.Appium;

[Collection("Default")]
public class ScreenTests : TestBase
{
    public ScreenTests(DefaultAppFixture fixture)
        : base(fixture, "Screens")
    {
    }

    [Fact]
    public void Can_Read_Current_Screen_Info()
    {
        var refreshButton = Session.FindElementByAccessibilityId("ScreenRefresh");
        refreshButton.SendClick();

        var screenName = Session.FindElementByAccessibilityId("ScreenName").Text;
        var screenHandle = Session.FindElementByAccessibilityId("ScreenHandle").Text;
        var screenBounds = Rect.Parse(Session.FindElementByAccessibilityId("ScreenBounds").Text);
        var screenWorkArea = Rect.Parse(Session.FindElementByAccessibilityId("ScreenWorkArea").Text);
        var screenScaling = double.Parse(Session.FindElementByAccessibilityId("ScreenScaling").Text, NumberStyles.Float, CultureInfo.InvariantCulture);
        var screenOrientation = Enum.Parse<ScreenOrientation>(Session.FindElementByAccessibilityId("ScreenOrientation").Text);

        Assert.NotNull(screenName);
        Assert.NotNull(screenHandle);
        Assert.True(screenBounds.Size is { Width: > 0, Height: > 0 });
        Assert.True(screenWorkArea.Size is { Width: > 0, Height: > 0 });
        Assert.True(screenBounds.Size.Width >= screenWorkArea.Size.Width);
        Assert.True(screenBounds.Size.Height >= screenWorkArea.Size.Height);
        Assert.True(screenScaling > 0);
        Assert.True(screenOrientation != ScreenOrientation.None);
    }

    [Fact]
    public void Returns_The_Same_Screen_Instance()
    {
        var refreshButton = Session.FindElementByAccessibilityId("ScreenRefresh");
        refreshButton.SendClick();

        var screenName1 = Session.FindElementByAccessibilityId("ScreenName").Text;
        var screenHandle1 = Session.FindElementByAccessibilityId("ScreenHandle").Text;

        refreshButton.SendClick();

        var screenName2 = Session.FindElementByAccessibilityId("ScreenName").Text;
        var screenHandle2 = Session.FindElementByAccessibilityId("ScreenHandle").Text;
        var screenSameReference = bool.Parse(Session.FindElementByAccessibilityId("ScreenSameReference").Text);

        Assert.Equal(screenName1, screenName2);
        Assert.Equal(screenHandle1, screenHandle2);
        Assert.True(screenSameReference);
    }
}
