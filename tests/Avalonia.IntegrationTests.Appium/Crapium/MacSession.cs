using System;
using System.Collections.Generic;
using System.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Enums;
using OpenQA.Selenium.Appium.Mac;
using Xunit;

namespace Avalonia.IntegrationTests.Appium.Crapium;

public class MacSession : ISession
{
    private static readonly Dictionary<Type, Func<IWebElement, object>> s_factory = new()
    {
        { typeof(IElement), x => new UIElement(x) }
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
                "/XCUIElementTypeApplication/XCUIElementTypeWindow")
                .Select(x => new UiWindowElement(x))
                .ToList();
        }
    }
    
    public IWindowElement GetWindow(string id)
    {
        return new UiWindowElement(
            _driver.FindElementByXPath(
                $"/XCUIElementTypeApplication/XCUIElementTypeWindow/XCUIElementTypeWindow[@identifier='{id}']/parent::XCUIElementTypeWindow"));
    }
    
    /// <summary>
    /// Remember to sleep in open window on macos
    /// </summary>
    /// <param name="openWindow"></param>
    /// <returns></returns>
    public IWindowElement GetNewWindow(Action openWindow)
    { 
        // Store the old window.
        var oldWindows = Windows.ToList();
        var oldWindowTitles = oldWindows.ToDictionary(x => x.Name);

        // open window with click
        openWindow();

        // find the new window
        var newWindows = Windows.ToList();
                
        // Try to find the new window by looking for a window with a title that didn't exist before the button
        // was clicked. Sometimes it seems that when a window becomes fullscreen, all other windows in the
        // application lose their titles, so filter out windows with no title (this may have started happening
        // with macOS 13.1?)
        var newWindowTitles = newWindows
            .Select(x => (x.Value, x))
            .Where(x => !string.IsNullOrEmpty(x.Value))
            .ToDictionary(x => x.Value, x => x.x);

        return Assert.Single(newWindowTitles.Where(x => !oldWindows.Contains(x.Value))).Value;
    }

    internal static Func<IWebElement, object> GetElementFactory()
    {
        if (s_factory.TryGetValue(typeof(IElement), out var f))
            return f;
        throw new NotSupportedException("Unexpected IElement type.");
    }
}
