using System;
using System.Globalization;
using Avalonia.Platform;
using Xunit;

namespace Avalonia.IntegrationTests.Appium;

[Collection("Default")]
public class ScreenTests
{
    private readonly AppiumDriver _session;

    public ScreenTests(DefaultAppFixture fixture)
    {
        _session = fixture.Session;

        var tabs = _session.FindElementByAccessibilityId("MainTabs");
        var tab = tabs.FindElementByName("Screens");
        tab.Click();
    }

    [Fact]
    public void Can_Read_Current_Screen_Info()
    {
        var refreshButton = _session.FindElementByAccessibilityId("ScreenRefresh");
        refreshButton.SendClick();

        var screenName = _session.FindElementByAccessibilityId("ScreenName").Text;
        var screenHandle = _session.FindElementByAccessibilityId("ScreenHandle").Text;
        var screenBounds = Rect.Parse(_session.FindElementByAccessibilityId("ScreenBounds").Text);
        var screenWorkArea = Rect.Parse(_session.FindElementByAccessibilityId("ScreenWorkArea").Text);
        var screenScaling = double.Parse(_session.FindElementByAccessibilityId("ScreenScaling").Text, NumberStyles.Float, CultureInfo.InvariantCulture);
        var screenOrientation = Enum.Parse<ScreenOrientation>(_session.FindElementByAccessibilityId("ScreenOrientation").Text);

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
        var refreshButton = _session.FindElementByAccessibilityId("ScreenRefresh");
        refreshButton.SendClick();

        var screenName1 = _session.FindElementByAccessibilityId("ScreenName").Text;
        var screenHandle1 = _session.FindElementByAccessibilityId("ScreenHandle").Text;

        refreshButton.SendClick();

        var screenName2 = _session.FindElementByAccessibilityId("ScreenName").Text;
        var screenHandle2 = _session.FindElementByAccessibilityId("ScreenHandle").Text;
        var screenSameReference = bool.Parse(_session.FindElementByAccessibilityId("ScreenSameReference").Text);

        Assert.Equal(screenName1, screenName2);
        Assert.Equal(screenHandle1, screenHandle2);
        Assert.True(screenSameReference);
    }
}
