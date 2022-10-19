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
                catch (WebDriverException) when (retry++ < 3)
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

                var secondaryWindowIndex = GetWindowOrder("SecondaryWindow");

                Assert.Equal(1, secondaryWindowIndex);
            }
        }

        [PlatformFact(TestPlatforms.MacOS)]
        public void WindowOrder_Modal_Dialog_Stays_InFront_Of_Parent_When_Clicking_Resize_Grip()
        {
            var mainWindow = GetWindow("MainWindow");

            using (OpenWindow(new PixelSize(200, 100), ShowWindowMode.Modal, WindowStartupLocation.Manual))
            {
                new Actions(_session)
                    .MoveToElement(mainWindow, 100, 1)
                    .ClickAndHold()
                    .Perform();

                var secondaryWindowIndex = GetWindowOrder("SecondaryWindow");

                new Actions(_session)
                    .MoveToElement(mainWindow, 100, 1)
                    .Release()
                    .Perform();

                Assert.Equal(1, secondaryWindowIndex);
            }
        }

        [PlatformFact(TestPlatforms.MacOS)]
        public void WindowOrder_Modal_Dialog_Stays_InFront_Of_Parent_When_In_Fullscreen()
        {
            var mainWindow = GetWindow("MainWindow");
            var buttons = mainWindow.GetChromeButtons();

            buttons.maximize.Click();

            Thread.Sleep(500);

            try
            {
                using (OpenWindow(new PixelSize(200, 100), ShowWindowMode.Modal, WindowStartupLocation.Manual))
                {
                    var secondaryWindowIndex = GetWindowOrder("SecondaryWindow");
                    Assert.Equal(1, secondaryWindowIndex);
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
                mainWindow.SendClick();
                var secondaryWindowIndex = GetWindowOrder("SecondaryWindow");
                Assert.Equal(1, secondaryWindowIndex);
            }
        }

        [PlatformFact(TestPlatforms.MacOS)]
        public void WindowOrder_NonOwned_Window_Does_Not_Stay_InFront_Of_Parent()
        {
            var mainWindow = _session.FindElementByAccessibilityId("MainWindow");

            using (OpenWindow(new PixelSize(800, 100), ShowWindowMode.NonOwned, WindowStartupLocation.Manual))
            {
                mainWindow.Click();

                var secondaryWindowIndex = GetWindowOrder("SecondaryWindow");

                Assert.Equal(2, secondaryWindowIndex);

                var sendToBack = _session.FindElementByAccessibilityId("SendToBack");
                sendToBack.Click();
            }
        }

        [PlatformFact(TestPlatforms.MacOS)]
        public void WindowOrder_Owned_Is_Correct_After_Closing_Window()
        {
            using (OpenWindow(new PixelSize(300, 500), ShowWindowMode.Owned, WindowStartupLocation.CenterOwner))
            {
                // Open a second child window, and close it.
                using (OpenWindow(new PixelSize(200, 200), ShowWindowMode.Owned, WindowStartupLocation.CenterOwner))
                {
                }
        
                var secondaryWindowIndex = GetWindowOrder("SecondaryWindow");
        
                Assert.Equal(1, secondaryWindowIndex);
            }
        }

        [PlatformFact(TestPlatforms.MacOS)]
        public void Parent_Window_Has_Disabled_ChromeButtons_When_Modal_Dialog_Shown()
        {
            var window = GetWindow("MainWindow");
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
                var secondaryWindow = GetWindow("SecondaryWindow");
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
                var secondaryWindow = GetWindow("SecondaryWindow");
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

        [PlatformFact(TestPlatforms.MacOS)]
        public void Hidden_Child_Window_Is_Not_Reshown_When_Parent_Clicked()
        {
            var mainWindow = _session.FindElementByAccessibilityId("MainWindow");

            // We don't use dispose to close the window here, because it seems that hiding and re-showing a window
            // causes Appium to think it's a different window.
            OpenWindow(null, ShowWindowMode.Owned, WindowStartupLocation.Manual);
            
            var secondaryWindow = GetWindow("SecondaryWindow");
            var hideButton = secondaryWindow.FindElementByAccessibilityId("HideButton");

            hideButton.Click();
                
            var windows = _session.FindElementsByXPath("XCUIElementTypeWindow");
            Assert.Single(windows);
                
            mainWindow.Click();
                
            windows = _session.FindElementsByXPath("XCUIElementTypeWindow");
            Assert.Single(windows);
                
            _session.FindElementByAccessibilityId("RestoreAll").Click();

            // Close the window manually.
            secondaryWindow = GetWindow("SecondaryWindow");
            secondaryWindow.GetChromeButtons().close.Click();
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

        private AppiumWebElement GetWindow(string identifier)
        {
            // The Avalonia a11y tree currently exposes two nested Window elements, this is a bug and should be fixed 
            // but in the meantime use the `parent::' selector to return the parent "real" window. 
            return _session.FindElementByXPath(
                $"XCUIElementTypeWindow//*[@identifier='{identifier}']/parent::XCUIElementTypeWindow");
        }

        private int GetWindowOrder(string identifier)
        {
            var window = GetWindow(identifier);
            var order = window.FindElementByXPath("//*[@identifier='Order']");
            return int.Parse(order.Text);
        }

        public enum ShowWindowMode
        {
            NonOwned,
            Owned,
            Modal
        }
    }
}
