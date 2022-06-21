using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Interactions;
using Xunit;
using Xunit.Sdk;

namespace Avalonia.IntegrationTests.Appium
{
    [Collection("Default")]
    public class WindowTests
    {
        private readonly AppiumDriver<AppiumWebElement> _session;

        public WindowTests(TestAppFixture fixture)
        {
            _session = fixture.Session;

            var tabs = _session.FindElementByAccessibilityId("MainTabs");
            var tab = tabs.FindElementByName("Window");
            tab.Click();
        }

        [Theory]
        [MemberData(nameof(StartupLocationData))]
        public void StartupLocation(PixelSize? size, ShowWindowMode mode, WindowStartupLocation location)
        {
            using var window = OpenWindow(size, mode, location);
            var clientSize = Size.Parse(_session.FindElementByAccessibilityId("ClientSize").Text);
            var frameSize = Size.Parse(_session.FindElementByAccessibilityId("FrameSize").Text);
            var position = PixelPoint.Parse(_session.FindElementByAccessibilityId("Position").Text);
            var screenRect = PixelRect.Parse(_session.FindElementByAccessibilityId("ScreenRect").Text);
            var scaling = double.Parse(_session.FindElementByAccessibilityId("Scaling").Text);

            Assert.True(frameSize.Width >= clientSize.Width, "Expected frame width >= client width.");
            Assert.True(frameSize.Height > clientSize.Height, "Expected frame height > client height.");

            var frameRect = new PixelRect(position, PixelSize.FromSize(frameSize, scaling));

            switch (location)
            {
                case WindowStartupLocation.CenterScreen:
                    {
                        var expected = screenRect.CenterRect(frameRect);
                        AssertCloseEnough(expected.Position, frameRect.Position);
                        break;
                    }
            }
        }

        [PlatformFact(SkipOnWindows = true)]
        public void OSX_WindowOrder_Modal_Dialog_Stays_InFront_Of_Parent()
        {
            var mainWindowHandle = GetCurrentWindowHandleHack();
            
            var mainWindow =
                _session.FindElementByAccessibilityId("MainWindow");

            try
            {
                using (OpenWindow(new PixelSize(200, 100), ShowWindowMode.Modal, WindowStartupLocation.CenterOwner))
                {
                    mainWindow.Click();

                    var windows = _session.FindElements(By.XPath("XCUIElementTypeWindow"));

                    int mainWindowIndex = windows.GetWindowOrder("MainWindow");
                    int secondaryWindowIndex = windows.GetWindowOrder("SecondaryWindow");

                    Assert.Equal(0, secondaryWindowIndex);
                    Assert.Equal(1, mainWindowIndex);
                }
            }
            finally
            {
                SwitchToMainWindowHack(mainWindowHandle);
                _session.ResetApp();
            }
        }
        
        [PlatformFact(SkipOnWindows = true)]
        public void OSX_WindowOrder_Modal_Dialog_Stays_InFront_Of_Parent_When_Clicking_Resize_Grip()
        {
            var mainWindowHandle = GetCurrentWindowHandleHack();
            
            var mainWindow =
                _session.FindWindowOuter("MainWindow");

            try
            {
                using (OpenWindow(new PixelSize(200, 100), ShowWindowMode.Modal, WindowStartupLocation.CenterOwner))
                {
                    new Actions(_session)
                        .MoveToElement(mainWindow, 100, 1)
                        .ClickAndHold()
                        .Perform();

                    var windows = _session.FindElements(By.XPath("XCUIElementTypeWindow"));

                    int mainWindowIndex = windows.GetWindowOrder("MainWindow");
                    int secondaryWindowIndex = windows.GetWindowOrder("SecondaryWindow");
                    
                    new Actions(_session)
                        .MoveToElement(mainWindow, 100, 1)
                        .Release()
                        .Perform();

                    Assert.Equal(0, secondaryWindowIndex);
                    Assert.Equal(1, mainWindowIndex);
                }
            }
            finally
            {
                SwitchToMainWindowHack(mainWindowHandle);
                _session.ResetApp();
            }
        }
        
