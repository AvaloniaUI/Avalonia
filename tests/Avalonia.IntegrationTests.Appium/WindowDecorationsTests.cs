using System;
using System.Threading;
using Avalonia.Controls;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Internal;
using Xunit;

namespace Avalonia.IntegrationTests.Appium;

[Collection("Default")]
public class WindowDecorationsTests : IDisposable
{
    private readonly AppiumDriver<AppiumWebElement> _session;

    public WindowDecorationsTests(DefaultAppFixture fixture)
    {
        _session = fixture.Session;

        var tabs = _session.FindElementByAccessibilityId("MainTabs");
        var tab = tabs.FindElementByName("Window Decorations");
        tab.Click();
    }

    [Fact(Skip = "Fix me")]
    public void Window_Size_Should_Be_Consistent_Between_Toggles()
    {
        var window = _session.FindElementByAccessibilityId("MainWindow");
        var original = window.Size;

        // Step 1: keep extend client area to false, but adjust some value that should not have any effect.
        SetParameters(false, false, false, false, 10);
        ApplyToCurrentWindow();
        Assert.Equal(original, window.Size);

        // Step 2: enable and disable extended system chrome. 
        SetParameters(true, true, false, false, 20);
        ApplyToCurrentWindow();
        SetParameters(false, false, false, false, 20);
        ApplyToCurrentWindow();
        Assert.Equal(original, window.Size);

        // Step 3: enable and disable extended client chrome. 
        SetParameters(true, false, true, false, 30);
        ApplyToCurrentWindow();
        SetParameters(false, false, true, false, 20);
        ApplyToCurrentWindow();
        Assert.Equal(original, window.Size);
    }

    [PlatformTheory(TestPlatforms.Windows)]
    [InlineData(-1)]
    [InlineData(25)]
    [InlineData(50)]
    public void Should_Apply_Client_Side_Chrome(int titleBarHeight)
    {
        SetParameters(true, false, true, false, titleBarHeight);

        ApplyToCurrentWindow();

        Thread.Sleep(500);

        var (actualTitleBarHeight, actualButtonHeight) = ReadClientDecorationsHeight(_session);

        if (titleBarHeight == -1) // default value - accept any greater than zero.
        {
            Assert.True(actualTitleBarHeight > 0);
            Assert.True(actualButtonHeight > 0);
        }
        else
        {
            Assert.Equal(titleBarHeight, actualTitleBarHeight);
            // In Avalonia title bar buttons are not constrained by title bar height, so it can be higher.
            // Assert.True(buttonHeight <= titleBarHeight, "Button is higher than requested title bar height.");
        }
    }

    [PlatformTheory(TestPlatforms.Windows)]
    [InlineData(-1)]
    [InlineData(25)]
    [InlineData(50)]
    public void Should_Apply_Server_Side_Chrome(int titleBarHeight)
    {
        SetParameters(true, true, false, false, titleBarHeight);

        ApplyToCurrentWindow();

        Thread.Sleep(500);

        var (actualTitleBarHeight, actualButtonHeight) = ReadServerDecorationsHeight(_session);

        if (titleBarHeight == -1) // default value - accept any greater than zero.
        {
            // On windows, system chrome is always 0px height, when client area is extended.
            if (!OperatingSystem.IsWindows())
                Assert.True(actualTitleBarHeight > 0);
            Assert.True(actualButtonHeight > 0, "Button is highter than requested title bar height.");
        }
        else
        {
            if (!OperatingSystem.IsWindows())
                Assert.Equal(titleBarHeight, actualTitleBarHeight);
            Assert.True(actualButtonHeight <= titleBarHeight, "Button is highter than requested title bar height.");
        }
    }

