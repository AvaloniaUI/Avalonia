using System;
using System.Runtime.InteropServices;
using OpenQA.Selenium.Appium;

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

        public static string GetAttribute(AppiumWebElement element, string windows, string macOS)
        {
            return element.GetAttribute(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? windows : macOS);
        }
    }
}
