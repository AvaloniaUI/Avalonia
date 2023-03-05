using System;
using System.Collections.Generic;
using System.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Enums;
using OpenQA.Selenium.Appium.Mac;

namespace Avalonia.IntegrationTests.Appium.Crapium;

public class MacSession : ISession
{
    private static readonly Dictionary<Type, Func<IWebElement, object>> s_factory = new()
    {
        { typeof(IElement), x => new AppiumElement(x) }
    };

    private readonly MacDriver _driver;
    
    public MacSession(string bundleId)
    {
        var options = new AppiumOptions
        {
            AutomationName = "Mac2",
            PlatformName = MobilePlatform.MacOS,
        };

        options.AddAdditionalAppiumOption("appium:bundleId", bundleId);
        options.AddAdditionalAppiumOption("appium:showServerLogs", true);

        _driver = new MacDriver(
                new Uri("http://127.0.0.1:4723"),
                options);
    }

    public MacSession(MacDriver driver) => _driver = driver;
    
    public IEnumerable<IWindowElement> Windows
    {
        get
        {
            // Double XCUIElementTypeWindow is a hack for Avalonia a11y returning 2 nested windows.
            return _driver.FindElementsByXPath(
                "/XCUIElementTypeApplication/XCUIElementTypeWindow/XCUIElementTypeWindow")
                .Select(x => new AppiumWindowElement(x))
                .ToList();
        }
    }

    public T FindElement<T>(string windowId, string elementId) where T : IElement
    {
        var f = GetElementFactory<T>();
        var e = _driver.FindElementByXPath(
            $"/XCUIElementTypeApplication/XCUIElementTypeWindow/" +
            $"XCUIElementTypeWindow[@identifier='{windowId}']//*[identifier='{elementId}']");
        return (T)f(e);
    }
    
    public IWindowElement GetWindow(string id)
    {
        return new AppiumWindowElement(
            _driver.FindElementByXPath(
                $"/XCUIElementTypeApplication/XCUIElementTypeWindow/XCUIElementTypeWindow[@identifier='{id}']"));
    }

    internal static Func<IWebElement, object> GetElementFactory<T>()
    {
        if (s_factory.TryGetValue(typeof(T), out var f))
            return f;
        throw new NotSupportedException("Unexpected IElement type.");
    }
}
