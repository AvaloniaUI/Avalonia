using System;
using System.Collections.Generic;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Interactions;

namespace Avalonia.IntegrationTests.Appium.Wrappers;

public class Element : IElement
{
    private readonly IWebElement _inner;

    public Element(IWebElement inner)
    {
        _inner = inner;
    }

    protected IWebElement Inner => _inner;

    public virtual string Name => _inner.GetAttribute("title");

    public virtual string Text => _inner.Text;

    public bool Enabled => _inner.Enabled;


    public string GetAttribute(string attribute)
    {
        return _inner.GetAttribute(attribute);
    }

    public virtual void Click()
    {
        _inner.Click();
    }

    public void SendClick()
    {
        var e = (AppiumWebElement)_inner;
        new Actions(e.WrappedDriver).MoveToElement(_inner).Click().Perform();
    }

    public void SendKeys(string text)
    {
        _inner.SendKeys(text);
    }

    public byte[] GetScreenshot()
    {
        var ss = (_inner as AppiumWebElement).GetScreenshot();

        return ss.AsByteArray;
    }
    
    public IElement FindElementByAccessibilityId(string id)
    {
        var f = MacSession.GetElementFactory();
        return (IElement)f(_inner.FindElement(MobileBy.AccessibilityId(id)));
    }
    
    public IElement FindElementByName(string id)
    {
        var f = MacSession.GetElementFactory();
        return (IElement)f(_inner.FindElement(MobileBy.Name(id)));
    }
    
    public IElement FindByXPath(string xpath)
    {
        var f = MacSession.GetElementFactory();
        return (IElement)f(Inner.FindElement(By.XPath(xpath)));
    }

    public IList<IElement> FindManyByXPath(string xpath)
    {
        var f = MacSession.GetElementFactory();

        var result = new List<IElement>();
        
        foreach (var element in Inner.FindElements(By.XPath(xpath)))
        {
            result.Add((IElement)f(element));
        }
        
        return result;
    }
    
    public IList<IElement> FindManyByTagName(string tagName)
    {
        var f = MacSession.GetElementFactory();

        var result = new List<IElement>();
        
        foreach (var element in Inner.FindElements(By.TagName(tagName)))
        {
            result.Add((IElement)f(element));
        }
        
        return result;
    }

    public IList<IElement> GetChildren() => FindManyByXPath("//*");
}
