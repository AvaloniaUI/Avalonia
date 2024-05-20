using System;
using System.Collections.ObjectModel;
using System.Linq;
using OpenQA.Selenium;

namespace Avalonia.IntegrationTests.Appium;

/// <summary>
/// An implementation of <see cref="OpenedWindowContext"/> for WebDriver implementations that support
/// a "current browsing context" (aka "Current Window").
/// </summary>
internal class StandardOpenedWindowContext : OpenedWindowContext
{
    private readonly IWebDriver _session;
    private readonly string _previousWindowHandle;
    private readonly IWebElement? _element;

    private StandardOpenedWindowContext(
        IWebDriver session,
        string previousWindowHandle,
        IWebElement? element)
    {
        _session = session;
        _previousWindowHandle = previousWindowHandle;
        _element = element;
    }

    public override void Dispose()
    {
        if (_element is not null)
        {
            _element.CloseWindow();
        }
        else
        {
            _session.Close();
            _session.SwitchTo().Window(_previousWindowHandle);
        }
    }

    public new static StandardOpenedWindowContext OpenFromInteraction(IWebDriver session, Action interaction)
    {
        var oldWindowHandle = session.CurrentWindowHandle;
        var oldWindowHandles = session.WindowHandles.ToList();

        interaction();

        var newWindowHandles = session.WindowHandles.ToList();

        if (newWindowHandles.Count > oldWindowHandles.Count + 1)
            throw new InvalidOperationException("Multiple windows were opened.");

        if (newWindowHandles.Count == oldWindowHandles.Count + 1)
        {
            // A new top-level window was opened. We need to switch to it.
            var newWindowHandle = newWindowHandles.Except(oldWindowHandles).Single();
            session.SwitchTo().Window(newWindowHandle);
            return new(session, oldWindowHandle, null);
        }
        else
        {
            // If a new window handle hasn't been added to the session then it's likely
            // that a child window was opened. These will appear as children of the current window.
            if (session.FindElements(By.TagName("Window")).SingleOrDefault() is { } childWindow)
                return new(session, oldWindowHandle, childWindow);
        }

        throw new NoSuchElementException("No new window was opened.");
    }

    public override IWebElement FindElement(By by)
    {
        return (((ISearchContext?)_element) ?? _session).FindElement(by);
    }

    public override ReadOnlyCollection<IWebElement> FindElements(By by)
    {
        return (((ISearchContext?)_element) ?? _session).FindElements(by);
    }
}
