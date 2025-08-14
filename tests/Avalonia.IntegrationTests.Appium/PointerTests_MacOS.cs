using System;
using System.Threading;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Interactions;
using Xunit;

namespace Avalonia.IntegrationTests.Appium;

[Collection("Default")]
public class PointerTests_MacOS : TestBase, IDisposable
{
    public PointerTests_MacOS(DefaultAppFixture fixture)
        : base(fixture, "Window Decorations")
    {
    }

    [PlatformFact(TestPlatforms.MacOS)]
    public void OSXThickTitleBar_Pointer_Events_Continue_Outside_Window_During_Drag()
    {
        // issue #15696
        SetParameters(true, false, true, true, true);
        
        var showNewWindowDecorations = Session.FindElementByAccessibilityId("ShowNewWindowDecorations");
        showNewWindowDecorations.Click();
        
        Thread.Sleep(1000);
        
        var secondaryWindow = Session.GetWindowById("SecondaryWindow");
        
        var titleAreaControl = secondaryWindow.FindElementByAccessibilityId("TitleAreaControl");
        Assert.NotNull(titleAreaControl);

        new Actions(Session).MoveToElement(secondaryWindow).Perform();
        new Actions(Session).MoveToElement(titleAreaControl).Perform();
        new Actions(Session).DragAndDropToOffset(titleAreaControl, 50, -100).Perform();
        
        var finalMoveCount = GetMoveCount(secondaryWindow);
        var finalReleaseCount = GetReleaseCount(secondaryWindow);
            
        Assert.True(finalMoveCount >= 10, $"Expected at least 10 new mouse move events outside window, got {finalMoveCount})");
        Assert.Equal(1, finalReleaseCount);
        
        secondaryWindow.FindElementByAccessibilityId("_XCUI:CloseWindow").Click();
    }

    [PlatformFact(TestPlatforms.MacOS, Skip = "Test")]
    public void OSXThickTitleBar_Single_Click_Does_Not_Generate_DoubleTapped_Event()
    {
        // Test that single clicks in titlebar area don't trigger false double-click events
        SetParameters(true, false, true, true, true);
        
        var showNewWindowDecorations = Session.FindElementByAccessibilityId("ShowNewWindowDecorations");
        showNewWindowDecorations.Click();
        
        Thread.Sleep(1000);
        
        var secondaryWindow = Session.GetWindowById("SecondaryWindow");
        var titleAreaControl = secondaryWindow.FindElementByAccessibilityId("TitleAreaControl");
        Assert.NotNull(titleAreaControl);
        
        // Verify initial state - counters should be 0
        var initialDoubleClickCount = GetDoubleClickCount(secondaryWindow);
        var initialReleaseCount = GetReleaseCount(secondaryWindow);
        var initialMouseDownCount = GetMouseDownCount(secondaryWindow);
        Assert.Equal(0, initialDoubleClickCount);
        Assert.Equal(0, initialReleaseCount);
        Assert.Equal(0, initialMouseDownCount);
        
        // Perform 3 single clicks in titlebar area with delays to avoid accidental double-clicks
        secondaryWindow.MovePointerOver();
        titleAreaControl.MovePointerOver();
        titleAreaControl.SendClick();
        Thread.Sleep(800); // Wait longer than double-click interval
        
        // After first single click - mouse down = 1, release = 1, double-click = 0
        var afterFirstClickMouseDownCount = GetMouseDownCount(secondaryWindow);
        var afterFirstClickReleaseCount = GetReleaseCount(secondaryWindow);
        var afterFirstClickDoubleClickCount = GetDoubleClickCount(secondaryWindow);
        Assert.Equal(1, afterFirstClickMouseDownCount);
        Assert.Equal(1, afterFirstClickReleaseCount);
        Assert.Equal(0, afterFirstClickDoubleClickCount);
        
        titleAreaControl.SendClick();
        Thread.Sleep(800);
        
        // After second single click - mouse down = 2, release = 2, double-click = 0
        var afterSecondClickMouseDownCount = GetMouseDownCount(secondaryWindow);
        var afterSecondClickReleaseCount = GetReleaseCount(secondaryWindow);
        var afterSecondClickDoubleClickCount = GetDoubleClickCount(secondaryWindow);
        Assert.Equal(2, afterSecondClickMouseDownCount);
        Assert.Equal(2, afterSecondClickReleaseCount);
        Assert.Equal(0, afterSecondClickDoubleClickCount);
        
        titleAreaControl.SendClick();
        Thread.Sleep(500);
        
        // After third single click - mouse down = 3, release = 3, double-click = 0
        var afterThirdClickMouseDownCount = GetMouseDownCount(secondaryWindow);
        var afterThirdClickReleaseCount = GetReleaseCount(secondaryWindow);
        var afterThirdClickDoubleClickCount = GetDoubleClickCount(secondaryWindow);
        Assert.Equal(3, afterThirdClickMouseDownCount);
        Assert.Equal(3, afterThirdClickReleaseCount);
        Assert.Equal(0, afterThirdClickDoubleClickCount);
        
        // Now perform an actual double-click to verify the counters work
        titleAreaControl.SendDoubleClick();
        Thread.Sleep(500);
        
        // After double-click - mouse down = 5 (3 + 2), release = 5 (3 + 2), double-click = 1
        var afterDoubleClickMouseDownCount = GetMouseDownCount(secondaryWindow);
        var afterDoubleClickReleaseCount = GetReleaseCount(secondaryWindow);
        var afterDoubleClickCount = GetDoubleClickCount(secondaryWindow);
        Assert.Equal(5, afterDoubleClickMouseDownCount);
        Assert.Equal(5, afterDoubleClickReleaseCount);
        Assert.Equal(1, afterDoubleClickCount);
        
        secondaryWindow.FindElementByAccessibilityId("_XCUI:CloseWindow").Click();
    }
    
