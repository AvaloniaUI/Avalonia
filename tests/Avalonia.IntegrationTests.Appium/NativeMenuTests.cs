using System.Diagnostics;
using System.IO;
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

    [Collection("Default")]
    public class DockMenuTests : TestBase
    {
        private const string DockAppName = "IntegrationTestApp";

        public DockMenuTests(DefaultAppFixture fixture)
            : base(fixture, "DesktopPage")
        {
        }

        [PlatformFact(TestPlatforms.MacOS)]
        public void MacOS_DockMenu_Can_Add_Items_Dynamically()
        {
            var countText = Session.FindElementByAccessibilityId("DockMenuItemCount");
            Assert.Equal("0", countText.Text);

            var addButton = Session.FindElementByAccessibilityId("AddDockMenuItem");
            addButton.Click();

            Thread.Sleep(500);

            countText = Session.FindElementByAccessibilityId("DockMenuItemCount");
            Assert.Equal("2", countText.Text);

            addButton.Click();

            Thread.Sleep(500);

            countText = Session.FindElementByAccessibilityId("DockMenuItemCount");
            Assert.Equal("3", countText.Text);
        }

        [PlatformFact(TestPlatforms.MacOS, Skip = "Requires runner to have accessibility permissions")]
        public void MacOS_DockMenu_Show_Main_Window_Sets_Checkbox()
        {
            var checkbox = Session.FindElementByAccessibilityId("DockMenuShowMainWindow");
            Assert.Equal(false, checkbox.GetIsChecked());

            ClickDockMenuItem(DockAppName, "Show Main Window");

            Thread.Sleep(2000);

            checkbox = Session.FindElementByAccessibilityId("DockMenuShowMainWindow");
            Assert.Equal(true, checkbox.GetIsChecked());
        }

        private static void ClickDockMenuItem(string appName, string menuItemName)
        {
            // Create the AppleScript to click the dock menu item.
            // Trying to get Appium to talk to the Dock directly is _pain_.
             
            var script = $@"
tell application ""System Events""
    tell process ""Dock""
        tell list 1
            perform action ""AXShowMenu"" of UI element ""{appName}""
            delay 0.5
            click menu item ""{menuItemName}"" of menu 1 of UI element ""{appName}""
        end tell
    end tell
end tell";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, script);
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "osascript",
                        Arguments = tempFile,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                process.WaitForExit(10000);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }
    }
}
