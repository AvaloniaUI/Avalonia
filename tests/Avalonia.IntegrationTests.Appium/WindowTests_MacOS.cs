using System;
using System.Linq;
using System.Threading;
using Avalonia.Controls;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Interactions;
using Xunit;

namespace Avalonia.IntegrationTests.Appium
{
    [Collection("Default")]
    public class WindowTests_MacOS : TestBase
    {
        public WindowTests_MacOS(DefaultAppFixture fixture)
            : base(fixture, "Window")
        {
        }

        [PlatformFact(TestPlatforms.MacOS)]
        public void WindowOrder_Modal_Dialog_Stays_InFront_Of_Parent()
        {
            var mainWindow = Session.FindElementByAccessibilityId("MainWindow");

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
                new Actions(Session)
                    .MoveToElement(mainWindow, 100, 1)
                    .ClickAndHold()
                    .Perform();

                var secondaryWindowIndex = GetWindowOrder("SecondaryWindow");

                new Actions(Session)
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
            var fullScreen = mainWindow.FindElementByAccessibilityId("_XCUI:FullScreenWindow");

            fullScreen.Click();

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
                Session.FindElementByAccessibilityId("ExitFullscreen").Click();
            }
        }

        [PlatformFact(TestPlatforms.MacOS)]
        public void WindowOrder_Owned_Dialog_Stays_InFront_Of_Parent()
        {
            var mainWindow = Session.FindElementByAccessibilityId("MainWindow");

            using (OpenWindow(new PixelSize(200, 100), ShowWindowMode.Owned, WindowStartupLocation.Manual))
            {
                mainWindow.SendClick();
                var secondaryWindowIndex = GetWindowOrder("SecondaryWindow");
                Assert.Equal(1, secondaryWindowIndex);
            }
        }
        
        [PlatformFact(TestPlatforms.MacOS)]
        public void WindowOrder_Owned_Dialog_Stays_InFront_Of_FullScreen_Parent()
        {
            var mainWindow = Session.FindElementByAccessibilityId("MainWindow");

            // Enter fullscreen
            mainWindow.FindElementByAccessibilityId("EnterFullscreen").Click();
            
            // Wait for fullscreen transition.
            Thread.Sleep(1000);

            // Make sure we entered fullscreen.
            var windowState = mainWindow.FindElementByAccessibilityId("MainWindowState");
            Assert.Equal("FullScreen", windowState.Text);

            // Open child window.
            using (OpenWindow(new PixelSize(200, 100), ShowWindowMode.Owned, WindowStartupLocation.Manual))
            {
                mainWindow.SendClick();
                var secondaryWindowIndex = GetWindowOrder("SecondaryWindow");
                Assert.Equal(1, secondaryWindowIndex);
            }

            // Exit fullscreen by menu shortcut Command+R
            mainWindow.FindElementByAccessibilityId("ExitFullscreen").Click();

            // Wait for restore transition.
            Thread.Sleep(1000);

            // Make sure we exited fullscreen.
            mainWindow = Session.FindElementByAccessibilityId("MainWindow");
            windowState = mainWindow.FindElementByAccessibilityId("MainWindowState");
            Assert.Equal("Normal", windowState.Text);
        }
        
        [PlatformFact(TestPlatforms.MacOS)]
        public void WindowOrder_Owned_Dialog_Stays_InFront_Of_Parent_After_Modal_Closed()
        {
            using (OpenWindow(new PixelSize(200, 300), ShowWindowMode.Owned, WindowStartupLocation.Manual))
            {
                OpenWindow(null, ShowWindowMode.Modal, WindowStartupLocation.Manual).Dispose();
                
                var secondaryWindowIndex = GetWindowOrder("SecondaryWindow");
                Assert.Equal(1, secondaryWindowIndex);
            }
        }

        [PlatformFact(TestPlatforms.MacOS, Skip = "Flaky test")]
        public void Does_Not_Switch_Space_From_FullScreen_To_Main_Desktop_When_FullScreen_Window_Clicked()
        {
            // Issue #9565
            var mainWindow = Session.FindElementByAccessibilityId("MainWindow");
            AppiumWebElement windowState;

            // Open child window.
            using (OpenWindow(new PixelSize(200, 100), ShowWindowMode.Owned, WindowStartupLocation.Manual))
            {
                // Enter fullscreen
                mainWindow.FindElementByAccessibilityId("EnterFullscreen").Click();
            
                // Wait for fullscreen transition.
                Thread.Sleep(1000);

                // Make sure we entered fullscreen.
                mainWindow = Session.FindElementByAccessibilityId("MainWindow");
                windowState = mainWindow.FindElementByAccessibilityId("MainWindowState");
                Assert.Equal("FullScreen", windowState.Text);
                
                // Click on main window
                mainWindow.Click();

                // Failed here due to #9565: main window is no longer visible as the main space is now shown instead
                // of the fullscreen space.
                mainWindow.FindElementByAccessibilityId("ExitFullscreen").Click();

                // Wait for restore transition.
                Thread.Sleep(1000);
            }

            // Make sure we exited fullscreen.
            mainWindow = Session.FindElementByAccessibilityId("MainWindow");
            windowState = mainWindow.FindElementByAccessibilityId("MainWindowState");
            Assert.Equal("Normal", windowState.Text);
        }

