using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
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
        public void StartupLocation(Size? size, ShowWindowMode mode, WindowStartupLocation location)
        {
            using var window = OpenWindow(size, mode, location);
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
        public void WindowState(Size? size, ShowWindowMode mode, WindowState state)
        {
            using var window = OpenWindow(size, mode, state: state);

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
                try
                {
                    _session.FindElementByAccessibilityId("WindowState").SendClick();
                    _session.FindElementByName("Normal").SendClick();

                    // Wait for animations to run.
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                        Thread.Sleep(1000);

                } catch { /* Ignore errors in cleanup */ }
            }
        }

        [PlatformFact(TestPlatforms.Windows)]
        public void OnWindows_Docked_Windows_Retain_Size_Position_When_Restored()
        {
            using (OpenWindow(new Size(400, 400), ShowWindowMode.NonOwned, WindowStartupLocation.Manual))
            {
                var windowState = _session.FindElementByAccessibilityId("WindowState");

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


        [Theory]
        [InlineData(ShowWindowMode.NonOwned)]
        [InlineData(ShowWindowMode.Owned)]
        [InlineData(ShowWindowMode.Modal)]
        public void ShowMode(ShowWindowMode mode)
        {
            using var window = OpenWindow(null, mode, WindowStartupLocation.Manual);
            var windowState = _session.FindElementByAccessibilityId("WindowState");
            var original = GetWindowInfo();

            Assert.Equal("Normal", windowState.GetComboBoxValue());

            windowState.Click();
            _session.FindElementByName("Maximized").SendClick();
            Assert.Equal("Maximized", windowState.GetComboBoxValue());

            windowState.Click();
            _session.FindElementByName("Normal").SendClick();

            var current = GetWindowInfo();
            Assert.Equal(original.Position, current.Position);
            Assert.Equal(original.FrameSize, current.FrameSize);

            // On macOS, only non-owned windows can go fullscreen.
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || mode == ShowWindowMode.NonOwned)
            {
                windowState.Click();
                _session.FindElementByName("FullScreen").SendClick();
                Assert.Equal("FullScreen", windowState.GetComboBoxValue());

                current = GetWindowInfo();
                var clientSize = PixelSize.FromSize(current.ClientSize, current.Scaling);
                Assert.True(clientSize.Width >= current.ScreenRect.Width);
                Assert.True(clientSize.Height >= current.ScreenRect.Height);

                windowState.SendClick();
                
                _session.FindElementByName("Normal").SendClick();

                current = GetWindowInfo();
                Assert.Equal(original.Position, current.Position);
                Assert.Equal(original.FrameSize, current.FrameSize);
            }
        }

        public static TheoryData<Size?, ShowWindowMode, WindowStartupLocation> StartupLocationData()
        {
            var sizes = new Size?[] { null, new Size(400, 300) };
            var data = new TheoryData<Size?, ShowWindowMode, WindowStartupLocation>();

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

        public static TheoryData<Size?, ShowWindowMode, WindowState> WindowStateData()
        {
            var sizes = new Size?[] { null, new Size(400, 300) };
            var data = new TheoryData<Size?, ShowWindowMode, WindowState>();

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
                        
                        data.Add(size, mode, state);
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
            WindowState state = Controls.WindowState.Normal)
        {
            var sizeTextBox = _session.FindElementByAccessibilityId("ShowWindowSize");
            var modeComboBox = _session.FindElementByAccessibilityId("ShowWindowMode");
            var locationComboBox = _session.FindElementByAccessibilityId("ShowWindowLocation");
            var stateComboBox = _session.FindElementByAccessibilityId("ShowWindowState");
            var showButton = _session.FindElementByAccessibilityId("ShowWindow");

            if (size.HasValue)
                sizeTextBox.SendKeys($"{size.Value.Width}, {size.Value.Height}");

            modeComboBox.Click();
            _session.FindElementByName(mode.ToString()).SendClick();

            locationComboBox.Click();
            _session.FindElementByName(location.ToString()).SendClick();

            stateComboBox.Click();
            _session.FindElementByName(state.ToString()).SendClick();

            return showButton.OpenWindowWithClick();
        }

        private WindowInfo GetWindowInfo()
        {
            PixelRect? ReadOwnerRect()
            {
                var text = _session.FindElementByAccessibilityId("OwnerRect").Text;
                return !string.IsNullOrWhiteSpace(text) ? PixelRect.Parse(text) : null;
            }

            var retry = 0;

            for (;;)
            {
                try
                {
                    return new(
                        Size.Parse(_session.FindElementByAccessibilityId("ClientSize").Text),
                        Size.Parse(_session.FindElementByAccessibilityId("FrameSize").Text),
                        PixelPoint.Parse(_session.FindElementByAccessibilityId("Position").Text),
                        ReadOwnerRect(),
                        PixelRect.Parse(_session.FindElementByAccessibilityId("ScreenRect").Text),
                        double.Parse(_session.FindElementByAccessibilityId("Scaling").Text),
                        Enum.Parse<WindowState>(_session.FindElementByAccessibilityId("WindowState").Text));
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
