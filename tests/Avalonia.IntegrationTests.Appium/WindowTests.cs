using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia.Controls;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Interactions;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;
using Xunit.Sdk;

namespace Avalonia.IntegrationTests.Appium
{
    [Collection("Default")]
    public class WindowTests
    {
        private readonly AppiumDriver<AppiumWebElement> _session;

        public WindowTests(DefaultAppFixture fixture)
        {
            _session = fixture.Session;

            var tabs = _session.FindElementByAccessibilityId("MainTabs");
            var tab = tabs.FindElementByName("Window");
            tab.Click();
        }

        [Theory]
        [MemberData(nameof(StartupLocationData))]
        public void StartupLocation(Size? size, ShowWindowMode mode, WindowStartupLocation location, bool canResize)
        {
            using var window = OpenWindow(size, mode, location, canResize: canResize);
            var info = GetWindowInfo();

            if (size.HasValue)
                Assert.Equal(size.Value, info.ClientSize);

            Assert.True(info.FrameSize.Width >= info.ClientSize.Width, "Expected frame width >= client width.");
            Assert.True(info.FrameSize.Height > info.ClientSize.Height, "Expected frame height > client height.");

            var frameRect = new PixelRect(info.Position, PixelSize.FromSize(info.FrameSize, info.Scaling));

            switch (location)
            {
                case WindowStartupLocation.CenterScreen:
                {
                    var expected = info.ScreenRect.CenterRect(frameRect);
                    AssertCloseEnough(expected.Position, frameRect.Position);
                    break;
                }
                case WindowStartupLocation.CenterOwner:
                {
                    Assert.NotNull(info.OwnerRect);
                    var expected = info.OwnerRect!.Value.CenterRect(frameRect);
                    AssertCloseEnough(expected.Position, frameRect.Position);
                    break;
                }
            }
        }

        [Theory]
        [MemberData(nameof(WindowStateData))]
        public void WindowState(Size? size, ShowWindowMode mode, WindowState state, bool canResize)
        {
            using var window = OpenWindow(size, mode, state: state, canResize: canResize);

            try
            {
                var info = GetWindowInfo();

                Assert.Equal(state, info.WindowState);

                switch (state)
                {
                    case Controls.WindowState.Normal:
                        Assert.True(info.FrameSize.Width * info.Scaling < info.ScreenRect.Size.Width);
                        Assert.True(info.FrameSize.Height * info.Scaling < info.ScreenRect.Size.Height);
                        break;
                    case Controls.WindowState.Maximized:
                    case Controls.WindowState.FullScreen:
                        Assert.True(info.FrameSize.Width * info.Scaling >= info.ScreenRect.Size.Width);
                        Assert.True(info.FrameSize.Height * info.Scaling >= info.ScreenRect.Size.Height);
                        break;
                }
            }
            finally
            {
                if (state == Controls.WindowState.FullScreen)
                {
                    try
                    {
                        _session.FindElementByAccessibilityId("CurrentWindowState").SendClick();
                        _session.FindElementByAccessibilityId("WindowStateNormal").SendClick();

                        // Wait for animations to run.
                        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                            Thread.Sleep(1000);
                    }
                    catch
                    {
                        /* Ignore errors in cleanup */
                    }
                }
            }
        }

        [PlatformFact(TestPlatforms.Windows)]
        public void OnWindows_Docked_Windows_Retain_Size_Position_When_Restored()
        {
            using (OpenWindow(new Size(400, 400), ShowWindowMode.NonOwned, WindowStartupLocation.Manual))
            {
                var windowState = _session.FindElementByAccessibilityId("CurrentWindowState");

                Assert.Equal("Normal", windowState.GetComboBoxValue());
                
                
                var window = _session.FindElements(By.XPath("//Window")).First();
                
                new Actions(_session)
                    .KeyDown(Keys.Meta)
                    .SendKeys(Keys.Left)
                    .KeyUp(Keys.Meta)
                    .Perform();
                
                var original = GetWindowInfo();
                
                windowState.Click();
                _session.FindElementByName("Minimized").SendClick();
                
                new Actions(_session)
                    .KeyDown(Keys.Alt)
                    .SendKeys(Keys.Tab)
                    .KeyUp(Keys.Alt)
                    .Perform();
                
                var current = GetWindowInfo();
                
                Assert.Equal(original.Position, current.Position);
                Assert.Equal(original.FrameSize, current.FrameSize);

            }
        }
        