        [PlatformFact(TestPlatforms.MacOS)]
        public void WindowOrder_NonOwned_Window_Does_Not_Stay_InFront_Of_Parent()
        {
            var mainWindow = Session.FindElementByAccessibilityId("MainWindow");

            using (OpenWindow(new PixelSize(800, 100), ShowWindowMode.NonOwned, WindowStartupLocation.Manual))
            {
                mainWindow.Click();

                var secondaryWindowIndex = GetWindowOrder("SecondaryWindow");

                Assert.Equal(2, secondaryWindowIndex);

                var sendToBack = Session.FindElementByAccessibilityId("SendToBack");
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
            var windowChrome = window.GetChromeButtons();

            Assert.True(windowChrome.Close!.Enabled);
            Assert.True(windowChrome.FullScreen!.Enabled);
            Assert.True(windowChrome.Minimize!.Enabled);
            Assert.Null(windowChrome.Maximize);

            using (OpenWindow(new PixelSize(200, 100), ShowWindowMode.Modal, WindowStartupLocation.CenterOwner))
            {
                Assert.False(windowChrome.Close!.Enabled);
                Assert.False(windowChrome.FullScreen!.Enabled);
                Assert.False(windowChrome.Minimize!.Enabled);
            }
        }

        [PlatformFact(TestPlatforms.MacOS)]
        public void Minimize_Button_Is_Disabled_On_Modal_Dialog()
        {
            using (OpenWindow(new PixelSize(200, 100), ShowWindowMode.Modal, WindowStartupLocation.CenterOwner))
            {
                var secondaryWindow = GetWindow("SecondaryWindow");
                var windowChrome = secondaryWindow.GetChromeButtons();

                Assert.True(windowChrome.Close!.Enabled);
                Assert.True(windowChrome.Maximize!.Enabled);
                Assert.False(windowChrome.Minimize!.Enabled);
            }
        }
        
        [PlatformTheory(TestPlatforms.MacOS)]
        [InlineData(ShowWindowMode.Owned)]
        public void Minimize_Button_Disabled_Owned_Window(ShowWindowMode mode)
        {
            using (OpenWindow(new PixelSize(200, 100), mode, WindowStartupLocation.Manual))
            {
                var secondaryWindow = GetWindow("SecondaryWindow");
                var miniaturizeButton = secondaryWindow.FindElementByAccessibilityId("_XCUI:MinimizeWindow");

                Assert.False(miniaturizeButton.Enabled);
            }
        }
        

        [PlatformTheory(TestPlatforms.MacOS)]
        [InlineData(ShowWindowMode.NonOwned)]
        public void Minimize_Button_Minimizes_Window(ShowWindowMode mode)
        {
            using (OpenWindow(new PixelSize(200, 100), mode, WindowStartupLocation.Manual))
            {
                var secondaryWindow = GetWindow("SecondaryWindow");
                var miniaturizeButton = secondaryWindow.FindElementByAccessibilityId("_XCUI:MinimizeWindow");

                miniaturizeButton.Click();
                Thread.Sleep(1000);

                var hittable = Session.FindElementsByXPath("/XCUIElementTypeApplication/XCUIElementTypeWindow")
                    .Select(x => x.GetAttribute("hittable")).ToList();
                Assert.Equal(new[] { "true", "false" }, hittable);

                Session.FindElementByAccessibilityId("RestoreAll").Click();
                Thread.Sleep(1000);

                hittable = Session.FindElementsByXPath("/XCUIElementTypeApplication/XCUIElementTypeWindow")
                    .Select(x => x.GetAttribute("hittable")).ToList();
                Assert.Equal(new[] { "true", "true" }, hittable);
            }
        }

        [PlatformFact(TestPlatforms.MacOS)]
        public void Hidden_Child_Window_Is_Not_Reshown_When_Parent_Clicked()
        {
            var mainWindow = Session.FindElementByAccessibilityId("MainWindow");

            // We don't use dispose to close the window here, because it seems that hiding and re-showing a window
            // causes Appium to think it's a different window.
            OpenWindow(null, ShowWindowMode.Owned, WindowStartupLocation.Manual);
            
            var secondaryWindow = GetWindow("SecondaryWindow");
            var hideButton = secondaryWindow.FindElementByAccessibilityId("HideButton");

            hideButton.Click();
                
            var windows = Session.FindElementsByXPath("XCUIElementTypeWindow");
            Assert.Single(windows);
                
            mainWindow.Click();
                
            windows = Session.FindElementsByXPath("XCUIElementTypeWindow");
            Assert.Single(windows);
                
            Session.FindElementByAccessibilityId("RestoreAll").Click();

            // Close the window manually.
            secondaryWindow = GetWindow("SecondaryWindow");
            secondaryWindow.FindElementByAccessibilityId("_XCUI:CloseWindow").Click();
        }

        [PlatformFact(TestPlatforms.MacOS)]
        public void Toggling_SystemDecorations_Should_Preserve_ExtendClientArea()
        {
            // #10650
            using (OpenWindow(extendClientArea: true))
            {
                var secondaryWindow = GetWindow("SecondaryWindow");
                
                // The XPath of the title bar text _should_ be "XCUIElementTypeStaticText"
                // but Appium seems to put a fake node between the window and the title bar
                // https://stackoverflow.com/a/71914227/6448
                var titleBar = secondaryWindow.FindElementsByXPath("/*/XCUIElementTypeStaticText").Count;
                
                Assert.Equal(0, titleBar);

                secondaryWindow.FindElementByAccessibilityId("CurrentSystemDecorations").Click();
                Session.FindElementByAccessibilityId("SystemDecorationsNone").SendClick();
                secondaryWindow.FindElementByAccessibilityId("CurrentSystemDecorations").Click();
                Session.FindElementByAccessibilityId("SystemDecorationsFull").SendClick();

                titleBar = secondaryWindow.FindElementsByXPath("/*/XCUIElementTypeStaticText").Count;
                Assert.Equal(0, titleBar);
            }
        }

        [PlatformTheory(TestPlatforms.MacOS)]
        [InlineData(SystemDecorations.None)]
        [InlineData(SystemDecorations.BorderOnly)]
        [InlineData(SystemDecorations.Full)]
        public void ExtendClientArea_SystemDecorations_Shows_Correct_Buttons(SystemDecorations decorations)
        {
            // #10650
            using (OpenWindow(extendClientArea: true, systemDecorations: decorations))
            {
                var secondaryWindow = GetWindow("SecondaryWindow");

                try
                {
                    var chrome = secondaryWindow.GetChromeButtons();
                
                    if (decorations == SystemDecorations.Full)
                    {
                        Assert.NotNull(chrome.Close);
                        Assert.NotNull(chrome.Minimize);
                        Assert.NotNull(chrome.FullScreen);
                    }
                    else
                    {
                        Assert.Null(chrome.Close);
                        Assert.Null(chrome.Minimize);
                        Assert.Null(chrome.FullScreen);
                    }
                }
                finally
                {
                    if (decorations != SystemDecorations.Full)
                    {
                        secondaryWindow.FindElementByAccessibilityId("CurrentSystemDecorations").Click();
                        Session.FindElementByAccessibilityId("SystemDecorationsFull").SendClick();
                    }
                }
            }
        }

        private IDisposable OpenWindow(
            PixelSize? size = null,
            ShowWindowMode mode = ShowWindowMode.NonOwned,
            WindowStartupLocation location = WindowStartupLocation.Manual,
            bool canResize = true,
            SystemDecorations systemDecorations = SystemDecorations.Full,
            bool extendClientArea = false)
        {
            var sizeTextBox = Session.FindElementByAccessibilityId("ShowWindowSize");
            var modeComboBox = Session.FindElementByAccessibilityId("ShowWindowMode");
            var locationComboBox = Session.FindElementByAccessibilityId("ShowWindowLocation");
            var canResizeCheckBox = Session.FindElementByAccessibilityId("ShowWindowCanResize");
            var showButton = Session.FindElementByAccessibilityId("ShowWindow");
            var systemDecorationsComboBox = Session.FindElementByAccessibilityId("ShowWindowSystemDecorations");
            var extendClientAreaCheckBox = Session.FindElementByAccessibilityId("ShowWindowExtendClientAreaToDecorationsHint");

            if (size.HasValue)
                sizeTextBox.SendKeys($"{size.Value.Width}, {size.Value.Height}");

            if (modeComboBox.GetComboBoxValue() != mode.ToString())
            {
                modeComboBox.Click();
                Session.FindElementByName(mode.ToString()).SendClick();
            }

            if (locationComboBox.GetComboBoxValue() != location.ToString())
            {
                locationComboBox.Click();
                Session.FindElementByName(location.ToString()).SendClick();
            }
            
            if (canResizeCheckBox.GetIsChecked() != canResize)
                canResizeCheckBox.Click();

            if (systemDecorationsComboBox.GetComboBoxValue() != systemDecorations.ToString())
            {
                systemDecorationsComboBox.Click();
                Session.FindElementByName(systemDecorations.ToString()).SendClick();
            }
            
            if (extendClientAreaCheckBox.GetIsChecked() != extendClientArea)
                extendClientAreaCheckBox.Click();
            
            return showButton.OpenWindowWithClick();
        }

        private AppiumWebElement GetWindow(string identifier)
        {
            // The Avalonia a11y tree currently exposes two nested Window elements, this is a bug and should be fixed 
            // but in the meantime use the `parent::' selector to return the parent "real" window. 
            return Session.FindElementByXPath(
                $"XCUIElementTypeWindow//*[@identifier='{identifier}']/parent::XCUIElementTypeWindow");
        }

        private int GetWindowOrder(string identifier)
        {
            var window = GetWindow(identifier);
            var order = window.FindElementByXPath("//*[@identifier='CurrentOrder']");
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
