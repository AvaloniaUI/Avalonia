using System.Threading;
using Xunit;

namespace Avalonia.IntegrationTests.Appium
{
    [Collection("Default")]
    public class NativeMenuTests : TestBase
    {
        public NativeMenuTests(DefaultAppFixture fixture)
            : base(fixture, "Automation")
        {
        }

        [PlatformFact(TestPlatforms.MacOS)]
        public void MacOS_View_Menu_Select_Button_Tab()
        {
            var tabs = Session.FindElementByAccessibilityId("Pager");
            var buttonTab = tabs.FindElementByName("Button");
            var menuBar = Session.FindElementByXPath("/XCUIElementTypeApplication/XCUIElementTypeMenuBar");
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
            var tabs = Session.FindElementByAccessibilityId("Pager");
            var buttonTab = tabs.FindElementByName("Button");
            var viewMenu = Session.FindElementByXPath("//MenuItem[@Name='View']");

            Assert.False(buttonTab.Selected);

            viewMenu.Click();
            var buttonMenu = viewMenu.FindElementByName("Button");
            buttonMenu.Click();

            Assert.True(buttonTab.Selected);
        }

        [PlatformFact(TestPlatforms.MacOS)]
        public void MacOS_Sanitizes_Access_Key_Markers_When_Included_In_Menu_Title()
        {
            var menuBar = Session.FindElementByXPath("/XCUIElementTypeApplication/XCUIElementTypeMenuBar");

            Assert.True(menuBar.FindElementsByName("_Options").Count == 0);
            Assert.True(menuBar.FindElementsByName("Options").Count == 1);
        }

        [PlatformFact(TestPlatforms.Windows)]
        public void Win32_Avalonia_Menu_Has_ToolTip_If_Defined()
        {
            var viewMenu = Session.FindElementByXPath("//MenuItem[@Name='View']");
            viewMenu.Click();

            var buttonMenuItem = viewMenu.FindElementByName("Button");
            buttonMenuItem.MovePointerOver();

            // Wait for tooltip to open.
            Thread.Sleep(2000);

            var toolTipCandidates = Session.FindElementsByClassName("TextBlock");
            Assert.Contains(toolTipCandidates, x => x.Text == "Tip:Button");
        }

        [PlatformFact(TestPlatforms.MacOS, Skip = "Flaky test")]
        public void MacOS_Native_Menu_Has_ToolTip_If_Defined()
        {
            var menuBar = Session.FindElementByXPath("/XCUIElementTypeApplication/XCUIElementTypeMenuBar");
            var viewMenu = menuBar.FindElementByName("View");
            viewMenu.Click();

            var buttonMenuItem = viewMenu.FindElementByName("Button");
            buttonMenuItem.MovePointerOver();

            // Wait for tooltip to open.
            Thread.Sleep(4000);

            var toolTipCandidates = Session.FindElementsByClassName("XCUIElementTypeStaticText");
            Assert.Contains(toolTipCandidates, x => x.Text == "Tip:Button");
        }
    }
}
