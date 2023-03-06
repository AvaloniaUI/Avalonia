using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Interactions;
using Xunit;

namespace Avalonia.IntegrationTests.Appium
{
    [Collection("Default")]
    public abstract class MenuTests
    {
        private readonly AppiumDriver<AppiumWebElement> _driver;

        public MenuTests(DefaultAppFixture fixture)
        {
            _driver = fixture.Driver;

            var tabs = _driver.FindElementByAccessibilityId("MainTabs");
            var tab = tabs.FindElementByName("Menu");
            tab.Click();

            var reset = _driver.FindElementByAccessibilityId("MenuClickedMenuItemReset");
            reset.Click();

            var clickedMenuItem = _driver.FindElementByAccessibilityId("ClickedMenuItem");
            Assert.Equal("None", clickedMenuItem.Text);
        }

        [Fact]
        public void Click_Child()
        {
            var rootMenuItem = _driver.FindElementByAccessibilityId("RootMenuItem");
            
            rootMenuItem.SendClick();

            var childMenuItem = _driver.FindElementByAccessibilityId("Child1MenuItem");
            childMenuItem.SendClick();

            var clickedMenuItem = _driver.FindElementByAccessibilityId("ClickedMenuItem");
            Assert.Equal("_Child 1", clickedMenuItem.Text);
        }

        [Fact]
        public void Click_Grandchild()
        {
            var rootMenuItem = _driver.FindElementByAccessibilityId("RootMenuItem");
            
            rootMenuItem.SendClick();

            var childMenuItem = _driver.FindElementByAccessibilityId("Child2MenuItem");
            childMenuItem.SendClick();

            var grandchildMenuItem = _driver.FindElementByAccessibilityId("GrandchildMenuItem");
            grandchildMenuItem.SendClick();

            var clickedMenuItem = _driver.FindElementByAccessibilityId("ClickedMenuItem");
            Assert.Equal("_Grandchild", clickedMenuItem.Text);
        }

        [PlatformFact(TestPlatforms.Windows)]
        public void Select_Child_With_Alt_Arrow_Keys()
        {
            MovePointerOutOfTheWay();

            new Actions(_driver)
                .KeyDown(Keys.Alt).KeyUp(Keys.Alt)
                .SendKeys(Keys.Down + Keys.Enter)
                .Perform();

            var clickedMenuItem = _driver.FindElementByAccessibilityId("ClickedMenuItem");
            Assert.Equal("_Child 1", clickedMenuItem.Text);
        }

        [PlatformFact(TestPlatforms.Windows)]
        public void Select_Grandchild_With_Alt_Arrow_Keys()
        {
            MovePointerOutOfTheWay();

            new Actions(_driver)
                .KeyDown(Keys.Alt).KeyUp(Keys.Alt)
                .SendKeys(Keys.Down + Keys.Down + Keys.Right + Keys.Enter)
                .Perform();

            var clickedMenuItem = _driver.FindElementByAccessibilityId("ClickedMenuItem");
            Assert.Equal("_Grandchild", clickedMenuItem.Text);
        }

        [PlatformFact(TestPlatforms.Windows)]
        public void Select_Child_With_Alt_Access_Keys()
        {
            MovePointerOutOfTheWay();

            new Actions(_driver)
                .KeyDown(Keys.Alt).KeyUp(Keys.Alt)
                .SendKeys("rc")
                .Perform();

            var clickedMenuItem = _driver.FindElementByAccessibilityId("ClickedMenuItem");
            Assert.Equal("_Child 1", clickedMenuItem.Text);
        }

        [PlatformFact(TestPlatforms.Windows)]
        public void Select_Grandchild_With_Alt_Access_Keys()
        {
            MovePointerOutOfTheWay();

            new Actions(_driver)
                .KeyDown(Keys.Alt).KeyUp(Keys.Alt)
                .SendKeys("rhg")
                .Perform();

            var clickedMenuItem = _driver.FindElementByAccessibilityId("ClickedMenuItem");
            Assert.Equal("_Grandchild", clickedMenuItem.Text);
        }

        [PlatformFact(TestPlatforms.Windows)]
        public void Select_Child_With_Click_Arrow_Keys()
        {
            var rootMenuItem = _driver.FindElementByAccessibilityId("RootMenuItem");
            rootMenuItem.SendClick();

            MovePointerOutOfTheWay();

            new Actions(_driver)
                .SendKeys(Keys.Down + Keys.Enter)
                .Perform();

            var clickedMenuItem = _driver.FindElementByAccessibilityId("ClickedMenuItem");
            Assert.Equal("_Child 1", clickedMenuItem.Text);
        }

        [PlatformFact(TestPlatforms.Windows)]
        public void Select_Grandchild_With_Click_Arrow_Keys()
        {
            var rootMenuItem = _driver.FindElementByAccessibilityId("RootMenuItem");
            rootMenuItem.SendClick();

            MovePointerOutOfTheWay();

            new Actions(_driver)
                .SendKeys(Keys.Down + Keys.Down + Keys.Right + Keys.Enter)
                .Perform();

            var clickedMenuItem = _driver.FindElementByAccessibilityId("ClickedMenuItem");
            Assert.Equal("_Grandchild", clickedMenuItem.Text);
        }

        [PlatformFact(TestPlatforms.Windows)]
        public void Child_AcceleratorKey()
        {
            var rootMenuItem = _driver.FindElementByAccessibilityId("RootMenuItem");

            rootMenuItem.SendClick();

            var childMenuItem = _driver.FindElementByAccessibilityId("Child1MenuItem");

            Assert.Equal("Ctrl+O", childMenuItem.GetAttribute("AcceleratorKey"));
        }

        [PlatformFact(TestPlatforms.Windows)]
        public void PointerOver_Does_Not_Steal_Focus()
        {
            // Issue #7906
            var textBox = _driver.FindElementByAccessibilityId("MenuFocusTest");
            textBox.Click();

            Assert.True(textBox.GetIsFocused());

            var rootMenuItem = _driver.FindElementByAccessibilityId("RootMenuItem");
            rootMenuItem.MovePointerOver();

            Assert.True(textBox.GetIsFocused());
        }

        private void MovePointerOutOfTheWay()
        {
            // Move the pointer to the menu tab item so that it's not over the menu in preparation
            // for key press tests. This prevents the mouse accidentially selecting the wrong item
            // by hovering.
            var tabs = _driver.FindElementByAccessibilityId("MainTabs");
            var tab = tabs.FindElementByName("Menu");
            tab.MovePointerOver();
        }

        [Collection("Default")]
        public class Default : MenuTests
        {
            public Default(DefaultAppFixture fixture) : base(fixture) { }
        }

        [Collection("OverlayPopups")]
        public class OverlayPopups : MenuTests
        {
            public OverlayPopups(OverlayPopupsAppFixture fixture) : base(fixture) { }
        }
    }
}
