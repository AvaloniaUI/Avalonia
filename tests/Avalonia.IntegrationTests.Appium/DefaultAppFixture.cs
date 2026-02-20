using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Enums;

namespace Avalonia.IntegrationTests.Appium
{
    public class DefaultAppFixture : IDisposable
    {
        private const string TestAppPath = @"..\..\..\..\..\samples\IntegrationTestApp\bin\Debug\net10.0\IntegrationTestApp.exe";
        private const string TestAppBundleId = "net.avaloniaui.avalonia.integrationtestapp";
        protected const string TestAppPathLinux =
            @"../../../../../samples/IntegrationTestApp/bin/Debug/net10.0/IntegrationTestApp";

        public DefaultAppFixture()
        {
            var options = new AppiumOptions();

            if (OperatingSystem.IsWindows())
            {
                ConfigureWin32Options(options);
                Session = new WindowsDriver(
                    new Uri("http://127.0.0.1:4723"),
                    options);

                // https://github.com/microsoft/WinAppDriver/issues/1025
                SetForegroundWindow(new IntPtr(int.Parse(
                    Session.WindowHandles[0].Substring(2),
                    NumberStyles.AllowHexSpecifier)));
            }
            else if (OperatingSystem.IsMacOS())
            {
                ConfigureMacOptions(options);
                Session = new MacDriver(
                    new Uri("http://127.0.0.1:4723/wd/hub"),
                    options);
            }
            else if (OperatingSystem.IsLinux())
            {
                ConfigureLinuxOptions(options);
                var url = Environment.GetEnvironmentVariable("SELENIUM_REMOTE_URL")
                    ?? "http://127.0.0.1:4723";
                Session = new LinuxDriver(new Uri(url), options);
            }
            else
            {
                throw new PlatformNotSupportedException();
            }
        }

        protected virtual void ConfigureWin32Options(AppiumOptions options, string? app = null)
        {
            options.AddAdditionalCapability(MobileCapabilityType.App, app ?? Path.GetFullPath(TestAppPath));
            options.AddAdditionalCapability(MobileCapabilityType.PlatformName, MobilePlatform.Windows);
            options.AddAdditionalCapability(MobileCapabilityType.DeviceName, "WindowsPC");
        }

        protected virtual void ConfigureMacOptions(AppiumOptions options, string? app = null)
        {
            options.AddAdditionalCapability("appium:bundleId", app ?? TestAppBundleId);
            options.AddAdditionalCapability(MobileCapabilityType.PlatformName, MobilePlatform.MacOS);
            options.AddAdditionalCapability(MobileCapabilityType.AutomationName, "mac2");
            options.AddAdditionalCapability("appium:showServerLogs", true);
        }

        protected virtual void ConfigureLinuxOptions(AppiumOptions options, string? app = null)
        {
            var appPath = app ?? Path.GetFullPath(TestAppPathLinux);
#if APPIUM2
            options.App = appPath;
#else
            options.AddAdditionalCapability("app", appPath);
#endif
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

        public AppiumDriver CreateNestedSession(string appName)
        {
            var options = new AppiumOptions();
            if (OperatingSystem.IsWindows())
            {
                ConfigureWin32Options(options, appName);
            
                return new WindowsDriver(new Uri("http://127.0.0.1:4723"), options);
            }
            else if (OperatingSystem.IsMacOS())
            {
                ConfigureMacOptions(options, appName);
                return new MacDriver(new Uri("http://127.0.0.1:4723/wd/hub"), options);
            }
            else if (OperatingSystem.IsLinux())
            {
                ConfigureLinuxOptions(options, appName);
                var url = Environment.GetEnvironmentVariable("SELENIUM_REMOTE_URL")
                    ?? "http://127.0.0.1:4723";
                return new LinuxDriver(new Uri(url), options);
            }
            else
            {
                throw new PlatformNotSupportedException();
            }
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
    }
}
