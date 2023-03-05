using System;
using System.Collections.Generic;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;

namespace Avalonia.IntegrationTests.Appium.Crapium;

public class AppiumElement : IElement
{
    private readonly IWebElement _inner;
    
    public AppiumElement(IWebElement inner)
    {
        _inner = inner;
    }

    public string Name => _inner.GetAttribute("title");
    
    public virtual void Click()
    {
        _inner.Click();
    }
    
    public T FindElement<T>(string id) where T : IElement
    {
        var f = MacSession.GetElementFactory<T>();
        return (T)f(_inner.FindElement(MobileBy.AccessibilityId(id)));
    }
    
    public T FindElementByName<T>(string id) where T : IElement
    {
        var f = MacSession.GetElementFactory<T>();
        return (T)f(_inner.FindElement(MobileBy.Name(id)));
    }
}
