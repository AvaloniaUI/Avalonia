using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Enums;
using OpenQA.Selenium.Appium.Mac;
using OpenQA.Selenium.Appium.Windows;

namespace Avalonia.IntegrationTests.Appium
{
    public class TestAppFixture : IDisposable
    {
        private const string TestAppPath = @"..\..\..\..\..\samples\IntegrationTestApp\bin\Debug\net6.0\IntegrationTestApp.exe";
        private const string TestAppBundleId = "net.avaloniaui.avalonia.integrationtestapp";

        public TestAppFixture()
        {
            var opts = new AppiumOptions();
            var path = Path.GetFullPath(TestAppPath);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                opts.AddAdditionalCapability(MobileCapabilityType.App, path);
                opts.AddAdditionalCapability(MobileCapabilityType.PlatformName, MobilePlatform.Windows);
                opts.AddAdditionalCapability(MobileCapabilityType.DeviceName, "WindowsPC");

                Session = new WindowsDriver<AppiumWebElement>(
                    new Uri("http://127.0.0.1:4723"),
                    opts);

                // https://github.com/microsoft/WinAppDriver/issues/1025
                SetForegroundWindow(new IntPtr(int.Parse(
                    Session.WindowHandles[0].Substring(2),
                    NumberStyles.AllowHexSpecifier)));
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                opts.AddAdditionalCapability("appium:bundleId", TestAppBundleId);
                opts.AddAdditionalCapability(MobileCapabilityType.PlatformName, MobilePlatform.MacOS);
                opts.AddAdditionalCapability(MobileCapabilityType.AutomationName, "mac2");
                opts.AddAdditionalCapability("appium:showServerLogs", true);

                Session = new MacDriver<AppiumWebElement>(
                    new Uri("http://127.0.0.1:4723/wd/hub"),
                    opts);
            }
            else
            {
                throw new NotSupportedException("Unsupported platform.");
            }
        }

        public AppiumDriver<AppiumWebElement> Session { get; }

        public void Dispose()
        {
            try
            {
                Session.Close();
            }
            catch
            {
                // Closing the session currently seems to crash the mac2 driver.
            }
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
    }
}