        [Fact]
        public void Showing_Window_With_Size_Larger_Than_Screen_Measures_Content_With_Working_Area()
        {
            using (OpenWindow(new Size(4000, 2200), ShowWindowMode.NonOwned, WindowStartupLocation.Manual))
            {
                var screenRectTextBox = _session.FindElementByAccessibilityId("CurrentClientSize");
                var measuredWithTextBlock = _session.FindElementByAccessibilityId("CurrentMeasuredWithText");
                
                var measuredWithString = measuredWithTextBlock.Text;
                var workingAreaString = screenRectTextBox.Text;

                var workingArea = Size.Parse(workingAreaString);
                var measuredWith = Size.Parse(measuredWithString);

                Assert.Equal(workingArea, measuredWith);
            }
        }

        [Theory]
        [InlineData(ShowWindowMode.NonOwned)]
        [InlineData(ShowWindowMode.Owned)]
        [InlineData(ShowWindowMode.Modal)]
        public void ShowMode(ShowWindowMode mode)
        {
            using var window = OpenWindow(null, mode, WindowStartupLocation.Manual);
            var windowState = _session.FindElementByAccessibilityId("CurrentWindowState");
            var original = GetWindowInfo();

            Assert.Equal("Normal", windowState.GetComboBoxValue());

            windowState.Click();
            _session.FindElementByAccessibilityId("WindowStateMaximized").SendClick();
            Assert.Equal("Maximized", windowState.GetComboBoxValue());

            windowState.Click();
            _session.FindElementByAccessibilityId("WindowStateNormal").SendClick();

            var current = GetWindowInfo();
            Assert.Equal(original.Position, current.Position);
            Assert.Equal(original.FrameSize, current.FrameSize);

            // On macOS, only non-owned windows can go fullscreen.
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || mode == ShowWindowMode.NonOwned)
            {
                windowState.Click();
                _session.FindElementByAccessibilityId("WindowStateFullScreen").SendClick();
                Assert.Equal("FullScreen", windowState.GetComboBoxValue());

                current = GetWindowInfo();
                var clientSize = PixelSize.FromSize(current.ClientSize, current.Scaling);
                Assert.True(clientSize.Width >= current.ScreenRect.Width);
                Assert.True(clientSize.Height >= current.ScreenRect.Height);

                windowState.SendClick();
                
                _session.FindElementByAccessibilityId("WindowStateNormal").SendClick();

                current = GetWindowInfo();
                Assert.Equal(original.Position, current.Position);
                Assert.Equal(original.FrameSize, current.FrameSize);
            }
        }

        [Fact]
        public void Extended_Client_Window_Shows_With_Requested_Size()
        {
            var clientSize = new Size(400, 400);
            using var window = OpenWindow(clientSize, ShowWindowMode.NonOwned, WindowStartupLocation.CenterScreen, extendClientArea: true);
            var windowState = _session.FindElementByAccessibilityId("CurrentWindowState");
            var current = GetWindowInfo();

            Assert.Equal(current.ClientSize, clientSize);
        }

        [Fact]
        public void TransparentWindow()
        {
            var showTransparentWindow = _session.FindElementByAccessibilityId("ShowTransparentWindow");
            showTransparentWindow.Click();
            Thread.Sleep(1000);

            var window = _session.FindElementByAccessibilityId("TransparentWindow");
            var screenshot = window.GetScreenshot();

            window.Click();

            var img = SixLabors.ImageSharp.Image.Load<Rgba32>(screenshot.AsByteArray);
            var topLeftColor = img[10, 10];
            var centerColor = img[img.Width / 2, img.Height / 2];

            Assert.Equal(new Rgba32(0, 128, 0), topLeftColor);
            Assert.Equal(new Rgba32(255, 0, 0), centerColor);
        }

