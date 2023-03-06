using System;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using Xunit.Sdk;

namespace Avalonia.IntegrationTests.Appium.Crapium;

public class UiWindowElement : UIElement, IWindowElement, IEquatable<IWindowElement>
{
    public UiWindowElement(IWebElement inner) : base(inner)
    {
        var parent = Inner.FindElement(By.XPath("//XCUIElementTypeWindow[1]"));

        var closeButton = inner.FindElement(By.XPath("//XCUIElementTypeButton[1]"));
        
        
    }

    public string Id => (Inner as AppiumElement).Id;

    public void Close()
    {
        var (close, _, _) = this.GetChromeButtons();
        close.Click();
    }
    
    private (IElement close, IElement minimize, IElement maximize) GetChromeButtons()
    {
        var closeButton = FindByXPath("//XCUIElementTypeButton[1]");
        var fullscreenButton = FindByXPath("//XCUIElementTypeButton[2]");
        var minimizeButton = FindByXPath("//XCUIElementTypeButton[3]");
        return (closeButton, minimizeButton, fullscreenButton);
    }

    public bool Equals(IWindowElement? other)
    {
        if (other is UiWindowElement element)
        {
            return Name == element.Name;
        }

        return false;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((UiWindowElement)obj);
    }

    public override int GetHashCode()
    {
        throw new NotImplementedException();
    }
}
