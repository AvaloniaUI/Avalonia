using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using Xunit;

namespace Avalonia.IntegrationTests.Appium
{
    [Collection("Default")]
    public class NativeMenuTests
    {
        private readonly AppiumDriver _session;

        public NativeMenuTests(DefaultAppFixture fixture)
        {
            _session = fixture.Session;

            var tabs = _session.FindElement(MobileBy.AccessibilityId("MainTabs"));
            var tab = tabs.FindElement(MobileBy.Name("Automation"));
            tab.Click();
        }

        [PlatformFact(TestPlatforms.MacOS)]
        public void MacOS_View_Menu_Select_Button_Tab()
        {
            var tabs = _session.FindElement(MobileBy.AccessibilityId("MainTabs"));
            var buttonTab = tabs.FindElement(MobileBy.Name("Button"));
            var menuBar = _session.FindElement(MobileBy.XPath("/XCUIElementTypeApplication/XCUIElementTypeMenuBar"));
            var viewMenu = menuBar.FindElement(MobileBy.Name("View"));
            
            Assert.False(buttonTab.Selected);
            
            viewMenu.Click();
            var buttonMenu = viewMenu.FindElement(MobileBy.Name("Button"));
            buttonMenu.Click();

            Assert.True(buttonTab.Selected);
        }

        [PlatformFact(TestPlatforms.Windows)]
        public void Win32_View_Menu_Select_Button_Tab()
        {
            var tabs = _session.FindElement(MobileBy.AccessibilityId("MainTabs"));
            var buttonTab = tabs.FindElement(MobileBy.Name("Button"));
            var viewMenu = _session.FindElement(MobileBy.XPath("//MenuItem[@Name='View']"));

            Assert.False(buttonTab.Selected);

            viewMenu.Click();
            var buttonMenu = viewMenu.FindElement(MobileBy.Name("Button"));
            buttonMenu.Click();

            Assert.True(buttonTab.Selected);
        }

        [PlatformFact(TestPlatforms.MacOS)]
        public void MacOS_Sanitizes_Access_Key_Markers_When_Included_In_Menu_Title()
        {
            var menuBar = _session.FindElement(MobileBy.XPath("/XCUIElementTypeApplication/XCUIElementTypeMenuBar"));

            Assert.True(menuBar.FindElements(MobileBy.Name("_Options")).Count == 0);
            Assert.True(menuBar.FindElements(MobileBy.Name("Options")).Count == 1);
        }

        [PlatformFact(TestPlatforms.Windows)]
        public void Win32_Avalonia_Menu_Has_ToolTip_If_Defined()
        {
            var viewMenu = _session.FindElement(MobileBy.XPath("//MenuItem[@Name='View']"));
            viewMenu.Click();

            var buttonMenuItem = viewMenu.FindElement(MobileBy.Name("Button"));
            buttonMenuItem.MovePointerOver();

            // Wait for tooltip to open.
            Thread.Sleep(2000);

            var toolTipCandidates = _session.FindElements(MobileBy.ClassName("TextBlock"));
            Assert.Contains(toolTipCandidates, x => x.Text == "Tip:Button");
        }

        [PlatformFact(TestPlatforms.MacOS, Skip = "Flaky test")]
        public void MacOS_Native_Menu_Has_ToolTip_If_Defined()
        {
            var menuBar = _session.FindElement(MobileBy.XPath("/XCUIElementTypeApplication/XCUIElementTypeMenuBar"));
            var viewMenu = menuBar.FindElement(MobileBy.Name("View"));
            viewMenu.Click();

            var buttonMenuItem = viewMenu.FindElement(MobileBy.Name("Button"));
            buttonMenuItem.MovePointerOver();

            // Wait for tooltip to open.
            Thread.Sleep(4000);

            var toolTipCandidates = _session.FindElements(MobileBy.ClassName("XCUIElementTypeStaticText"));
            Assert.Contains(toolTipCandidates, x => x.Text == "Tip:Button");
        }
    }
}
