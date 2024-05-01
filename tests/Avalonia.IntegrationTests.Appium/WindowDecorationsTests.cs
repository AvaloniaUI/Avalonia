using System;
using System.Threading;
using Xunit;

namespace Avalonia.IntegrationTests.Appium;

[Collection("WindowDecorations")]
public class WindowDecorationsTests : IDisposable
{
    private readonly AppiumDriver _session;

    public WindowDecorationsTests(DefaultAppFixture fixture)
    {
        _session = fixture.Session;

        var tabs = _session.FindElementByAccessibilityId("MainTabs");
        var tab = tabs.FindElementByName("Window Decorations");
        tab.Click();
    }

    [PlatformFact(TestPlatforms.MacOS)] // TODO fix me on Windows
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

    [Fact]
    public void Can_Restore_To_Non_Extended_State()
    {
        SetParameters(true, true, false, false, 20);
        ApplyToCurrentWindow();

        SetParameters(false, false, false, false, 1000);
        ApplyToCurrentWindow();

        var currentWindow = _session.GetCurrentSingleWindow();
        var systemChrome = currentWindow.GetSystemChromeButtons();
        var clientChrome = currentWindow.GetClientChromeButtons();

        AssertSystemChrome(systemChrome, clientChrome, false);

        var props = _session.FindElementByAccessibilityId("WindowDecorationProperties");
        Assert.Equal($"0 0 False", props.Text);
    }

    [PlatformTheory(TestPlatforms.Windows)]  // We don't have client chrome on macOS
    [InlineData(-1)]
    [InlineData(25)]
    [InlineData(50)]
    public void Should_Apply_Client_Chrome(int titleBarHeight)
    {
        SetParameters(true, false, true, false, titleBarHeight);

        ApplyToCurrentWindow();

        Thread.Sleep(500);

        var currentWindow = _session.GetCurrentSingleWindow();
        var systemChrome = currentWindow.GetSystemChromeButtons();
        var clientChrome = currentWindow.GetClientChromeButtons();

        AssertClientChrome(systemChrome, clientChrome, titleBarHeight);

        var props = _session.FindElementByAccessibilityId("WindowDecorationProperties");
        if (titleBarHeight > 0)
        {
            Assert.Equal($"0 {titleBarHeight} True", props.Text);
        }
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(25)]
    [InlineData(50)]
    public void Should_Apply_System_Chrome(int titleBarHeight)
    {
        SetParameters(true, true, false, false, titleBarHeight);

        ApplyToCurrentWindow();

        Thread.Sleep(500);

        var currentWindow = _session.GetCurrentSingleWindow();
        var systemChrome = currentWindow.GetSystemChromeButtons();
        var clientChrome = currentWindow.GetClientChromeButtons();

        AssertSystemChrome(systemChrome, clientChrome, true);

        var props = _session.FindElementByAccessibilityId("WindowDecorationProperties");
        if (titleBarHeight > 0)
        {
            Assert.Equal($"0 {titleBarHeight} True", props.Text);
        }
    }

    [PlatformTheory(TestPlatforms.Windows)] // We don't have client chrome on macOS
    [InlineData(-1)]
    [InlineData(25)]
    [InlineData(50)]
    public void Should_Apply_Client_Chrome_On_New_Window(int titleBarHeight)
    {
        SetParameters(true, false, true, false, titleBarHeight);

        using (ApplyOnNewWindow())
        {
            Thread.Sleep(500);

            var secondaryWindow = _session.GetWindowById("SecondaryWindow");
            var systemChrome = secondaryWindow.GetSystemChromeButtons();
            var clientChrome = secondaryWindow.GetClientChromeButtons();

            AssertClientChrome(systemChrome, clientChrome, titleBarHeight);
        }
    }

    [PlatformTheory(TestPlatforms.MacOS)] // fix me, for some reason Windows doesn't return TitleBar system chrome for a child window. 
    [InlineData(-1)]
    [InlineData(25)]
    [InlineData(50)]
    public void Should_Apply_System_Chrome_On_New_Window(int titleBarHeight)
    {
        SetParameters(true, true, false, false, titleBarHeight);

        using (ApplyOnNewWindow())
        {
            Thread.Sleep(500);

            var secondaryWindow = _session.GetWindowById("SecondaryWindow");
            var systemChrome = secondaryWindow.GetSystemChromeButtons();
            var clientChrome = secondaryWindow.GetClientChromeButtons();

            AssertSystemChrome(systemChrome, clientChrome, true);
        }
    }

    private void AssertClientChrome(WindowChrome systemChrome, WindowChrome clientChrome, int titleBarHeight)
    {
        // Ignore windows, it always reports full sized and enabled buttons and title bar. Just drawn behind.
        if (!OperatingSystem.IsWindows())
        {
            // All system chrome buttons are hidden.
            Assert.False(systemChrome.IsAnyButtonEnabled);
            Assert.Equal(-1, systemChrome.MaxButtonHeight);
            Assert.True(systemChrome.TitleBarHeight is -1 or 0);
        }

        // At least some client chrome buttons are shown.
        Assert.True(clientChrome.IsAnyButtonEnabled);
        Assert.True(clientChrome.MaxButtonHeight > 0);
        if (titleBarHeight != -1)
        {
            Assert.Equal(titleBarHeight, clientChrome.TitleBarHeight);
        }
    }

    private void AssertSystemChrome(WindowChrome systemChrome, WindowChrome clientChrome, bool isExtended)
    {
        // At least some server chrome buttons are shown.
        Assert.True(systemChrome.IsAnyButtonEnabled);
        Assert.True(systemChrome.MaxButtonHeight > 0);

        // All client chrome buttons are hidden.
        Assert.False(clientChrome.IsAnyButtonEnabled);
        Assert.Equal(-1, clientChrome.MaxButtonHeight);

        // System chrome is always 0px height, when client area is extended.
        if (isExtended)
        {
            Assert.True(systemChrome.TitleBarHeight is -1 or 0);
        }
        // Can't get titlebar height on macOS.
        else if (!OperatingSystem.IsMacOS())
        {
            Assert.True(systemChrome.TitleBarHeight > 0);
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
