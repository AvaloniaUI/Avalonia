using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using Avalonia.IntegrationTests.Appium.Crapium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Enums;
using OpenQA.Selenium.Appium.Mac;
using OpenQA.Selenium.Appium.Windows;

namespace Avalonia.IntegrationTests.Appium
{
    public class DefaultAppFixture : IDisposable
    {
        private const string TestAppPath = @"..\..\..\..\..\samples\IntegrationTestApp\bin\Debug\net7.0\IntegrationTestApp.exe";
        private const string TestAppBundleId = "net.avaloniaui.avalonia.integrationtestapp";

        public DefaultAppFixture()
        {
            var options = new AppiumOptions();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                ConfigureWin32Options(options);
                Driver = new WindowsDriver(
                    new Uri("http://127.0.0.1:4723"),
                    options);

                // https://github.com/microsoft/WinAppDriver/issues/1025
                SetForegroundWindow(new IntPtr(int.Parse(
                    Driver.WindowHandles[0].Substring(2),
                    NumberStyles.AllowHexSpecifier)));
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                ConfigureMacOptions(options);
                var driver = new MacDriver(
                    new Uri("http://127.0.0.1:4723"),
                    options);
                Driver = driver;
                Session = new MacSession(driver);
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
            options.AutomationName = "Windows";
            options.PlatformName = MobilePlatform.Windows;
            options.DeviceName = "WindowsPC";            
        }

        protected virtual void ConfigureMacOptions(AppiumOptions options)
        {
            options.AutomationName = "Mac2";
            options.PlatformName = MobilePlatform.MacOS;
            options.AddAdditionalAppiumOption("appium:bundleId", TestAppBundleId);
            options.AddAdditionalAppiumOption("appium:showServerLogs", true);
        }

        public AppiumDriver Driver { get; }
        public ISession Session { get; }
        
        public void Dispose()
        {
            try
            {
                Driver.Close();
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
