using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using Avalonia.IntegrationTests.Appium.Wrappers;
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
                Driver = new WindowsDriver<AppiumWebElement>(
                    new Uri("http://127.0.0.1:4723"),
                    options);

                Session = new MacSession(Driver);

                // https://github.com/microsoft/WinAppDriver/issues/1025
                SetForegroundWindow(new IntPtr(int.Parse(
                    Driver.WindowHandles[0].Substring(2),
                    NumberStyles.AllowHexSpecifier)));
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                ConfigureMacOptions(options);
                Driver = new MacDriver<AppiumWebElement>(
                    new Uri("http://127.0.0.1:4723/wd/hub"),
                    options);
                Session = new MacSession(Driver);
            }
            else
            {
                throw new NotSupportedException("Unsupported platform.");
            }
        }

        protected virtual void ConfigureWin32Options(AppiumOptions options)
        {
            var path = Path.GetFullPath(TestAppPath);
            options.AddAdditionalCapability(MobileCapabilityType.App, path);
            options.AddAdditionalCapability(MobileCapabilityType.PlatformName, MobilePlatform.Windows);
            options.AddAdditionalCapability(MobileCapabilityType.DeviceName, "WindowsPC");
        }

        protected virtual void ConfigureMacOptions(AppiumOptions options)
        {
            options.AddAdditionalCapability("appium:bundleId", TestAppBundleId);
            options.AddAdditionalCapability(MobileCapabilityType.PlatformName, MobilePlatform.MacOS);
            options.AddAdditionalCapability(MobileCapabilityType.AutomationName, "mac2");
            options.AddAdditionalCapability("appium:showServerLogs", false);
        }

        public AppiumDriver<AppiumWebElement> Driver { get; }
        
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
