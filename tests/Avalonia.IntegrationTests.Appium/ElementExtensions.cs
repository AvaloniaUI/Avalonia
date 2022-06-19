using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Interactions;

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