    private void SetParameters(
        bool extendClientArea,
        bool forceSystemChrome,
        bool preferSystemChrome,
        bool macOsThickSystemChrome,
        bool showTitleAreaControl)
    {
        var extendClientAreaCheckBox = Session.FindElementByAccessibilityId("WindowExtendClientAreaToDecorationsHint");
        var forceSystemChromeCheckBox = Session.FindElementByAccessibilityId("WindowForceSystemChrome");
        var preferSystemChromeCheckBox = Session.FindElementByAccessibilityId("WindowPreferSystemChrome");
        var macOsThickSystemChromeCheckBox = Session.FindElementByAccessibilityId("WindowMacThickSystemChrome");
        var showTitleAreaControlCheckBox = Session.FindElementByAccessibilityId("WindowShowTitleAreaControl");

        if (extendClientAreaCheckBox.GetIsChecked() != extendClientArea)
            extendClientAreaCheckBox.Click();
        if (forceSystemChromeCheckBox.GetIsChecked() != forceSystemChrome)
            forceSystemChromeCheckBox.Click();
        if (preferSystemChromeCheckBox.GetIsChecked() != preferSystemChrome)
            preferSystemChromeCheckBox.Click();
        if (macOsThickSystemChromeCheckBox.GetIsChecked() != macOsThickSystemChrome)
            macOsThickSystemChromeCheckBox.Click();
        if (showTitleAreaControlCheckBox.GetIsChecked() != showTitleAreaControl)
            showTitleAreaControlCheckBox.Click();
    }
    
    private int GetMoveCount(AppiumWebElement window)
    {
        var mouseMoveCountTextBox = window.FindElementByAccessibilityId("MouseMoveCount");
        return int.Parse(mouseMoveCountTextBox.Text ?? "0");
    }
    
    private int GetReleaseCount(AppiumWebElement window)
    {
        var mouseReleaseCountTextBox = window.FindElementByAccessibilityId("MouseReleaseCount");
        return int.Parse(mouseReleaseCountTextBox.Text ?? "0");
    }
    
    private int GetMouseDownCount(AppiumWebElement window)
    {
        var mouseDownCountTextBox = window.FindElementByAccessibilityId("MouseDownCount");
        return int.Parse(mouseDownCountTextBox.Text ?? "0");
    }
    
    private int GetDoubleClickCount(AppiumWebElement window)
    {
        var doubleClickCountTextBox = window.FindElementByAccessibilityId("DoubleClickCount");
        return int.Parse(doubleClickCountTextBox.Text ?? "0");
    }

    public void Dispose()
    {
        SetParameters(false, false, false, false, false);
        var applyButton = Session.FindElementByAccessibilityId("ApplyWindowDecorations");
        applyButton.Click();
    }
}
