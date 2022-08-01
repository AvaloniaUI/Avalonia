using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Avalonia.Controls;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Interactions;
using Xunit;

namespace Avalonia.IntegrationTests.Appium
{
    [Collection("Default")]
    public class WindowTests_MacOS
    {
        private readonly AppiumDriver<AppiumWebElement> _session;

        public WindowTests_MacOS(TestAppFixture fixture)
        {
            var retry = 0;

            _session = fixture.Session;

            for (;;)
            {
                try
                {
                    var tabs = _session.FindElementByAccessibilityId("MainTabs");
                    var tab = tabs.FindElementByName("Window");
                    tab.Click();
                    return;
                }
                catch (WebDriverException e) when (retry++ < 3)
                {
                    // MacOS sometimes seems to need a bit of time to get itself back in order after switching out
                    // of fullscreen.
                    Thread.Sleep(1000);
                }
            }

        }

        [PlatformFact(TestPlatforms.MacOS)]
        public void WindowOrder_Modal_Dialog_Stays_InFront_Of_Parent()
        {
            var mainWindow = _session.FindElementByAccessibilityId("MainWindow");

            using (OpenWindow(new PixelSize(200, 100), ShowWindowMode.Modal, WindowStartupLocation.Manual))
            {
                mainWindow.Click();

                var windows = _session.FindElements(By.XPath("XCUIElementTypeWindow"));
                var mainWindowIndex = GetWindowOrder(windows, "MainWindow");
                var secondaryWindowIndex = GetWindowOrder(windows, "SecondaryWindow");

                Assert.Equal(0, secondaryWindowIndex);
                Assert.Equal(1, mainWindowIndex);
            }
        }

        [PlatformFact(TestPlatforms.MacOS)]
        public void WindowOrder_Modal_Dialog_Stays_InFront_Of_Parent_When_Clicking_Resize_Grip()
        {
            var mainWindow = FindWindow(_session, "MainWindow");

            using (OpenWindow(new PixelSize(200, 100), ShowWindowMode.Modal, WindowStartupLocation.Manual))
            {
                new Actions(_session)
                    .MoveToElement(mainWindow, 100, 1)
                    .ClickAndHold()
                    .Perform();

                var windows = _session.FindElements(By.XPath("XCUIElementTypeWindow"));
                var mainWindowIndex = GetWindowOrder(windows, "MainWindow");
                var secondaryWindowIndex = GetWindowOrder(windows, "SecondaryWindow");

                new Actions(_session)
                    .MoveToElement(mainWindow, 100, 1)
                    .Release()
                    .Perform();

                Assert.Equal(0, secondaryWindowIndex);
                Assert.Equal(1, mainWindowIndex);
            }
        }

        [PlatformFact(TestPlatforms.MacOS)]
        public void WindowOrder_Modal_Dialog_Stays_InFront_Of_Parent_When_In_Fullscreen()
        {
            var mainWindow = FindWindow(_session, "MainWindow");
            var buttons = mainWindow.GetChromeButtons();

            buttons.maximize.Click();

            Thread.Sleep(500);

            try
            {
                using (OpenWindow(new PixelSize(200, 100), ShowWindowMode.Modal, WindowStartupLocation.Manual))
                {
                    var windows = _session.FindElements(By.XPath("XCUIElementTypeWindow"));
                    var mainWindowIndex = GetWindowOrder(windows, "MainWindow");
                    var secondaryWindowIndex = GetWindowOrder(windows, "SecondaryWindow");

                    Assert.Equal(0, secondaryWindowIndex);
                    Assert.Equal(1, mainWindowIndex);

                    Thread.Sleep(5000);
                }
            }
            finally
            {
                _session.FindElementByAccessibilityId("ExitFullscreen").Click();
            }
        }

        [PlatformFact(TestPlatforms.MacOS)]
        public void WindowOrder_Owned_Dialog_Stays_InFront_Of_Parent()
        {
            var mainWindow = _session.FindElementByAccessibilityId("MainWindow");

            using (OpenWindow(new PixelSize(200, 100), ShowWindowMode.Owned, WindowStartupLocation.Manual))
            {
                mainWindow.Click();

                var windows = _session.FindElements(By.XPath("XCUIElementTypeWindow"));
                var mainWindowIndex = GetWindowOrder(windows, "MainWindow");
                var secondaryWindowIndex = GetWindowOrder(windows, "SecondaryWindow");

                Assert.Equal(0, secondaryWindowIndex);
                Assert.Equal(1, mainWindowIndex);
            }
        }

