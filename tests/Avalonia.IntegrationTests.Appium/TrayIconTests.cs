using System;
using System.IO;
using System.Linq;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Interactions;
using Xunit;

namespace Avalonia.IntegrationTests.Appium;

[Collection("Default")]
public class TrayIconTests : TestBase, IDisposable
{
    private readonly AppiumDriver? _rootSession;
    private const string TrayIconName = "IntegrationTestApp TrayIcon";

    public TrayIconTests(DefaultAppFixture fixture)
        : base(fixture, "Desktop")
    {
        // "Root" is a special name for windows the desktop session, that has access to task bar.
        if (OperatingSystem.IsWindows())
        {
            _rootSession = fixture.CreateNestedSession("Root");
        }
    }

    // Left click is only supported on Windows.
    [PlatformFact(TestPlatforms.Windows, Skip = "Flaky test")]
    public void Should_Handle_Left_Click()
    {
        var avaloniaTrayIconButton = GetTrayIconButton(_rootSession ?? Session, TrayIconName);
        Assert.NotNull(avaloniaTrayIconButton);

        avaloniaTrayIconButton.SendClick();

        Thread.Sleep(2000);
        
        var checkBox = Session.FindElementByAccessibilityId("TrayIconClicked");
        Assert.True(checkBox.GetIsChecked());
    }

    [Fact(Skip = "Flaky test")]
    public void Should_Handle_Context_Menu_Item_Click()
    {
        var avaloniaTrayIconButton = GetTrayIconButton(_rootSession ?? Session, TrayIconName);
        Assert.NotNull(avaloniaTrayIconButton);

        var contextMenu = ShowAndGetTrayMenu(avaloniaTrayIconButton, TrayIconName);
        Assert.NotNull(contextMenu);

        var menuItem = contextMenu.FindElementByName("Raise Menu Clicked");
        menuItem.SendClick();

        Thread.Sleep(2000);

        var checkBox = Session.FindElementByAccessibilityId("TrayIconMenuClicked");
        Assert.True(checkBox.GetIsChecked());
    }

    [Fact(Skip = "Flaky test")]
    public void Can_Toggle_TrayIcon_Visibility()
    {
        var avaloniaTrayIconButton = GetTrayIconButton(_rootSession ?? Session, TrayIconName);
        Assert.NotNull(avaloniaTrayIconButton);

        var toggleButton = Session.FindElementByAccessibilityId("ToggleTrayIconVisible");
        toggleButton.SendClick();

        avaloniaTrayIconButton = GetTrayIconButton(_rootSession ?? Session, TrayIconName);
        Assert.Null(avaloniaTrayIconButton);

        toggleButton.SendClick();

        avaloniaTrayIconButton = GetTrayIconButton(_rootSession ?? Session, TrayIconName);
        Assert.NotNull(avaloniaTrayIconButton);
    }

    private static AppiumWebElement? GetTrayIconButton(AppiumDriver session, string trayIconName)
    {
        if (OperatingSystem.IsWindows())
        {
            var taskBar = session.FindElementsByClassName("Shell_TrayWnd")
                .FirstOrDefault() ?? throw new InvalidOperationException("Couldn't find Taskbar on current system.");

            if (TryToGetIcon(taskBar, trayIconName) is { } trayIcon)
            {
                return trayIcon;
            }
            else
            {
                // Add a sleep here, as previous test might still run popup closing animation.
                Thread.Sleep(1000);

                // win11: SystemTrayIcon
                // win10: Notification Chevron
                var trayIconsButton = taskBar.FindElementsByAccessibilityId("SystemTrayIcon").FirstOrDefault()
                    ?? taskBar.FindElementsByName("Notification Chevron").FirstOrDefault()
                    ?? throw new InvalidOperationException("SystemTrayIcon cannot be found.");
                trayIconsButton.Click();

                // win11: TopLevelWindowForOverflowXamlIsland
                // win10: NotifyIconOverflowWindow
                var trayIconsFlyout = session.FindElementsByClassName("TopLevelWindowForOverflowXamlIsland").FirstOrDefault()
                      ?? session.FindElementsByClassName("NotifyIconOverflowWindow").FirstOrDefault()
                      ?? throw new InvalidOperationException("System tray overflow window cannot be found.");
                return TryToGetIcon(trayIconsFlyout, trayIconName);
            }

            static AppiumWebElement? TryToGetIcon(AppiumWebElement parent, string trayIconName) =>
                parent.FindElementsByName(trayIconName).LastOrDefault()
                // Some icons (including Avalonia) for some reason include leading whitespace in their name.
                // Couldn't find any info on that, which is weird.
                ?? parent.FindElementsByName(" " + trayIconName).LastOrDefault();
        }
        if (OperatingSystem.IsMacOS())
        {
            return session.FindElementsByXPath("//XCUIElementTypeStatusItem").FirstOrDefault();
        }

        throw new PlatformNotSupportedException();
    }

    private static AppiumWebElement ShowAndGetTrayMenu(AppiumWebElement trayIcon, string trayIconName)
    {
        if (OperatingSystem.IsWindows())
        {
            var session = (AppiumDriver)trayIcon.WrappedDriver;
            new Actions(trayIcon.WrappedDriver).ContextClick(trayIcon).Perform();

            Thread.Sleep(1000);

            return session.FindElementByXPath($"//Window[@AutomationId='AvaloniaTrayPopupRoot_{trayIconName}']");
        }
        else
        {
            trayIcon.Click();
            return trayIcon.FindElementByXPath("//XCUIElementTypeStatusItem/XCUIElementTypeMenu");
        }
    }

    public void Dispose()
    {
        _rootSession?.Dispose();
    }
}
