using System;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using OpenQA.Selenium.Appium;
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
}
