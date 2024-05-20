using System;
using System.Collections.ObjectModel;
using OpenQA.Selenium;

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
/// - The macOS driver has no concept of window handles
/// - Even when a WebDriver implementation does support the concept of a "current browsing context",
/// child windows are not always represented as separate windows in the WebDriver API. For example,
/// the WebDriver API for Windows does not have a way to switch to a child window. Instead, child
/// windows are represented as children of the current window.
/// 
/// This class and its derived implementations abstract the complexity in handling these special
/// cases.
/// </remarks>
internal abstract class OpenedWindowContext : ISearchContext, IDisposable
{
    /// <summary>
    /// Closes the window and switches the context back to the previous window.
    /// </summary>
    public abstract void Dispose();

    /// <summary>
    /// Invokes an interaction that opens a new window and returns a new
    /// <see cref="OpenedWindowContext"/> representing the window opened by that interaction.
    /// </summary>
    /// <param name="session">The session.</param>
    /// <param name="interaction">The interaction.</param>
    public static OpenedWindowContext OpenFromInteraction(IWebDriver session, Action interaction)
    {
        return OperatingSystem.IsMacOS() ? 
            MacOSOpenedWindowContext.OpenFromInteraction(session, interaction) : 
            StandardOpenedWindowContext.OpenFromInteraction(session, interaction);
    }

    public abstract IWebElement FindElement(By by);

    public abstract ReadOnlyCollection<IWebElement> FindElements(By by);
}