    [PlatformTheory(TestPlatforms.Windows)]
    [InlineData(-1)]
    [InlineData(25)]
    [InlineData(50)]
    public void Should_Apply_Client_Side_Chrome_On_New_Window(int titleBarHeight)
    {
        SetParameters(true, false, true, false, titleBarHeight);

        using (ApplyOnNewWindow())
        {
            Thread.Sleep(500);

            var secondaryWindow = WindowTests.GetWindow(_session, "SecondaryWindow");

            var (actualTitleBarHeight, actualButtonHeight) = ReadClientDecorationsHeight(secondaryWindow);

            if (titleBarHeight == -1) // default value - accept any greater than zero.
            {
                Assert.True(actualTitleBarHeight > 0);
                Assert.True(actualButtonHeight > 0);
            }
            else
            {
                Assert.Equal(titleBarHeight, actualTitleBarHeight);
                // In Avalonia title bar buttons are not constrained by title bar height, so it can be higher.
                // Assert.True(buttonHeight <= titleBarHeight, "Button is higher than requested title bar height.");
            }
        }
    }

    private void SetParameters(
        bool extendClientArea,
        bool forceSystemChrome,
        bool preferSystemChrome,
        bool macOsThickSystemChrome,
        int titleBarHeight)
    {
        var extendClientAreaCheckBox = _session.FindElementByAccessibilityId("WindowExtendClientAreaToDecorationsHint");
        var forceSystemChromeCheckBox = _session.FindElementByAccessibilityId("WindowForceSystemChrome");
        var preferSystemChromeCheckBox = _session.FindElementByAccessibilityId("WindowPreferSystemChrome");
        var macOsThickSystemChromeCheckBox = _session.FindElementByAccessibilityId("WindowMacThickSystemChrome");
        var titleBarHeightBox = _session.FindElementByAccessibilityId("WindowTitleBarHeightHint");

        if (extendClientAreaCheckBox.GetIsChecked() != extendClientArea)
            extendClientAreaCheckBox.Click();
        if (forceSystemChromeCheckBox.GetIsChecked() != forceSystemChrome)
            forceSystemChromeCheckBox.Click();
        if (preferSystemChromeCheckBox.GetIsChecked() != preferSystemChrome)
            preferSystemChromeCheckBox.Click();
        if (macOsThickSystemChromeCheckBox.GetIsChecked() != macOsThickSystemChrome)
            macOsThickSystemChromeCheckBox.Click();

        titleBarHeightBox.Click();
        titleBarHeightBox.Clear();
        if (titleBarHeight >= 0)
            titleBarHeightBox.SendKeys(titleBarHeight.ToString());
    }

    private (int titleBarHeight, int buttonHeight) ReadClientDecorationsHeight(IFindsByClassName root)
    {
        var titlebar = (AppiumWebElement)root.FindElementByClassName("TitleBar");
        var minimize = titlebar.FindElementByName("Minimize");
        _ = titlebar.FindElementByName("Maximize");
        _ = titlebar.FindElementByName("Close");

        return (titlebar.Size.Height, minimize.Size.Height);
    }

    private (int titleBarHeight, int buttonHeight) ReadServerDecorationsHeight(IFindsByTagName root)
    {
        var titlebar = (AppiumWebElement)root.FindElementByTagName("TitleBar");
        var minimize = titlebar.FindElementByName("Minimize");
        _ = titlebar.FindElementByName("Maximize");
        _ = titlebar.FindElementByName("Close");

        return (titlebar.Size.Height, minimize.Size.Height);
    }

    private void ApplyToCurrentWindow()
    {
        var applyWindowDecorations = _session.FindElementByAccessibilityId("ApplyWindowDecorations");
        applyWindowDecorations.Click();
    }

    private IDisposable ApplyOnNewWindow()
    {
        var showNewWindowDecorations = _session.FindElementByAccessibilityId("ShowNewWindowDecorations");
        return showNewWindowDecorations.OpenWindowWithClick();
    }

    public void Dispose()
    {
        SetParameters(false, false, false, false, -1);
        ApplyToCurrentWindow();
    }
}