        [PlatformFact(TestPlatforms.MacOS)]
        public void WindowOrder_NonOwned_Window_Does_Not_Stay_InFront_Of_Parent()
        {
            var mainWindow = _session.FindElementByAccessibilityId("MainWindow");

            using (OpenWindow(new PixelSize(800, 100), ShowWindowMode.NonOwned, WindowStartupLocation.Manual))
            {
                mainWindow.Click();

                var windows = _session.FindElements(By.XPath("XCUIElementTypeWindow"));
                var mainWindowIndex = GetWindowOrder(windows, "MainWindow");
                var secondaryWindowIndex = GetWindowOrder(windows, "SecondaryWindow");

                Assert.Equal(1, secondaryWindowIndex);
                Assert.Equal(0, mainWindowIndex);

                var sendToBack = _session.FindElementByAccessibilityId("SendToBack");
                sendToBack.Click();
            }
        }

        [PlatformFact(TestPlatforms.MacOS)]
        public void Parent_Window_Has_Disabled_ChromeButtons_When_Modal_Dialog_Shown()
        {
            var window = FindWindow(_session, "MainWindow");
            var (closeButton, miniaturizeButton, zoomButton) = window.GetChromeButtons();

            Assert.True(closeButton.Enabled);
            Assert.True(zoomButton.Enabled);
            Assert.True(miniaturizeButton.Enabled);

            using (OpenWindow(new PixelSize(200, 100), ShowWindowMode.Modal, WindowStartupLocation.CenterOwner))
            {
                Assert.False(closeButton.Enabled);
                Assert.False(zoomButton.Enabled);
                Assert.False(miniaturizeButton.Enabled);
            }
        }

        [PlatformFact(TestPlatforms.MacOS)]
        public void Minimize_Button_Is_Disabled_On_Modal_Dialog()
        {
            using (OpenWindow(new PixelSize(200, 100), ShowWindowMode.Modal, WindowStartupLocation.CenterOwner))
            {
                var secondaryWindow = FindWindow(_session, "SecondaryWindow");
                var (closeButton, miniaturizeButton, zoomButton) = secondaryWindow.GetChromeButtons();

                Assert.True(closeButton.Enabled);
                Assert.True(zoomButton.Enabled);
                Assert.False(miniaturizeButton.Enabled);
            }
        }

        [PlatformTheory(TestPlatforms.MacOS)]
        [InlineData(ShowWindowMode.NonOwned)]
        [InlineData(ShowWindowMode.Owned)]
        public void Minimize_Button_Minimizes_Window(ShowWindowMode mode)
        {
            using (OpenWindow(new PixelSize(200, 100), mode, WindowStartupLocation.Manual))
            {
                var secondaryWindow = FindWindow(_session, "SecondaryWindow");
                var (_, miniaturizeButton, _) = secondaryWindow.GetChromeButtons();

                miniaturizeButton.Click();
                Thread.Sleep(1000);

                var hittable = _session.FindElementsByXPath("/XCUIElementTypeApplication/XCUIElementTypeWindow")
                    .Select(x => x.GetAttribute("hittable")).ToList();
                Assert.Equal(new[] { "true", "false" }, hittable);

                _session.FindElementByAccessibilityId("RestoreAll").Click();
                Thread.Sleep(1000);

                hittable = _session.FindElementsByXPath("/XCUIElementTypeApplication/XCUIElementTypeWindow")
                    .Select(x => x.GetAttribute("hittable")).ToList();
                Assert.Equal(new[] { "true", "true" }, hittable);
            }
        }

        private IDisposable OpenWindow(PixelSize? size, ShowWindowMode mode, WindowStartupLocation location)
        {
            var sizeTextBox = _session.FindElementByAccessibilityId("ShowWindowSize");
            var modeComboBox = _session.FindElementByAccessibilityId("ShowWindowMode");
            var locationComboBox = _session.FindElementByAccessibilityId("ShowWindowLocation");
            var showButton = _session.FindElementByAccessibilityId("ShowWindow");

            if (size.HasValue)
                sizeTextBox.SendKeys($"{size.Value.Width}, {size.Value.Height}");

            modeComboBox.Click();
            _session.FindElementByName(mode.ToString()).SendClick();

            locationComboBox.Click();
            _session.FindElementByName(location.ToString()).SendClick();

            return showButton.OpenWindowWithClick();
        }

        private static int GetWindowOrder(IReadOnlyCollection<AppiumWebElement> elements, string identifier)
        {
            return elements.TakeWhile(x =>
                x.FindElementByXPath("XCUIElementTypeWindow")?.GetAttribute("identifier") != identifier).Count();
        }

        private static AppiumWebElement FindWindow(AppiumDriver<AppiumWebElement> session, string identifier)
        {
            var windows = session.FindElementsByXPath("XCUIElementTypeWindow");
            return windows.First(x =>
                x.FindElementsByXPath("XCUIElementTypeWindow")
                    .Any(y => y.GetAttribute("identifier") == identifier));
        }

        public enum ShowWindowMode
        {
            NonOwned,
            Owned,
            Modal
        }
    }
}
