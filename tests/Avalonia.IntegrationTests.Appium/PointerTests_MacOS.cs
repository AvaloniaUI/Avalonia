using System;
using System.Threading;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Interactions;
using Xunit;

namespace Avalonia.IntegrationTests.Appium;

[Collection("Default")]
public class PointerTests_MacOS : TestBase
{
    public PointerTests_MacOS(DefaultAppFixture fixture)
        : base(fixture, "Window Decorations")
    {
    }
    
    private void SetParameters(
        bool extendClientArea,
        bool showTitleAreaControl)
    {
        var extendClientAreaCheckBox = Session.FindElementByAccessibilityId("WindowExtendClientAreaToDecorationsHint");
        var showTitleAreaControlCheckBox = Session.FindElementByAccessibilityId("WindowShowTitleAreaControl");

        if (extendClientAreaCheckBox.GetIsChecked() != extendClientArea)
            extendClientAreaCheckBox.Click();
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

    public override void Dispose()
    {
        SetParameters(false, false);
        var applyButton = Session.FindElementByAccessibilityId("ApplyWindowDecorations");
        applyButton.Click();
        base.Dispose();
    }
}