        [PlatformFact(SkipOnWindows = true)]
        public void OSX_WindowOrder_Modal_Dialog_Stays_InFront_Of_Parent_When_In_Fullscreen()
        {
            var mainWindowHandle = GetCurrentWindowHandleHack();
            
            var mainWindow =
                _session.FindWindowOuter("MainWindow");

            var buttons = mainWindow.GetChromeButtons();
            
            buttons.zoomButton.Click();

            Task.Delay(500).Wait();

            try
            {
                using (OpenWindow(new PixelSize(200, 100), ShowWindowMode.Modal, WindowStartupLocation.CenterOwner))
                {
                    var windows = _session.FindElements(By.XPath("XCUIElementTypeWindow"));

                    int mainWindowIndex = windows.GetWindowOrder("MainWindow");
                    int secondaryWindowIndex = windows.GetWindowOrder("SecondaryWindow");

                    Assert.Equal(0, secondaryWindowIndex);
                    Assert.Equal(1, mainWindowIndex);
                }
            }
            finally
            {
                SwitchToMainWindowHack(mainWindowHandle);
                _session.ResetApp();
            }
        }
        
        [PlatformFact(SkipOnWindows = true)]
        public void OSX_WindowOrder_Owned_Dialog_Stays_InFront_Of_Parent()
        {
            var mainWindowHandle = GetCurrentWindowHandleHack();
            
            var mainWindow =
                _session.FindElementByAccessibilityId("MainWindow");

            try
            {
                using (OpenWindow(new PixelSize(200, 100), ShowWindowMode.Owned, WindowStartupLocation.CenterOwner))
                {
                    mainWindow.Click();

                    var windows = _session.FindElements(By.XPath("XCUIElementTypeWindow"));

                    int mainWindowIndex = windows.GetWindowOrder("MainWindow");
                    int secondaryWindowIndex = windows.GetWindowOrder("SecondaryWindow");

                    Assert.Equal(0, secondaryWindowIndex);
                    Assert.Equal(1, mainWindowIndex);
                }
            }
            finally
            {
                SwitchToMainWindowHack(mainWindowHandle);
                _session.ResetApp();
            }
        }

        [PlatformFact(SkipOnWindows = true)]
        public void OSX_WindowOrder_NonOwned_Window_Does_Not_Stay_InFront_Of_Parent()
        {
            var mainWindow =
                _session.FindElementByAccessibilityId("MainWindow");

            try
            {
                using (OpenWindow(new PixelSize(1400, 100), ShowWindowMode.NonOwned, WindowStartupLocation.CenterOwner))
                {
                    mainWindow.Click();

                    var secondaryWindow =
                        _session.FindElementByAccessibilityId("SecondaryWindow");

                    var windows = _session.FindElements(By.XPath("XCUIElementTypeWindow"));

                    int mainWindowIndex = windows.GetWindowOrder("MainWindow");
                    int secondaryWindowIndex = windows.GetWindowOrder("SecondaryWindow");

                    Assert.Equal(1, secondaryWindowIndex);
                    Assert.Equal(0, mainWindowIndex);

                    secondaryWindow.SendClick();
                }
            }
            finally
            {
                _session.ResetApp();
            }
        }

        [PlatformFact(SkipOnWindows = true)]
        public void OSX_Parent_Window_Has_Disabled_ChromeButtons_When_Modal_Dialog_Shown()
        {
            var mainWindowHandle = GetCurrentWindowHandleHack();
            
            try
            {
                var window = _session.FindWindowOuter("MainWindow");

                var (closeButton, zoomButton, miniturizeButton) 
                    = window.GetChromeButtons();
                
                Assert.True(closeButton.Enabled);
                Assert.True(zoomButton.Enabled);
                Assert.True(miniturizeButton.Enabled);

                using (OpenWindow(new PixelSize(200, 100), ShowWindowMode.Modal, WindowStartupLocation.CenterOwner))
                {
                    SwitchToNewWindowHack(oldWindowHandle: mainWindowHandle);

                    Assert.False(closeButton.Enabled);
                    Assert.False(zoomButton.Enabled);
                    Assert.False(miniturizeButton.Enabled);
                }
            }
            finally
            {
                SwitchToMainWindowHack(mainWindowHandle);
                _session.ResetApp();
            }
        }
        
        [PlatformFact(SkipOnWindows = true)]
        public void OSX_Minimize_Button_Disabled_Modal_Dialog()
        {
            var mainWindowHandle = GetCurrentWindowHandleHack();
            
            try
            {
                using (OpenWindow(new PixelSize(200, 100), ShowWindowMode.Modal, WindowStartupLocation.CenterOwner))
                {
                    var secondaryWindow = _session.FindWindowOuter("SecondaryWindow");

                    var (closeButton, zoomButton, miniturizeButton) 
                        = secondaryWindow.GetChromeButtons();
                    
                    Assert.True(closeButton.Enabled);
                    Assert.True(zoomButton.Enabled);
                    Assert.False(miniturizeButton.Enabled);
                    
                    SwitchToNewWindowHack(oldWindowHandle: mainWindowHandle);
                }
            }
            finally
            {
                SwitchToMainWindowHack(mainWindowHandle);
                _session.ResetApp();
            }
        }
        
