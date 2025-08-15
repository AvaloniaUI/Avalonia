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

    [PlatformFact(TestPlatforms.MacOS)]
    public void OSXThickTitleBar_Single_Click_Does_Not_Generate_DoubleTapped_Event()
    {
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
        
        // Perform a single click in titlebar area 
        secondaryWindow.MovePointerOver();
        titleAreaControl.MovePointerOver();
        titleAreaControl.SendClick();
        Thread.Sleep(800);
        
        // After first single click - mouse down = 1, release = 1, double-click = 0
        var afterFirstClickMouseDownCount = GetMouseDownCount(secondaryWindow);
        var afterFirstClickReleaseCount = GetReleaseCount(secondaryWindow);
        var afterFirstClickDoubleClickCount = GetDoubleClickCount(secondaryWindow);
        Assert.Equal(1, afterFirstClickMouseDownCount);
        Assert.Equal(1, afterFirstClickReleaseCount);
        Assert.Equal(0, afterFirstClickDoubleClickCount);
        
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
