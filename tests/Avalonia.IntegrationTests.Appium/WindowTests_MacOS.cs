using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Avalonia.Controls;
using Avalonia.IntegrationTests.Appium.Wrappers;
using Avalonia.Utilities;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Interactions;
using Xunit;

namespace Avalonia.IntegrationTests.Appium
{
    [Collection("Default")]
    public class WindowTests_MacOS
    {
        private readonly IWindowElement _mainWindow;
        private readonly ISession _session;
        private readonly AppiumDriver<AppiumWebElement> _driver;

        public WindowTests_MacOS(DefaultAppFixture fixture)
        {
            _session = fixture.Session;
            _driver = fixture.Driver;
            
            var retry = 0;

            for (;;)
            {
                try
                {
                    _mainWindow = fixture.Session.GetWindow("MainWindow");
                    var tab = _mainWindow.FindElementByName("Window");
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
            using (var window = OpenWindow(new PixelSize(200, 100), ShowWindowMode.Modal, WindowStartupLocation.Manual))
            {
                _mainWindow.Click();

                var secondaryWindowIndex = GetWindowOrder(window);

                Assert.Equal(1, secondaryWindowIndex);
            }
        }

        [PlatformFact(TestPlatforms.MacOS)]
        public void WindowOrder_Modal_Dialog_Stays_InFront_Of_Parent_When_Clicking_Resize_Grip()
        {
            var mainWindow = GetWindow("MainWindow");
            
            using (var window = OpenWindow(new PixelSize(200, 100), ShowWindowMode.Modal, WindowStartupLocation.Manual))
            {
                new Actions(_driver)
                    .MoveToElement(mainWindow, 100, 1)
                    .ClickAndHold()
                    .Perform();

                var secondaryWindowIndex = GetWindowOrder(window);

                new Actions(_driver)
                    .MoveToElement(mainWindow, 100, 1)
                    .Release()
                    .Perform();

                Assert.Equal(1, secondaryWindowIndex);
            }
        }

        [PlatformFact(TestPlatforms.MacOS)]
        public void WindowOrder_Modal_Dialog_Stays_InFront_Of_Parent_When_In_Fullscreen()
        {
            var buttons = (_mainWindow as WindowElement).GetChromeButtons();
            
            buttons.maximize.Click();

            Thread.Sleep(500);

            try
            {
                using (var window = OpenWindow(new PixelSize(200, 100), ShowWindowMode.Modal, WindowStartupLocation.Manual))
                {
                    var secondaryWindowIndex = GetWindowOrder(window);
                    Assert.Equal(1, secondaryWindowIndex);
                }
            }
            finally
            {
                _mainWindow.FindElementByAccessibilityId("ExitFullscreen").Click();
            }
        }

        [PlatformFact(TestPlatforms.MacOS)]
        public void WindowOrder_Owned_Dialog_Stays_InFront_Of_Parent()
        {
            using (var window = OpenWindow(new PixelSize(200, 100), ShowWindowMode.Owned, WindowStartupLocation.Manual))
            {
                _mainWindow.SendClick();
                var secondaryWindowIndex = GetWindowOrder(window);
                Assert.Equal(1, secondaryWindowIndex);
            }
        }
        
        [PlatformFact(TestPlatforms.MacOS)]
        public void WindowOrder_Owned_Dialog_Stays_InFront_Of_FullScreen_Parent()
        {
            // Enter fullscreen
            _mainWindow.FindElementByAccessibilityId("EnterFullscreen").Click();
            
            // Wait for fullscreen transition.
            Thread.Sleep(1000);

            // Make sure we entered fullscreen.
            var windowState = _mainWindow.FindElementByAccessibilityId("MainWindowState");
            Assert.Equal("FullScreen", windowState.Text);

            // Open child window.
            using (var window = OpenWindow(new PixelSize(200, 100), ShowWindowMode.Owned, WindowStartupLocation.Manual))
            {
                _mainWindow.SendClick();
                var secondaryWindowIndex = GetWindowOrder(window);
                Assert.Equal(1, secondaryWindowIndex);
            }

            // Exit fullscreen by menu shortcut Command+R
            _mainWindow.FindElementByAccessibilityId("ExitFullscreen").Click();

            // Wait for restore transition.
            Thread.Sleep(1000);

            // Make sure we exited fullscreen.
            windowState = _mainWindow.FindElementByAccessibilityId("MainWindowState");
            Assert.Equal("Normal", windowState.Text);
        }
        
        [PlatformFact(TestPlatforms.MacOS)]
        public void WindowOrder_Owned_Dialog_Stays_InFront_Of_Parent_After_Modal_Closed()
        {
            using (var window = OpenWindow(new PixelSize(200, 300), ShowWindowMode.Owned, WindowStartupLocation.Manual))
            {
                OpenWindow(null, ShowWindowMode.Modal, WindowStartupLocation.Manual).Dispose();
                
                var secondaryWindowIndex = GetWindowOrder(window);
                Assert.Equal(1, secondaryWindowIndex);
            }
        }

        [PlatformFact(TestPlatforms.MacOS)]
        public void Does_Not_Switch_Space_From_FullScreen_To_Main_Desktop_When_FullScreen_Window_Clicked()
        {
            // Issue #9565
            IElement windowState;

            // Open child window.
            using (OpenWindow(new PixelSize(200, 100), ShowWindowMode.Owned, WindowStartupLocation.Manual))
            {
                // Enter fullscreen
                _mainWindow.FindElementByAccessibilityId("EnterFullscreen").Click();
            
                // Wait for fullscreen transition.
                Thread.Sleep(1000);

                // Make sure we entered fullscreen.
                //_mainWindow = _driver.FindElementByAccessibilityId("MainWindow");
                windowState = _mainWindow.FindElementByAccessibilityId("MainWindowState");
                Assert.Equal("FullScreen", windowState.Text);
                
                // Click on main window
                _mainWindow.Click();

                // Failed here due to #9565: main window is no longer visible as the main space is now shown instead
                // of the fullscreen space.
                _mainWindow.FindElementByAccessibilityId("ExitFullscreen").Click();

                // Wait for restore transition.
                Thread.Sleep(1000);
            }

            // Make sure we exited fullscreen.
            //mainWindow = _driver.FindElementByAccessibilityId("MainWindow");
            windowState = _mainWindow.FindElementByAccessibilityId("MainWindowState");
            Assert.Equal("Normal", windowState.Text);
        }

        [PlatformFact(TestPlatforms.MacOS)]
        public void WindowOrder_NonOwned_Window_Does_Not_Stay_InFront_Of_Parent()
        {
            using (var window = OpenWindow(new PixelSize(800, 100), ShowWindowMode.NonOwned, WindowStartupLocation.Manual))
            {
                _mainWindow.Click();

                var secondaryWindowIndex = GetWindowOrder(window);

                Assert.Equal(2, secondaryWindowIndex);

                var sendToBack = _driver.FindElementByAccessibilityId("SendToBack");
                sendToBack.Click();
            }
        }

        [PlatformFact(TestPlatforms.MacOS)]
        public void WindowOrder_Owned_Is_Correct_After_Closing_Window()
        {
            using (var window = OpenWindow(new PixelSize(300, 500), ShowWindowMode.Owned, WindowStartupLocation.CenterOwner))
            {
                // Open a second child window, and close it.
                using (OpenWindow(new PixelSize(200, 200), ShowWindowMode.Owned, WindowStartupLocation.CenterOwner))
                {
                }
        
                var secondaryWindowIndex = GetWindowOrder(window);
        
                Assert.Equal(1, secondaryWindowIndex);
            }
        }

        [PlatformFact(TestPlatforms.MacOS)]
        public void Parent_Window_Has_Disabled_ChromeButtons_When_Modal_Dialog_Shown()
        {
            var (closeButton, miniaturizeButton, zoomButton) = (_mainWindow as WindowElement).GetChromeButtons();

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
            using (var secondaryWindow = OpenWindow(new PixelSize(200, 100), ShowWindowMode.Modal, WindowStartupLocation.CenterOwner))
            {
                var (closeButton, miniaturizeButton, zoomButton) = (secondaryWindow as WindowElement).GetChromeButtons();

                Assert.True(closeButton.Enabled);
                Assert.True(zoomButton.Enabled);
                Assert.False(miniaturizeButton.Enabled);
            }
        }
        
        [PlatformTheory(TestPlatforms.MacOS)]
        [InlineData(ShowWindowMode.Owned)]
        public void Minimize_Button_Disabled_Owned_Window(ShowWindowMode mode)
        {
            using (var secondaryWindow = OpenWindow(new PixelSize(200, 100), mode, WindowStartupLocation.Manual))
            {
                var (_, miniaturizeButton, _) = (secondaryWindow as WindowElement).GetChromeButtons();

                Assert.False(miniaturizeButton.Enabled);
            }
        }
        

        [PlatformTheory(TestPlatforms.MacOS)]
        [InlineData(ShowWindowMode.NonOwned)]
        public void Minimize_Button_Minimizes_Window(ShowWindowMode mode)
        {
            using (var secondaryWindow = OpenWindow(new PixelSize(200, 100), mode, WindowStartupLocation.Manual))
            {
                var (_, miniaturizeButton, _) = (secondaryWindow as WindowElement).GetChromeButtons();

                miniaturizeButton.Click();
                Thread.Sleep(1000);

                var hittable = _driver.FindElementsByXPath("/XCUIElementTypeApplication/XCUIElementTypeWindow")
                    .Select(x => x.GetAttribute("hittable")).ToList();
                Assert.Equal(new[] { "true", "false" }, hittable);

                _driver.FindElementByAccessibilityId("RestoreAll").Click();
                Thread.Sleep(1000);

                hittable = _driver.FindElementsByXPath("/XCUIElementTypeApplication/XCUIElementTypeWindow")
                    .Select(x => x.GetAttribute("hittable")).ToList();
                Assert.Equal(new[] { "true", "true" }, hittable);
            }
        }

        [PlatformFact(TestPlatforms.MacOS)]
        public void Hidden_Child_Window_Is_Not_Reshown_When_Parent_Clicked()
        {
            // We don't use dispose to close the window here, because it seems that hiding and re-showing a window
            // causes Appium to think it's a different window.
            var secondaryWindow = OpenWindow(null, ShowWindowMode.Owned, WindowStartupLocation.Manual);
            
            var hideButton = secondaryWindow.FindElementByAccessibilityId("HideButton");

            hideButton.Click();

            var windows = _session.Windows.ToList();
            Assert.Single(windows);
                
            _mainWindow.Click();
                
            windows = windows = _session.Windows.ToList();
            Assert.Single(windows);
                
            _driver.FindElementByAccessibilityId("RestoreAll").Click();

            secondaryWindow = _session.GetWindow("SecondaryWindow");
            secondaryWindow.Close();
        }

        [PlatformTheory(TestPlatforms.MacOS)]
        [InlineData(ShowWindowMode.NonOwned)]
        [InlineData(ShowWindowMode.Owned)]
        [InlineData(ShowWindowMode.Modal)]
        public void Window_Has_Disabled_Zoom_Button_When_CanResize_Is_False(ShowWindowMode mode)
        {
            using (var window = OpenWindow(null, mode, WindowStartupLocation.Manual, canResize: false))
            {
                var (_, _, zoomButton) = (window as WindowElement).GetChromeButtons();
                Assert.False(zoomButton.Enabled);
            }
        }
        
        private IWindowElement OpenWindow(
            PixelSize? size,
            ShowWindowMode mode,
            WindowStartupLocation location,
            bool canResize = true)
        {
            var sizeTextBox = _mainWindow.FindElementByAccessibilityId("ShowWindowSize");
            var modeComboBox = _mainWindow.FindElementByAccessibilityId("ShowWindowMode");
            var locationComboBox = _mainWindow.FindElementByAccessibilityId("ShowWindowLocation");
            var canResizeCheckBox = _mainWindow.FindElementByAccessibilityId("ShowWindowCanResize");
            var showButton = _mainWindow.FindElementByAccessibilityId("ShowWindow");

            if (size.HasValue)
                sizeTextBox.SendKeys($"{size.Value.Width}, {size.Value.Height}");

            if (modeComboBox.GetComboBoxValue() != mode.ToString())
            {
                modeComboBox.Click();
                _mainWindow.FindElementByName(mode.ToString()).SendClick();
            }

            if (locationComboBox.GetComboBoxValue() != location.ToString())
            {
                locationComboBox.Click();
                _mainWindow.FindElementByName(location.ToString()).SendClick();
            }
            
            if (canResizeCheckBox.GetIsChecked() != canResize)
                canResizeCheckBox.Click();

            return _session.GetNewWindow(() => showButton.Click());
        }

        private AppiumWebElement GetWindow(string identifier)
        {
            // The Avalonia a11y tree currently exposes two nested Window elements, this is a bug and should be fixed 
            // but in the meantime use the `parent::' selector to return the parent "real" window. 
            return _driver.FindElementByXPath(
                $"XCUIElementTypeWindow//*[@identifier='{identifier}']/parent::XCUIElementTypeWindow");
        }

        private int GetWindowOrder(IWindowElement window)
        {
            var order = window.FindByXPath("//*[@identifier='CurrentOrder']");
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