        [Fact]
        public void TransparentPopup()
        {
            var showTransparentWindow = _session.FindElementByAccessibilityId("ShowTransparentPopup");
            showTransparentWindow.Click();
            Thread.Sleep(1000);

            var window = _session.FindElementByAccessibilityId("TransparentPopupBackground");
            var container = window.FindElementByAccessibilityId("PopupContainer");
            var screenshot = container.GetScreenshot();

            window.Click();

            var img = SixLabors.ImageSharp.Image.Load<Rgba32>(screenshot.AsByteArray);
            var topLeftColor = img[10, 10];
            var centerColor = img[img.Width / 2, img.Height / 2];

            Assert.Equal(new Rgba32(0, 128, 0), topLeftColor);
            Assert.Equal(new Rgba32(255, 0, 0), centerColor);
        }

        [Theory]
        [InlineData(ShowWindowMode.NonOwned, true)]
        [InlineData(ShowWindowMode.Owned, true)]
        [InlineData(ShowWindowMode.Modal, true)]
        [InlineData(ShowWindowMode.NonOwned, false)]
        [InlineData(ShowWindowMode.Owned, false)]
        [InlineData(ShowWindowMode.Modal, false)]
        public void Window_Has_Disabled_Maximize_Button_When_CanResize_Is_False(ShowWindowMode mode, bool extendClientArea)
        {
            using (OpenWindow(null, mode, WindowStartupLocation.Manual, canResize: false, extendClientArea: extendClientArea))
            {
                var secondaryWindow = GetWindow("SecondaryWindow");
                AppiumWebElement? maximizeButton;

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    maximizeButton = extendClientArea ?
                        secondaryWindow.FindElementByXPath("//Button[@Name='Maximize']") :
                        secondaryWindow.FindElementByXPath("//TitleBar/Button[2]");
                }
                else
                {
                    maximizeButton = mode == ShowWindowMode.NonOwned ?
                        secondaryWindow.FindElementByAccessibilityId("_XCUI:FullScreenWindow") :
                        secondaryWindow.FindElementByAccessibilityId("_XCUI:ZoomWindow");
                }

                Assert.False(maximizeButton.Enabled);
            }
        }

        public static TheoryData<Size?, ShowWindowMode, WindowStartupLocation, bool> StartupLocationData()
        {
            var sizes = new Size?[] { null, new Size(400, 300) };
            var data = new TheoryData<Size?, ShowWindowMode, WindowStartupLocation, bool>();

            foreach (var size in sizes)
            {
                foreach (var mode in Enum.GetValues<ShowWindowMode>())
                {
                    foreach (var location in Enum.GetValues<WindowStartupLocation>())
                    {
                        if (!(location == WindowStartupLocation.CenterOwner && mode == ShowWindowMode.NonOwned))
                        {
                            data.Add(size, mode, location, true);
                            data.Add(size, mode, location, false);
                        }
                    }
                }
            }

            return data;
        }

        public static TheoryData<Size?, ShowWindowMode, WindowState, bool> WindowStateData()
        {
            var sizes = new Size?[] { null, new Size(400, 300) };
            var data = new TheoryData<Size?, ShowWindowMode, WindowState, bool>();

            foreach (var size in sizes)
            {
                foreach (var mode in Enum.GetValues<ShowWindowMode>())
                {
                    foreach (var state in Enum.GetValues<WindowState>())
                    {
                        // Not sure how to handle testing minimized windows currently.
                        if (state == Controls.WindowState.Minimized)
                            continue;

                        // Child/Modal windows cannot be fullscreen on macOS.
                        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) &&
                            state == Controls.WindowState.FullScreen &&
                            mode != ShowWindowMode.NonOwned)
                            continue;

                        data.Add(size, mode, state, true);
                        data.Add(size, mode, state, false);
                    }
                }
            }

            return data;
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

        private IDisposable OpenWindow(
            Size? size,
            ShowWindowMode mode,
            WindowStartupLocation location = WindowStartupLocation.Manual,
            WindowState state = Controls.WindowState.Normal,
            bool canResize = true,
            bool extendClientArea = false)
        {
            var sizeTextBox = _session.FindElementByAccessibilityId("ShowWindowSize");
            var modeComboBox = _session.FindElementByAccessibilityId("ShowWindowMode");
            var locationComboBox = _session.FindElementByAccessibilityId("ShowWindowLocation");
            var stateComboBox = _session.FindElementByAccessibilityId("ShowWindowState");
            var canResizeCheckBox = _session.FindElementByAccessibilityId("ShowWindowCanResize");
            var showButton = _session.FindElementByAccessibilityId("ShowWindow");
            var extendClientAreaCheckBox = _session.FindElementByAccessibilityId("ShowWindowExtendClientAreaToDecorationsHint");

            if (size.HasValue)
                sizeTextBox.SendKeys($"{size.Value.Width}, {size.Value.Height}");

            if (modeComboBox.GetComboBoxValue() != mode.ToString())
            {
                modeComboBox.Click();
                _session.FindElementByName(mode.ToString()).SendClick();
            }

            if (locationComboBox.GetComboBoxValue() != location.ToString())
            {
                locationComboBox.Click();
                _session.FindElementByName(location.ToString()).SendClick();
            }

            if (stateComboBox.GetComboBoxValue() != state.ToString())
            {
                stateComboBox.Click();
                _session.FindElementByAccessibilityId($"ShowWindowState{state}").SendClick();
            }

            if (canResizeCheckBox.GetIsChecked() != canResize)
                canResizeCheckBox.Click();

            if (extendClientAreaCheckBox.GetIsChecked() != extendClientArea)
                extendClientAreaCheckBox.Click();

            return showButton.OpenWindowWithClick();
        }

        private AppiumWebElement GetWindow(string identifier)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // The Avalonia a11y tree currently exposes two nested Window elements, this is a bug and should be fixed 
                // but in the meantime use the `parent::' selector to return the parent "real" window. 
                return _session.FindElementByXPath(
                    $"XCUIElementTypeWindow//*[@identifier='{identifier}']/parent::XCUIElementTypeWindow");
            }
            else
            {
                return _session.FindElementByXPath($"//Window[@AutomationId='{identifier}']");
            }
        }

        private WindowInfo GetWindowInfo()
        {
            PixelRect? ReadOwnerRect()
            {
                var text = _session.FindElementByAccessibilityId("CurrentOwnerRect").Text;
                return !string.IsNullOrWhiteSpace(text) ? PixelRect.Parse(text) : null;
            }

            var retry = 0;

            for (;;)
            {
                try
                {
                    return new(
                        Size.Parse(_session.FindElementByAccessibilityId("CurrentClientSize").Text),
                        Size.Parse(_session.FindElementByAccessibilityId("CurrentFrameSize").Text),
                        PixelPoint.Parse(_session.FindElementByAccessibilityId("CurrentPosition").Text),
                        ReadOwnerRect(),
                        PixelRect.Parse(_session.FindElementByAccessibilityId("CurrentScreenRect").Text),
                        double.Parse(_session.FindElementByAccessibilityId("CurrentScaling").Text),
                        Enum.Parse<WindowState>(_session.FindElementByAccessibilityId("CurrentWindowState").Text));
                }
                catch (OpenQA.Selenium.NoSuchElementException) when (retry++ < 3)
                {
                    // MacOS sometimes seems to need a bit of time to get itself back in order after switching out
                    // of fullscreen.
                    Thread.Sleep(1000);
                }
            }
        }

        public enum ShowWindowMode
        {
            NonOwned,
            Owned,
            Modal
        }

        private record WindowInfo(
            Size ClientSize,
            Size FrameSize,
            PixelPoint Position,
            PixelRect? OwnerRect,
            PixelRect ScreenRect,
            double Scaling,
            WindowState WindowState);
    }
}
