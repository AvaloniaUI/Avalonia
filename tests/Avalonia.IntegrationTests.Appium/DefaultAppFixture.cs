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
    public class DefaultAppFixture : IDisposable
    {
        private const string TestAppPath = @"..\..\..\..\..\samples\IntegrationTestApp\bin\Debug\net8.0\IntegrationTestApp.exe";
        private const string TestAppBundleId = "net.avaloniaui.avalonia.integrationtestapp";

        public DefaultAppFixture()
        {
            var options = new AppiumOptions();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                ConfigureWin32Options(options);
                Session = new WindowsDriver(
                    new Uri("http://127.0.0.1:4723"),
                    options);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                ConfigureMacOptions(options);
                Session = new MacDriver(
                    new Uri("http://127.0.0.1:4723"),
                    options);
            }
            else
            {
                throw new NotSupportedException("Unsupported platform.");
            }
        }

        protected virtual void ConfigureWin32Options(AppiumOptions options)
        {
            var path = Path.GetFullPath(TestAppPath);
            options.App = path;
            options.AutomationName = "FlaUI";
        }

        protected virtual void ConfigureMacOptions(AppiumOptions options)
        {
            options.AddAdditionalAppiumOption("appium:bundleId", TestAppBundleId);
            options.AddAdditionalAppiumOption("appium:showServerLogs", true);
            options.AutomationName = "mac2";
            options.PlatformName = MobilePlatform.MacOS;
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
