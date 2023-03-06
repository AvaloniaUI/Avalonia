using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia.Controls;
using Avalonia.IntegrationTests.Appium.Wrappers;
using OpenQA.Selenium.Appium;
using Xunit;
using Xunit.Sdk;

namespace Avalonia.IntegrationTests.Appium;

public class WindowTestsBase
{
    private readonly ISession _session;
    private readonly IWindowElement _mainWindow;

    public WindowTestsBase(ISession session)
    {
        _session = session;
        _mainWindow = _session.GetWindow("MainWindow");
    }

    protected IWindowElement MainWindow => _mainWindow;

    protected ISession Session => _session;

    protected IWindowElement OpenWindow(
        Size? size,
        ShowWindowMode mode,
        WindowStartupLocation location = WindowStartupLocation.Manual,
        WindowState state = Controls.WindowState.Normal,
        bool canResize = true)
    {
        var timer = new SplitTimer();

        var elements = _mainWindow.GetChildren();

        timer.SplitLog("getChildren");

        if (size.HasValue)
        {
            var sizeTextBox = _mainWindow.FindElementByAccessibilityId("ShowWindowSize");
            timer.SplitLog(nameof(sizeTextBox));
            sizeTextBox.SendKeys($"{size.Value.Width}, {size.Value.Height}");
        }

        var modeComboBox = _mainWindow.FindElementByAccessibilityId("ShowWindowMode");
        timer.SplitLog(nameof(modeComboBox));

        if (modeComboBox.GetComboBoxValue() != mode.ToString())
        {
            modeComboBox.Click();
            _mainWindow.FindElementByName(mode.ToString()).SendClick();
        }

        var locationComboBox = _mainWindow.FindElementByAccessibilityId("ShowWindowLocation");
        timer.SplitLog(nameof(locationComboBox));
        if (locationComboBox.GetComboBoxValue() != location.ToString())
        {
            locationComboBox.Click();
            _mainWindow.FindElementByName(location.ToString()).SendClick();
        }

        var stateComboBox = _mainWindow.FindElementByAccessibilityId("ShowWindowState");
        timer.SplitLog(nameof(stateComboBox));
        if (stateComboBox.GetComboBoxValue() != state.ToString())
        {
            stateComboBox.Click();
            _mainWindow.FindElementByAccessibilityId($"ShowWindowState{state}").SendClick();
        }

        var canResizeCheckBox = _mainWindow.FindElementByAccessibilityId("ShowWindowCanResize");
        timer.SplitLog(nameof(canResizeCheckBox));
        if (canResizeCheckBox.GetIsChecked() != canResize)
            canResizeCheckBox.Click();

        timer.Reset();

        var showButton = _mainWindow.FindElementByAccessibilityId("ShowWindow");
        timer.SplitLog(nameof(showButton));

        var result = _session.GetNewWindow(() => showButton.Click());

        timer.SplitLog("GetNewWindow");

        return result;
    }

    protected static void AssertCloseEnough(PixelPoint expected, PixelPoint actual)
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

    protected static WindowInfo GetWindowInfo(IWindowElement window)
    {
        var dictionary = new Dictionary<string, string>();

        PixelRect? ReadOwnerRect()
        {
            if (dictionary.ContainsKey("ownerrect"))
            {
                return PixelRect.Parse(dictionary["ownerrect"]);
            }

            return null;
        }

        var retry = 0;

        for (;;)
        {
            try
            {
                var timer = new SplitTimer();
                var summary = window.FindElementByAccessibilityId("CurrentSummary").Text;

                var items = summary.Split("::");

                foreach (var item in items)
                {
                    var kv = item.Split(":");

                    if (kv.Length == 2)
                    {
                        var key = kv[0];
                        var value = kv[1];

                        dictionary[key] = value;
                    }
                }

                timer.SplitLog("summary");

                var result = new WindowInfo(
                    Size.Parse(dictionary["clientSize"]),
                    Size.Parse(dictionary["frameSize"]),
                    PixelPoint.Parse(dictionary["position"]),
                    ReadOwnerRect(),
                    PixelRect.Parse(dictionary["screen"]),
                    double.Parse(dictionary["scaling"]),
                    Enum.Parse<WindowState>(dictionary["windowstate"]));

                return result;
            }
            catch (OpenQA.Selenium.NoSuchElementException) when (retry++ < 3)
            {
                // MacOS sometimes seems to need a bit of time to get itself back in order after switching out
                // of fullscreen.
                Thread.Sleep(1000);
            }
        }
    }

    protected int GetWindowOrder(IWindowElement window)
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

    protected record WindowInfo(
        Size ClientSize,
        Size FrameSize,
        PixelPoint Position,
        PixelRect? OwnerRect,
        PixelRect ScreenRect,
        double Scaling,
        WindowState WindowState);
}
