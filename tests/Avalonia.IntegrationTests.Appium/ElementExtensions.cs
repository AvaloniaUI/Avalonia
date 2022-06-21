using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Runtime.InteropServices;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Interactions;
using Xunit;

namespace Avalonia.IntegrationTests.Appium
{
    internal static class ElementExtensions
    {
        public static IReadOnlyList<AppiumWebElement> GetChildren(this AppiumWebElement element) =>
            element.FindElementsByXPath("*/*");

        public static string GetComboBoxValue(this AppiumWebElement element)
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
                element.Text :
                element.GetAttribute("value");
        }
        
        public static string GetName(this AppiumWebElement element) => GetAttribute(element, "Name", "title");

        public static bool? GetIsChecked(this AppiumWebElement element) =>
            GetAttribute(element, "Toggle.ToggleState", "value") switch
            {
                "0" => false,
                "1" => true,
                "2" => null,
                _ => throw new ArgumentOutOfRangeException($"Unexpected IsChecked value.")
            };

        public static bool GetIsFocused(this AppiumWebElement element)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var active = element.WrappedDriver.SwitchTo().ActiveElement() as AppiumWebElement;
                return element.Id == active?.Id;
            }
            else
            {
                // https://stackoverflow.com/questions/71807788/check-if-element-is-focused-in-appium
                throw new NotSupportedException("Couldn't work out how to check if an element is focused on mac.");
            }
        }

        /// <summary>
        /// Clicks a button which is expected to open a new window.
        /// </summary>
        /// <param name="element">The button to click.</param>
        /// <returns>
        /// An object which when disposed will cause the newly opened window to close.
        /// </returns>
        public static IDisposable OpenWindowWithClick(this AppiumWebElement element)
        {
            var session = element.WrappedDriver;
            var oldHandle = session.CurrentWindowHandle;
            var oldHandles = session.WindowHandles.ToList();
            var oldChildWindows = session.FindElements(By.XPath("//Window"));

            element.Click();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
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
                var newHandle = session.CurrentWindowHandle;

                Assert.NotEqual(oldHandle, newHandle);

                return Disposable.Create(() =>
                {
                    session.Close();
                    session.SwitchTo().Window(oldHandle);
                });
            }
        }

        public static void SendClick(this AppiumWebElement element)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                element.Click();
            }
            else
            {
                // The Click() method seems to correspond to accessibilityPerformPress on macOS but certain controls
                // such as list items don't support this action, so instead simulate a physical click as VoiceOver
                // does.
                new Actions(element.WrappedDriver).MoveToElement(element).Click().Perform();
            }
        }

        public static void MovePointerOver(this AppiumWebElement element)
        {
            new Actions(element.WrappedDriver).MoveToElement(element).Perform();
        }

        public static string GetAttribute(AppiumWebElement element, string windows, string macOS)
        {
            return element.GetAttribute(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? windows : macOS);
        }
    }
}
