using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Runtime.InteropServices;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Interactions;
using Xunit;

namespace Avalonia.IntegrationTests.Appium
{
    public record class WindowChrome(
        AppiumElement? Close,
        AppiumElement? Minimize,
        AppiumElement? Maximize,
        AppiumElement? FullScreen);

    internal static class ElementExtensions
    {
        public static IReadOnlyList<AppiumElement> GetChildren(this AppiumElement element) =>
            element.FindElements(MobileBy.XPath("*/*"));

        public static WindowChrome GetChromeButtons(this AppiumElement window)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                var closeButton = window.FindElements(MobileBy.AccessibilityId("_XCUI:CloseWindow")).FirstOrDefault();
                var fullscreenButton = window.FindElements(MobileBy.AccessibilityId("_XCUI:FullScreenWindow")).FirstOrDefault();
                var minimizeButton = window.FindElements(MobileBy.AccessibilityId("_XCUI:MinimizeWindow")).FirstOrDefault();
                var zoomButton = window.FindElements(MobileBy.AccessibilityId("_XCUI:ZoomWindow")).FirstOrDefault();
                return new(closeButton, minimizeButton, zoomButton, fullscreenButton);
            }

            throw new NotSupportedException("GetChromeButtons not supported on this platform.");
        }

        public static string GetComboBoxValue(this AppiumElement element)
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
                element.Text :
                element.GetAttribute("value");
        }
        
        public static string GetName(this AppiumElement element) => GetAttribute(element, "Name", "title");

        public static bool? GetIsChecked(this AppiumElement element)
        {
            var value = GetAttribute(element, "Toggle.ToggleState", "value");
            return value switch
            {
                "0" => false,
                "1" => true,
                "2" => null,
                "On" => true,
                "Off" => false,
                "Indeterminate" => null,
                _ => throw new ArgumentOutOfRangeException($"Unexpected IsChecked value.")
            };
        }

        public static bool GetIsFocused(this AppiumElement element)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var active = element.WrappedDriver.SwitchTo().ActiveElement() as AppiumElement;
                return element.Id == active?.Id;
            }
            else
            {
                // https://stackoverflow.com/questions/71807788/check-if-element-is-focused-in-appium
                throw new NotSupportedException("Couldn't work out how to check if an element is focused on mac.");
            }
        }

        public static void CloseWindow(this IWebElement element)
        {
            if (OperatingSystem.IsWindows())
                element.SendKeys(Keys.Alt + Keys.F4);
            else
                throw new NotImplementedException();
        }

        /// <summary>
        /// Clicks a button which is expected to open a new window.
        /// </summary>
        /// <param name="element">The button to click.</param>
        /// <returns>
        /// An object which when disposed will cause the newly opened window to close.
        /// </returns>
        public static OpenedWindowContext OpenWindowWithClick(this AppiumElement element)
        {
            return OpenedWindowContext.OpenFromInteraction(element.WrappedDriver, element.Click);
        }

        public static void SendClick(this AppiumElement element)
        {
            // The Click() method seems to correspond to accessibilityPerformPress on macOS but certain controls
            // such as list items don't support this action, so instead simulate a physical click as VoiceOver
            // does.
            if (OperatingSystem.IsMacOS())
            {
                new Actions(element.WrappedDriver).MoveToElement(element).Click().Perform();
            }
            else
            {
                element.Click();
            }
        }

        public static void MovePointerOver(this AppiumElement element)
        {
            new Actions(element.WrappedDriver).MoveToElement(element).Perform();
        }

        public static string GetAttribute(AppiumElement element, string windows, string macOS)
        {
            return element.GetAttribute(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? windows : macOS);
        }
    }
}
