using System;
using System.Linq;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Interactions;
using Xunit;

namespace Avalonia.IntegrationTests.Appium;

// TODO implement macOS tests.
[Collection("Default")]
public class TrayIconTests : IDisposable
{
    private readonly AppiumDriver _session;
    private readonly AppiumDriver _rootSession;
    private const string TrayIconName = "IntegrationTestApp TrayIcon";

    public TrayIconTests(DefaultAppFixture fixture)
    {
        _session = fixture.Session;
        // "Root" is a special name for windows the desktop session, that has access to task bar.
        _rootSession = fixture.CreateNestedSession("Root");

        var tabs = _session.FindElementByAccessibilityId("MainTabs");
        var tab = tabs.FindElementByName("Desktop");
        tab.Click();
    }

    [PlatformFact(TestPlatforms.Windows)]
    public void Should_Handle_Left_Click()
    {
        var avaloinaTrayIconButton = GetTrayIconButton(_rootSession, TrayIconName);
        Assert.NotNull(avaloinaTrayIconButton);

        avaloinaTrayIconButton.Click();

        Thread.Sleep(1000);
        
        var checkBox = _session.FindElementByAccessibilityId("TrayIconClicked");
        Assert.True(checkBox.GetIsChecked());
    }

    [PlatformFact(TestPlatforms.Windows)]
    public void Should_Handle_Context_Menu_Item_Click()
    {
        var avaloinaTrayIconButton = GetTrayIconButton(_rootSession, TrayIconName);
        Assert.NotNull(avaloinaTrayIconButton);

        var contextMenu = ShowAndGetTrayMenu(avaloinaTrayIconButton, TrayIconName);
        Assert.NotNull(contextMenu);

        var menuItem = contextMenu.FindElementByName("Raise Menu Clicked");
        menuItem.Click();
        
        Thread.Sleep(1000);

        var checkBox = _session.FindElementByAccessibilityId("TrayIconMenuClicked");
        Assert.True(checkBox.GetIsChecked());
    }

    [PlatformFact(TestPlatforms.Windows)]
    public void Can_Toggle_TrayIcon_Visibility()
    {
        var avaloinaTrayIconButton = GetTrayIconButton(_rootSession, TrayIconName);
        Assert.NotNull(avaloinaTrayIconButton);

        var toggleButton = _session.FindElementByAccessibilityId("ToggleTrayIconVisible");
        toggleButton.SendClick();

        avaloinaTrayIconButton = GetTrayIconButton(_rootSession, TrayIconName);
        Assert.Null(avaloinaTrayIconButton);

        toggleButton.SendClick();

        avaloinaTrayIconButton = GetTrayIconButton(_rootSession, TrayIconName);
        Assert.NotNull(avaloinaTrayIconButton);
    }

    private static AppiumWebElement? GetTrayIconButton(AppiumDriver session, string trayIconName)
    {
        var taskBar = session.FindElementByName("Taskbar");

        if (TryToGetIcon(taskBar, trayIconName) is { } trayIcon)
        {
            return trayIcon;
        }
        else
        {
            // Add a sleep here, as previous test might still run popup closing animation.
            Thread.Sleep(1000);
            
            // Try to open "Show hidden icons"
            var trayIconsButton = taskBar.FindElementByAccessibilityId("SystemTrayIcon");
            trayIconsButton.Click();

            // Is it localizable? Or is it safe?
            var trayIconsFlyout = session.FindElementByName("System tray overflow window.");
            return TryToGetIcon(trayIconsFlyout, trayIconName);
        }

        static AppiumWebElement? TryToGetIcon(AppiumWebElement parent, string trayIconName) =>
            parent.FindElementsByName(trayIconName).LastOrDefault()
            // Some icons (including Avalonia) for some reason include leading whitespace in their name.
            // Couldn't find any info on that, which is weird.
            ?? parent.FindElementsByName(" " + trayIconName).LastOrDefault();
    }

    private static AppiumWebElement? ShowAndGetTrayMenu(AppiumWebElement trayIcon, string trayIconName)
    {
        var session = (AppiumDriver)trayIcon.WrappedDriver;
        new Actions(trayIcon.WrappedDriver).ContextClick(trayIcon).Perform();

        Thread.Sleep(1000);

        return session.FindElementByXPath($"//Window[@AutomationId='AvaloniaTrayPopupRoot_{trayIconName}']");
    }

    public void Dispose()
    {
        _rootSession.Dispose();
    }
}
