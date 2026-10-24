using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using OpenQA.Selenium.Appium;
using Xunit;

namespace Avalonia.IntegrationTests.Appium;

[Collection("Default")]
public class TrafficLightHostTests : TestBase
{
    public TrafficLightHostTests(DefaultAppFixture fixture)
        : base(fixture, "Window Decorations")
    {
    }

    [PlatformFact(TestPlatforms.MacOS)]
    public void Host_Without_Explicit_Size_Fits_Buttons()
    {
        using (OpenHostWindow())
        {
            ToggleHost();

            AssertRectsAreClose(GetHostRect(), GetButtonGroupRect());
        }
    }

    [PlatformFact(TestPlatforms.MacOS)]
    public void Buttons_Are_Centered_In_Larger_Host()
    {
        using (OpenHostWindow())
        {
            ApplyHostLayout("40,10", "200,50");

            ToggleHost();

            var host = GetHostRect();
            var buttonGroup = GetButtonGroupRect();
            AssertAreClose(host.Left + host.Width / 2, buttonGroup.Left + buttonGroup.Width / 2);
            AssertAreClose(host.Top + host.Height / 2, buttonGroup.Top + buttonGroup.Height / 2);
        }
    }

    [PlatformFact(TestPlatforms.MacOS)]
    public void Buttons_Follow_Host_When_It_Moves()
    {
        using (OpenHostWindow())
        {
            ToggleHost();

            ApplyHostLayout("120,20", "");

            AssertRectsAreClose(GetHostRect(), GetButtonGroupRect());
        }
    }

    [PlatformFact(TestPlatforms.MacOS)]
    public void Buttons_Stay_In_Host_When_Window_Is_Resized()
    {
        using (OpenHostWindow())
        {
            ToggleHost();

            Session.FindElementByAccessibilityId("AddToTrafficLightHostWindowWidth").Click();
            Thread.Sleep(500);

            AssertRectsAreClose(GetHostRect(), GetButtonGroupRect());
        }
    }

    [PlatformFact(TestPlatforms.MacOS)]
    public void System_Layout_Is_Restored_When_Host_Is_Hidden()
    {
        using (OpenHostWindow())
        {
            var systemButtonRects = GetButtonRects();
            ToggleHost();

            ToggleHost();

            Assert.Equal(systemButtonRects, GetButtonRects());
        }
    }

    private IDisposable OpenHostWindow() =>
        Session.FindElementByAccessibilityId("ShowTrafficLightHostWindow").OpenWindowWithClick();

    private void ToggleHost()
    {
        Session.FindElementByAccessibilityId("TrafficLightHostIsVisible").Click();
        Thread.Sleep(500);
    }

    private void ApplyHostLayout(string margin, string size)
    {
        var marginBox = Session.FindElementByAccessibilityId("TrafficLightHostMargin");
        var sizeBox = Session.FindElementByAccessibilityId("TrafficLightHostSize");

        marginBox.Click();
        marginBox.Clear();
        marginBox.SendKeys(margin);

        sizeBox.Click();
        sizeBox.Clear();
        if (size.Length > 0)
            sizeBox.SendKeys(size);

        Session.FindElementByAccessibilityId("ApplyTrafficLightHost").Click();
        Thread.Sleep(500);
    }

    private Rectangle GetHostRect() => Session.FindElementByAccessibilityId("TrafficLightHost").Rect;

    private IReadOnlyList<Rectangle> GetButtonRects()
    {
        var chrome = Session.GetWindowById("TrafficLightHostWindow").GetSystemChromeButtons();
        return new[] { chrome.Close, chrome.Minimize, chrome.Maximize, chrome.FullScreen }
            .OfType<AppiumWebElement>()
            .Select(button => button.Rect)
            .ToList();
    }

    private Rectangle GetButtonGroupRect() => GetButtonRects().Aggregate(Rectangle.Union);

    // Screen coordinates are rounded to whole points, so equal layouts can differ by one point.
    private static void AssertAreClose(int expected, int actual) =>
        Assert.InRange(actual, expected - 1, expected + 1);

    private static void AssertRectsAreClose(Rectangle expected, Rectangle actual)
    {
        AssertAreClose(expected.Left, actual.Left);
        AssertAreClose(expected.Top, actual.Top);
        AssertAreClose(expected.Right, actual.Right);
        AssertAreClose(expected.Bottom, actual.Bottom);
    }
}
