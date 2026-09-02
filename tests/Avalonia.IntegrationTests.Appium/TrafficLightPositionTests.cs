using System;
using System.Reactive.Disposables;
using System.Threading;
using Xunit;

namespace Avalonia.IntegrationTests.Appium;

[Collection("WindowDecorations")]
public class TrafficLightPositionTests : TestBase
{
    public TrafficLightPositionTests(DefaultAppFixture fixture)
        : base(fixture, "Window Decorations")
    {
    }

    [PlatformFact(TestPlatforms.MacOS)]
    public void Xaml_Position_Is_Applied_On_First_Show()
    {
        using (OpenWindow())
        {
            AssertTrafficLightPosition(50, 50);
        }
    }

    [PlatformFact(TestPlatforms.MacOS)]
    public void Position_Can_Be_Changed_And_Reset_To_System()
    {
        using (OpenWindow())
        {
            ClickAndWait("ResetTrafficLightPosition");
            var systemPosition = GetTrafficLightPosition();

            ClickAndWait("SetCustomTrafficLightPosition");
            AssertTrafficLightPosition(70, 40);

            ClickAndWait("ResetTrafficLightPosition");
            Assert.Equal(systemPosition, GetTrafficLightPosition());
        }
    }

    [PlatformFact(TestPlatforms.MacOS)]
    public void Custom_Position_Is_Restored_After_Appearance_Change()
    {
        using (OpenWindow())
        {
            ClickAndWait("ToggleTrafficLightAppearance");
            AssertTrafficLightPosition(50, 50);
        }
    }

    [PlatformFact(TestPlatforms.MacOS)]
    public void Custom_Position_Is_Restored_After_Resize()
    {
        using (OpenWindow())
        {
            ClickAndWait("ResizeTrafficLightWindow");
            AssertTrafficLightPosition(50, 50);
        }
    }

    private IDisposable OpenWindow()
    {
        Session.FindElementByAccessibilityId("ShowTrafficLightPositionWindow").Click();
        Thread.Sleep(1000);

        return Disposable.Create(() =>
        {
            var window = Session.GetWindowById("TrafficLightPositionTestWindow");
            window.GetSystemChromeButtons().Close!.Click();
            Thread.Sleep(1000);
        });
    }

    private void ClickAndWait(string automationId)
    {
        Session.FindElementByAccessibilityId(automationId).Click();
        Thread.Sleep(500);
    }

    private (int X, int Y) GetTrafficLightPosition()
    {
        var window = Session.GetWindowById("TrafficLightPositionTestWindow");
        var closeButton = window.GetSystemChromeButtons().Close;

        Assert.NotNull(closeButton);
        return (
            closeButton.Location.X - window.Location.X,
            closeButton.Location.Y - window.Location.Y);
    }

    private void AssertTrafficLightPosition(int expectedX, int expectedY)
    {
        var actual = GetTrafficLightPosition();
        Assert.InRange(actual.X, expectedX - 1, expectedX + 1);
        Assert.InRange(actual.Y, expectedY - 1, expectedY + 1);
    }
}
