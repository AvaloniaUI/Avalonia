using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Interactions;
using Xunit;

namespace Avalonia.IntegrationTests.Appium
{
    [Collection("Default")]
    public class MenuTests
    {
        private readonly AppiumDriver<AppiumWebElement> _session;

        public MenuTests(TestAppFixture fixture)
        {
            _session = fixture.Session;

            var tabs = _session.FindElementByAccessibilityId("MainTabs");
            var tab = tabs.FindElementByName("Menu");
            tab.Click();

            var reset = _session.FindElementByAccessibilityId("MenuClickedMenuItemReset");
            reset.Click();

            var clickedMenuItem = _session.FindElementByAccessibilityId("ClickedMenuItem");
            Assert.Equal("None", clickedMenuItem.Text);
        }

        [Fact]
        public void Click_Child()
        {
            var fileMenu = _session.FindElementByAccessibilityId("FileMenu");
            
            fileMenu.SendClick();

            var openMenu = _session.FindElementByAccessibilityId("OpenMenu");
            openMenu.SendClick();

            var clickedMenuItem = _session.FindElementByAccessibilityId("ClickedMenuItem");
            Assert.Equal("_Open...", clickedMenuItem.Text);
        }

        [Fact]
        public void Click_Grandchild()
        {
            var fileMenu = _session.FindElementByAccessibilityId("FileMenu");
            
            fileMenu.SendClick();

            var openRecentMenu = _session.FindElementByAccessibilityId("OpenRecentMenu");
            openRecentMenu.SendClick();

            var file1Menu = _session.FindElementByAccessibilityId("OpenRecentFile1Menu");
            file1Menu.SendClick();

            var clickedMenuItem = _session.FindElementByAccessibilityId("ClickedMenuItem");
            Assert.Equal("File_1.txt", clickedMenuItem.Text);
        }

        [PlatformFact(SkipOnOSX = true)]
        public void Select_Child_With_Alt_Arrow_Keys()
        {
            new Actions(_session)
                .KeyDown(Keys.Alt).KeyUp(Keys.Alt)
                .SendKeys(Keys.Down + Keys.Enter)
                .Perform();

            var clickedMenuItem = _session.FindElementByAccessibilityId("ClickedMenuItem");
            Assert.Equal("_Open...", clickedMenuItem.Text);
        }

        [PlatformFact(SkipOnOSX = true)]
        public void Select_Grandchild_With_Alt_Arrow_Keys()
        {
            new Actions(_session)
                .KeyDown(Keys.Alt).KeyUp(Keys.Alt)
                .SendKeys(Keys.Down + Keys.Down + Keys.Right + Keys.Enter)
                .Perform();

            var clickedMenuItem = _session.FindElementByAccessibilityId("ClickedMenuItem");
            Assert.Equal("File_1.txt", clickedMenuItem.Text);
        }

        [PlatformFact(SkipOnOSX = true)]
        public void Select_Child_With_Alt_Access_Keys()
        {
            new Actions(_session)
                .KeyDown(Keys.Alt).KeyUp(Keys.Alt)
                .SendKeys("fo")
                .Perform();

            var clickedMenuItem = _session.FindElementByAccessibilityId("ClickedMenuItem");
            Assert.Equal("_Open...", clickedMenuItem.Text);
        }

        [PlatformFact(SkipOnOSX = true)]
        public void Select_Grandchild_With_Alt_Access_Keys()
        {
            new Actions(_session)
                .KeyDown(Keys.Alt).KeyUp(Keys.Alt)
                .SendKeys("fr1")
                .Perform();

            var clickedMenuItem = _session.FindElementByAccessibilityId("ClickedMenuItem");
            Assert.Equal("File_1.txt", clickedMenuItem.Text);
        }

        [PlatformFact(SkipOnOSX = true)]
        public void Select_Child_With_Click_Arrow_Keys()
        {
            var fileMenu = _session.FindElementByAccessibilityId("FileMenu");
            fileMenu.SendClick();

            new Actions(_session)
                .SendKeys(Keys.Down + Keys.Enter)
                .Perform();

            var clickedMenuItem = _session.FindElementByAccessibilityId("ClickedMenuItem");
            Assert.Equal("_Open...", clickedMenuItem.Text);
        }

        [PlatformFact(SkipOnOSX = true)]
        public void Select_Grandchild_With_Click_Arrow_Keys()
        {
            var fileMenu = _session.FindElementByAccessibilityId("FileMenu");
            fileMenu.SendClick();

            new Actions(_session)
                .SendKeys(Keys.Down + Keys.Down + Keys.Right + Keys.Enter)
                .Perform();

            var clickedMenuItem = _session.FindElementByAccessibilityId("ClickedMenuItem");
            Assert.Equal("File_1.txt", clickedMenuItem.Text);
        }

        [PlatformFact(SkipOnOSX = true)]
        public void Child_AcceleratorKey()
        {
            var fileMenu = _session.FindElementByAccessibilityId("FileMenu");

            fileMenu.SendClick();

            var openMenu = _session.FindElementByAccessibilityId("OpenMenu");

            Assert.Equal("Ctrl+O", openMenu.GetAttribute("AcceleratorKey"));
        }

        [PlatformFact(SkipOnOSX = true)]
        public void PointerOver_Does_Not_Steal_Focus()
        {
            // Issue #7906
            var textBox = _session.FindElementByAccessibilityId("MenuFocusTest");
            textBox.Click();

            Assert.True(textBox.GetIsFocused());

            var FileMenu = _session.FindElementByAccessibilityId("FileMenu");
            FileMenu.MovePointerOver();

            Assert.True(textBox.GetIsFocused());
        }
    }
}
