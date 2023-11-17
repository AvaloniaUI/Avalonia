using System.Threading;
using OpenQA.Selenium.Appium;
using Xunit;

namespace Avalonia.IntegrationTests.Appium
{
    [Collection("Default")]
    public class NativeMenuTests
    {
        private readonly AppiumDriver<AppiumWebElement> _session;

        public NativeMenuTests(DefaultAppFixture fixture)
        {
            _session = fixture.Session;

            var tabs = _session.FindElementByAccessibilityId("MainTabs");
            var tab = tabs.FindElementByName("Automation");
            tab.Click();
        }

        [PlatformFact(TestPlatforms.MacOS)]
        public void MacOS_View_Menu_Select_Button_Tab()
        {
            var tabs = _session.FindElementByAccessibilityId("MainTabs");
            var buttonTab = tabs.FindElementByName("Button");
            var menuBar = _session.FindElementByXPath("/XCUIElementTypeApplication/XCUIElementTypeMenuBar");
            var viewMenu = menuBar.FindElementByName("View");
            
            Assert.False(buttonTab.Selected);
            
            viewMenu.Click();
            var buttonMenu = viewMenu.FindElementByName("Button");
            buttonMenu.Click();

            Assert.True(buttonTab.Selected);
        }

        [PlatformFact(TestPlatforms.Windows)]
        public void Win32_View_Menu_Select_Button_Tab()
        {
            var tabs = _session.FindElementByAccessibilityId("MainTabs");
            var buttonTab = tabs.FindElementByName("Button");
            var viewMenu = _session.FindElementByXPath("//MenuItem[@Name='View']");

            Assert.False(buttonTab.Selected);

            viewMenu.Click();
            var buttonMenu = viewMenu.FindElementByName("Button");
            buttonMenu.Click();

            Assert.True(buttonTab.Selected);
        }

        [PlatformFact(TestPlatforms.MacOS)]
        public void MacOS_Sanitizes_Access_Key_Markers_When_Included_In_Menu_Title()
        {
            var menuBar = _session.FindElementByXPath("/XCUIElementTypeApplication/XCUIElementTypeMenuBar");

            Assert.True(menuBar.FindElementsByName("_Options").Count == 0);
            Assert.True(menuBar.FindElementsByName("Options").Count == 1);
        }

        [PlatformFact(TestPlatforms.Windows)]
        public void Win32_Avalonia_Menu_Has_ToolTip_If_Defined()
        {
            var viewMenu = _session.FindElementByXPath("//MenuItem[@Name='View']");
            viewMenu.Click();

            var buttonMenuItem = viewMenu.FindElementByName("Button");
            buttonMenuItem.MovePointerOver();

            // Wait for tooltip to open.
            Thread.Sleep(1000);

            var toolTipCandidates = _session.FindElementsByClassName("TextBlock");
            Assert.Contains(toolTipCandidates, x => x.Text == "Button");
        }

        [PlatformFact(TestPlatforms.MacOS)]
        public void MacOS_Native_Menu_Has_ToolTip_If_Defined()
        {
            var menuBar = _session.FindElementByXPath("/XCUIElementTypeApplication/XCUIElementTypeMenuBar");
            var viewMenu = menuBar.FindElementByName("View");
            viewMenu.Click();

            var buttonMenuItem = viewMenu.FindElementByName("Button");
            buttonMenuItem.MovePointerOver();

            // Wait for tooltip to open.
            Thread.Sleep(2000);

            var toolTipCandidates = _session.FindElementsByClassName("XCUIElementTypeStaticText");
            Assert.Contains(toolTipCandidates, x => x.Text == "Button");
        }
    }
}
