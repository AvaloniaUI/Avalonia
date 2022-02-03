using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;

namespace Avalonia.IntegrationTests.Appium
{
    public class TestAppFixture : IDisposable
    {
        private const string TestAppPath = @"..\..\..\..\..\samples\IntegrationTestApp\bin\Debug\net6.0\IntegrationTestApp.exe";
        private const string TestAppBundleId = "net.avaloniaui.avalonia.integrationtestapp";
        public class AppiumCapabilities : DesiredCapabilities
        {
            /// <summary>
            /// Get the capabilities back as a dictionary
            ///
            /// This method uses Reflection and should be removed once
            /// AppiumOptions class is avalaible for each driver
            /// </summary>
            /// <returns></returns>
            public Dictionary<string, object> ToDictionary()
            {
                var bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic;
                FieldInfo capsField = typeof(DesiredCapabilities)
                        .GetField("capabilities", bindingFlags);

                return capsField?.GetValue(this) as Dictionary<string, object>;
            }
        }
        public class AppiumOptions : DriverOptions
        {
            /// <summary>
            /// The dictionary of capabilities
            /// </summary>
            private readonly AppiumCapabilities capabilities = new AppiumCapabilities();

            /// <summary>
            /// Add new capabilities
            /// </summary>
            /// <param name="capabilityName">Capability name</param>
            /// <param name="capabilityValue">Capabilities value, which cannot be null or empty</param>
            public override void AddAdditionalCapability(string capabilityName, object capabilityValue)
            {
                if (string.IsNullOrEmpty(capabilityName))
                {
                    throw new ArgumentException("Capability name may not be null an empty string.", "capabilityName");
                }

                this.capabilities[capabilityName] = capabilityValue;
            }

            /// <summary>
            /// Turn the capabilities into an desired capability
            /// </summary>
            /// <returns>A desired capability</returns>
            public override ICapabilities ToCapabilities()
            {
                return this.capabilities;
            }

            public Dictionary<string, object> ToDictionary()
            {
                return this.capabilities.ToDictionary();
            }
        }
       public class AvaloniaWebDriver : RemoteWebDriver
        {
            public AvaloniaWebDriver(DriverOptions options) : base(options)
            {
            }

            public AvaloniaWebDriver(ICapabilities desiredCapabilities) : base(desiredCapabilities)
            {
            }

            public AvaloniaWebDriver(Uri remoteAddress, DriverOptions options) : base(remoteAddress, options)
            {
            }

            public AvaloniaWebDriver(Uri remoteAddress, ICapabilities desiredCapabilities) : base(remoteAddress, desiredCapabilities)
            {
            }

            public AvaloniaWebDriver(ICommandExecutor commandExecutor, ICapabilities desiredCapabilities) : base(commandExecutor, desiredCapabilities)
            {
            }

            public AvaloniaWebDriver(Uri remoteAddress, ICapabilities desiredCapabilities, TimeSpan commandTimeout) : base(remoteAddress, desiredCapabilities, commandTimeout)
            {
            }
            public IWebElement FindElementByAccessibilityId(string value) => base.FindElement("accessibility id", value);
        }
        public TestAppFixture()
        {
            var opts = new AppiumOptions();
            var path = Path.GetFullPath(TestAppPath);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                opts.AddAdditionalCapability("app", path);
                opts.AddAdditionalCapability("platformName", "Windows");
                opts.AddAdditionalCapability("deviceName", "WindowsPC");

                Session = new AvaloniaWebDriver(
                    new Uri("http://127.0.0.1:4723"),
                    opts);

                // https://github.com/microsoft/WinAppDriver/issues/1025
                SetForegroundWindow(new IntPtr(int.Parse(
                    Session.WindowHandles[0].Substring(2),
                    NumberStyles.AllowHexSpecifier)));
            }
            //else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            //{
            //    opts.AddAdditionalCapability("appium:bundleId", TestAppBundleId);
            //    opts.AddAdditionalCapability(MobileCapabilityType.PlatformName, MobilePlatform.MacOS);
            //    opts.AddAdditionalCapability(MobileCapabilityType.AutomationName, "mac2");
            //    opts.AddAdditionalCapability("appium:showServerLogs", true);

            //    Session = new MacDriver<AppiumWebElement>(
            //        new Uri("http://127.0.0.1:4723/wd/hub"),
            //        opts);
            //}
            //else
            //{
            //    throw new NotSupportedException("Unsupported platform.");
            //}
        }

        public AvaloniaWebDriver Session { get; }

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
