using System;
using System.Collections.ObjectModel;
using System.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Interactions;

namespace Avalonia.IntegrationTests.Appium;

/// <summary>
/// Utility class for managing opened windows.
/// </summary>
/// <remarks>
/// There are a few special cases that need to be handled when working with Appium/WebDriver and
/// windows:
/// 
/// - Detecting a newly opened window isn't straightforward.
/// - The API to close an open window differs between Appium WebDriver implementations.
/// - While most WebDriver implementations have the the concept of a "current browsing context"
/// (aka "Current Window"), the macOS driver does not and all FindElement calls are global.
/// - Even when a WebDriver implementation does support the concept of a "current browsing context",
/// child windows are not always represented as separate windows in the WebDriver API. For example,
/// the WebDriver API for Windows does not have a way to switch to a child window. Instead, child
/// windows are represented as children of the current window.
/// 
/// This class abstracts the complexity in handling these special cases.
/// </remarks>
internal class OpenedWindowContext : ISearchContext, IDisposable
{
    private readonly IWebDriver _session;
    private readonly string _previousWindowHandle;
    private readonly IWebElement? _element;

    private OpenedWindowContext(
        IWebDriver session,
        string previousWindowHandle,
        IWebElement? element)
    {
        _session = session;
        _previousWindowHandle = previousWindowHandle;
        _element = element;
    }

    /// <summary>
    /// Closes the window and switches the context back to the previous window.
    /// </summary>
    public void Dispose()
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

    /// <summary>
    /// Invokes an interaction that opens a new window and returns a new
    /// <see cref="OpenedWindowContext"/> representing the window opened by that interaction.
    /// </summary>
    /// <param name="session">The session.</param>
    /// <param name="interaction">The interaction.</param>
    public static OpenedWindowContext OpenFromInteraction(IWebDriver session, Action interaction)
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

    public IWebElement FindElement(By by)
    {
        return (((ISearchContext?)_element) ?? _session).FindElement(by);
    }

    public ReadOnlyCollection<IWebElement> FindElements(By by)
    {
        return (((ISearchContext?)_element) ?? _session).FindElements(by);
    }
}
