using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;

namespace Avalonia.IntegrationTests.Appium
{
    internal static class ElementExtensions
    {
        public static IReadOnlyList<IWebElement> GetChildren(this IWebElement element) =>
            element.FindElements(By.XPath("*/*"));

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

        public static void SendClick(this IWebElement element)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                element.Click();
            }
            else
            {
                //// The Click() method seems to correspond to accessibilityPerformPress on macOS but certain controls
                //// such as list items don't support this action, so instead simulate a physical click as VoiceOver
                //// does.
                //new Actions(element.dr).MoveToElement(element).Click().Perform();
            }
        }

        public static string GetAttribute(IWebElement element, string windows, string macOS)
        {
            return element.GetAttribute(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? windows : macOS);
        }
    }
}
