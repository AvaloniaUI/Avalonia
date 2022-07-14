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
                opts.App = path;
                opts.DeviceName = "WindowsPC";
                opts.AutomationName = "windows";
                opts.AddAdditionalAppiumOption(MobileCapabilityType.PlatformName, MobilePlatform.Windows);
                opts.AddAdditionalAppiumOption(MobileCapabilityType.NewCommandTimeout, 10000);

                Session = new WindowsDriver(
                    new Uri("http://127.0.0.1:4723"),
                    opts);

                // https://github.com/microsoft/WinAppDriver/issues/1025
                SetForegroundWindow(new IntPtr(int.Parse(
                    Session.WindowHandles[0].Substring(2),
                    NumberStyles.AllowHexSpecifier)));
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                opts.AutomationName = "mac2";
                opts.AddAdditionalAppiumOption("appium:bundleId", TestAppBundleId);
                opts.AddAdditionalAppiumOption(MobileCapabilityType.PlatformName, MobilePlatform.MacOS);
                opts.AddAdditionalAppiumOption("appium:showServerLogs", true);
                opts.AddAdditionalAppiumOption(MobileCapabilityType.NewCommandTimeout, 10000);

                Session = new MacDriver(
                    new Uri("http://127.0.0.1:4723"),
                    opts);
            }
            else
            {
                throw new NotSupportedException("Unsupported platform.");
            }
        }

        public AppiumDriver Session { get; }

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