        public static TheoryData<PixelSize?, ShowWindowMode, WindowStartupLocation> StartupLocationData()
        {
            var sizes = new PixelSize?[] { null, new PixelSize(400, 300) };
            var data = new TheoryData<PixelSize?, ShowWindowMode, WindowStartupLocation>();

            foreach (var size in sizes)
            {
                foreach (var mode in Enum.GetValues<ShowWindowMode>())
                {
                    foreach (var location in Enum.GetValues<WindowStartupLocation>())
                    {
                        if (!(location == WindowStartupLocation.CenterOwner && mode == ShowWindowMode.NonOwned))
                        {
                            data.Add(size, mode, location);
                        }
                    }
                }
            }

            return data;
        }

        private string? GetCurrentWindowHandleHack()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // HACK: WinAppDriver only seems to switch to a newly opened window if the window has an owner,
                // otherwise the session remains targeting the previous window. Return the handle for the
                // current window so we know which window to switch to when another is opened.
                return _session.WindowHandles.Single();
            }

            return null;
        }

        private void SwitchToNewWindowHack(string? oldWindowHandle)
        {
            if (oldWindowHandle is not null)
            {
                var newWindowHandle = _session.WindowHandles.FirstOrDefault(x => x != oldWindowHandle);

                // HACK: Looks like WinAppDriver only adds window handles for non-owned windows, but luckily
                // non-owned windows is where we're having the problem, so if we find a window handle that
                // isn't the main window handle then switch to it.
                if (newWindowHandle is not null)
                    _session.SwitchTo().Window(newWindowHandle);
            }
        }

        private void SwitchToMainWindowHack(string? mainWindowHandle)
        {
            if (mainWindowHandle is not null)
                _session.SwitchTo().Window(mainWindowHandle);
        }

        private static void AssertCloseEnough(PixelPoint expected, PixelPoint actual)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // On win32, accurate frame information cannot be obtained until a window is shown but
                // WindowStartupLocation needs to be calculated before the window is shown, meaning that
                // the position of a centered window can be off by a bit. From initial testing, looks
                // like this shouldn't be more than 10 pixels.
                if (Math.Abs(expected.X - actual.X) > 10)
                    throw new EqualException(expected, actual);
                if (Math.Abs(expected.Y - actual.Y) > 10)
                    throw new EqualException(expected, actual);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                if (Math.Abs(expected.X - actual.X) > 15)
                    throw new EqualException(expected, actual);
                if (Math.Abs(expected.Y - actual.Y) > 15)
                    throw new EqualException(expected, actual);
            }
            else
            {
                Assert.Equal(expected, actual);
            }
        }

        private IDisposable OpenWindow(PixelSize? size, ShowWindowMode mode, WindowStartupLocation location)
        {
            var mainWindow = GetCurrentWindowHandleHack();
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

        public enum ShowWindowMode
        {
            NonOwned,
            Owned,
            Modal
        }
    }

    static class Extensions
    {
        public static int GetWindowOrder(this IReadOnlyCollection<AppiumWebElement> elements, string identifier)
        {
            return elements.TakeWhile(x =>
                x.FindElementByXPath("XCUIElementTypeWindow")?.GetAttribute("identifier") != identifier).Count();
        }

        public static AppiumWebElement? FindWindowInner(this AppiumDriver<AppiumWebElement> session, string identifier)
        {
            return session.FindElementsByXPath("XCUIElementTypeWindow")
                .FirstOrDefault(x => x.GetAttribute("identifier") == identifier);
        }
        
        public static AppiumWebElement? FindWindowOuter(this AppiumDriver<AppiumWebElement> session, string identifier)
        {
            var windows = session.FindElementsByXPath("XCUIElementTypeWindow");
                
            var window = windows.FirstOrDefault(x=>x.FindElementsByXPath("XCUIElementTypeWindow").Any(x => x.GetAttribute("identifier") == identifier));

            return window;
        }

        public static (AppiumWebElement? closeButton, AppiumWebElement? zoomButton, AppiumWebElement? miniturizeButton) GetChromeButtons (this AppiumWebElement outerWindow)
        {
            var closeButton =
                outerWindow.FindElementByXPath("XCUIElementTypeButton[1]");
                
            var zoomButton =
                outerWindow.FindElementByXPath("XCUIElementTypeButton[2]");
                
            var miniturizeButton =
                outerWindow.FindElementByXPath("XCUIElementTypeButton[3]");

            return (closeButton, zoomButton, miniturizeButton);
        }
    }
}
