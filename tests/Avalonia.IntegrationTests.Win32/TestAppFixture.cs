using System;
using System.IO;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Windows;

namespace Avalonia.IntegrationTests.Win32
{
    public class TestAppFixture : IDisposable
    {
        private const string WindowsApplicationDriverUrl = "http://127.0.0.1:4723";
        private const string TestAppPath = @"..\..\..\..\..\samples\IntegrationTestApp\bin\Debug\netcoreapp3.1\IntegrationTestApp.exe";

        public TestAppFixture()
        {
            var opts = new AppiumOptions();
            var path = Path.GetFullPath(TestAppPath);
            opts.AddAdditionalCapability("app", path);
            opts.AddAdditionalCapability("deviceName", "WindowsPC");
            Session = new WindowsDriver<WindowsElement>(
                new Uri(WindowsApplicationDriverUrl),
                opts);
        }

        public WindowsDriver<WindowsElement> Session { get; }

        public void Dispose() => Session.Close();
    }
}
