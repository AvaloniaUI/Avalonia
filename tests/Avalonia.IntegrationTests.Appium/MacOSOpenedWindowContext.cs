using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using Xunit;

namespace Avalonia.IntegrationTests.Appium;

internal class MacOSOpenedWindowContext : OpenedWindowContext
{
    private readonly IWebDriver _session;
    private readonly string _newWindowTitle;

    private MacOSOpenedWindowContext(IWebDriver session, string newWindowTitle)
    {
        _session = session;
        _newWindowTitle = newWindowTitle;
    }

    public override void Dispose()
    {
        // TODO: We should be able to use Cmd+W here but Avalonia apps don't seem to have this shortcut
        // set up by default.
        var windows = _session.FindElements(MobileBy.XPath("/XCUIElementTypeApplication/XCUIElementTypeWindow"));
        var text = windows.Select(x => x.Text).ToList();
        var newWindow = _session.FindElements(MobileBy.XPath("/XCUIElementTypeApplication/XCUIElementTypeWindow"))
            .First(x => x.Text == _newWindowTitle);
        var close = ((AppiumElement)newWindow).FindElement(MobileBy.AccessibilityId("_XCUI:CloseWindow"));
        close!.Click();
        Thread.Sleep(1000);
    }

    public new static MacOSOpenedWindowContext OpenFromInteraction(IWebDriver session, Action interaction)
    {
        var oldWindows = session.FindElements(MobileBy.XPath("/XCUIElementTypeApplication/XCUIElementTypeWindow"));
        var oldWindowTitles = oldWindows.ToDictionary(x => x.Text);

        interaction();

        // Wait for animations to run.
        Thread.Sleep(1000);

        var newWindows = session.FindElements(MobileBy.XPath("/XCUIElementTypeApplication/XCUIElementTypeWindow"));

        // Try to find the new window by looking for a window with a title that didn't exist before the button
        // was clicked. Sometimes it seems that when a window becomes full-screen, all other windows in the
        // application lose their titles, so filter out windows with no title (this may have started happening
        // with macOS 13.1?)
        var newWindowTitles = newWindows
            .Select(x => (x.Text, x))
            .Where(x => !string.IsNullOrEmpty(x.Text))
            .ToDictionary(x => x.Text, x => x.x);

        var newWindowTitle = Assert.Single(newWindowTitles.Keys.Except(oldWindowTitles.Keys));
        return new(session, newWindowTitle);
    }

    public override IWebElement FindElement(By by)
    {
        return _session.FindElement(by);
    }

    public override ReadOnlyCollection<IWebElement> FindElements(By by)
    {
        return _session.FindElements(by);
    }
}
