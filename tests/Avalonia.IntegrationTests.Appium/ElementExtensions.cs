using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Runtime.InteropServices;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Interfaces;
using OpenQA.Selenium.Interactions;
using Xunit;

namespace Avalonia.IntegrationTests.Appium
{
    internal static class ElementExtensions
    {
        public static IWebElement FindElementByAccessibilityId(this ISearchContext context, string id)
        {
            return context.FindElement(MobileBy.AccessibilityId(id));
        }
        
        public static IWebElement FindElementByName(this ISearchContext context, string name)
        {
            return context.FindElement(MobileBy.Name(name));
        }
        
        public static IWebElement FindElementByXPath(this ISearchContext context, string path)
        {
            return context.FindElement(MobileBy.XPath(path));
        }
        
        public static ReadOnlyCollection<IWebElement> FindElementsByXPath(this ISearchContext context, string path)
        {
            return context.FindElements(MobileBy.XPath(path));
        }
        
        public static IReadOnlyList<IWebElement> GetChildren(this IWebElement element) =>
            element.FindElements(By.XPath("*/*"));

        public static (IWebElement close, IWebElement minimize, IWebElement maximize) GetChromeButtons(this IWebElement window)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                var closeButton = window.FindElement(By.XPath("//XCUIElementTypeButton[1]"));
                var fullscreenButton = window.FindElement(By.XPath("//XCUIElementTypeButton[2]"));
                var minimizeButton = window.FindElement(By.XPath("//XCUIElementTypeButton[3]"));
                return (closeButton, minimizeButton, fullscreenButton);
            }

            throw new NotSupportedException("GetChromeButtons not supported on this platform.");
        }

        public static string GetComboBoxValue(this IWebElement element)
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
                element.Text :
                element.GetAttribute("value");
        }
        
        public static string GetName(this IWebElement element) => GetAttribute(element, "Name", "title");

        public static bool? GetIsChecked(this IWebElement element) =>
            GetAttribute(element, "Toggle.ToggleState", "value") switch
            {
                "0" => false,
                "1" => true,
                "2" => null,
                _ => throw new ArgumentOutOfRangeException($"Unexpected IsChecked value.")
            };

        public static bool GetIsFocused(this IWebElement element)
        {
            var e = (AppiumElement)element;
            
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var active = e.WrappedDriver.SwitchTo().ActiveElement() as AppiumElement;
                return e.Id == active?.Id;
            }
            else
            {
                // https://stackoverflow.com/questions/71807788/check-if-element-is-focused-in-appium
                throw new NotSupportedException("Couldn't work out how to check if an element is focused on mac.");
            }
        }

        public static Screenshot GetScreenshot(this IWebElement element)
        {
            return ((AppiumElement)element).GetScreenshot();
        }
        
        /// <summary>
        /// Clicks a button which is expected to open a new window.
        /// </summary>
        /// <param name="button">The button to click.</param>
        /// <returns>
        /// An object which when disposed will cause the newly opened window to close.
        /// </returns>
        public static IDisposable OpenWindowWithClick(this IWebElement button)
        {
            var element = (AppiumElement)button;
            var session = element.WrappedDriver;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var oldHandle = session.CurrentWindowHandle;
                var oldHandles = session.WindowHandles.ToList();
                var oldChildWindows = session.FindElements(By.XPath("//Window"));

                element.Click();

                var newHandle = session.WindowHandles.Except(oldHandles).SingleOrDefault();

                if (newHandle is not null)
                {
                    // A new top-level window was opened. We need to switch to it.
                    session.SwitchTo().Window(newHandle);

                    return Disposable.Create(() =>
                    {
                        session.Close();
                        session.SwitchTo().Window(oldHandle);
                    });
                }
                else
                {
                    // If a new window handle hasn't been added to the session then it's likely
                    // that a child window was opened. These don't appear in session.WindowHandles
                    // so we have to use an XPath query to get hold of it.
                    var newChildWindows = session.FindElements(By.XPath("//Window"));
                    var childWindow = Assert.Single(newChildWindows.Except(oldChildWindows));

                    return Disposable.Create(() =>
                    {
                        childWindow.SendKeys(Keys.Alt + Keys.F4 + Keys.Alt);
                    });
                }
            }
            else
            {
                var oldWindows = session.FindElements(By.XPath("/XCUIElementTypeApplication/XCUIElementTypeWindow"));
                var oldWindowTitles = oldWindows.ToDictionary(x => x.Text);

                element.Click();
                
                // Wait for animations to run.
                Thread.Sleep(1000);

                var newWindows = session.FindElements(By.XPath("/XCUIElementTypeApplication/XCUIElementTypeWindow"));
                
                // Try to find the new window by looking for a window with a title that didn't exist before the button
                // was clicked. Sometimes it seems that when a window becomes fullscreen, all other windows in the
                // application lose their titles, so filter out windows with no title (this may have started happening
                // with macOS 13.1?)
                var newWindowTitles = newWindows
                    .Select(x => (x.Text, x))
                    .Where(x => !string.IsNullOrEmpty(x.Text))
                    .ToDictionary(x => x.Text, x => x.x);

                var newWindowTitle = Assert.Single(newWindowTitles.Keys.Except(oldWindowTitles.Keys));

                return Disposable.Create(() =>
                {
                    // TODO: We should be able to use Cmd+W here but Avalonia apps don't seem to have this shortcut
                    // set up by default.
                    var windows = session.FindElements(By.XPath("/XCUIElementTypeApplication/XCUIElementTypeWindow"));
                    var text = windows.Select(x => x.Text).ToList();
                    var newWindow = session.FindElements(By.XPath("/XCUIElementTypeApplication/XCUIElementTypeWindow"))
                        .First(x => x.Text == newWindowTitle);
                    var (close, _, _) = ((IWebElement)newWindow).GetChromeButtons();
                    close!.Click();
                    Thread.Sleep(1000);
                });
            }
        }

        public static void SendClick(this IWebElement element)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // The Click() method seems to correspond to accessibilityPerformPress on macOS but certain controls
                // such as list items don't support this action, so instead simulate a physical click as VoiceOver
                // does.
                var e = (AppiumElement)element;
                new Actions(e.WrappedDriver).MoveToElement(element).Click().Perform();
            }
            else
            {
                element.Click();
            }
        }

        public static void MovePointerOver(this IWebElement element)
        {
            var e = (AppiumElement)element;
            new Actions(e.WrappedDriver).MoveToElement(element).Perform();
        }

        public static string GetAttribute(IWebElement element, string windows, string macOS)
        {
            return element.GetAttribute(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? windows : macOS);
        }
    }
}
