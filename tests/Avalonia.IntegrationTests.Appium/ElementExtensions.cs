using System;
using System.Runtime.InteropServices;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.MultiTouch;
using OpenQA.Selenium.Interactions;

namespace Avalonia.IntegrationTests.Appium
{
    internal static class ElementExtensions
    {
        public static string GetName(this AppiumWebElement element) => GetAttribute(element, "Name", "title");

        public static bool? GetIsChecked(this AppiumWebElement element) =>
            GetAttribute(element, "Toggle.ToggleState", "value") switch
            {
                "0" => false,
                "1" => true,
                "2" => null,
                _ => throw new ArgumentOutOfRangeException($"Unexpected IsChecked value.")
            };

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
                var action = new Actions(element.WrappedDriver);
                action.MoveToElement(element);
                action.Click();
                action.Perform();
            }
        }

        public static string GetAttribute(AppiumWebElement element, string windows, string macOS)
        {
            return element.GetAttribute(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? windows : macOS);
        }
    }
}
