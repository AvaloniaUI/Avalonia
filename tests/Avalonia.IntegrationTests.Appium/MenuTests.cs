using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Interactions;
using System.Threading;
using Xunit;

namespace Avalonia.IntegrationTests.Appium
{
    [Collection("Default")]
    public abstract class MenuTests : TestBase
    {
        public MenuTests(DefaultAppFixture fixture)
            : base(fixture, "Menu")
        {
            var reset = Session.FindElementByAccessibilityId("MenuClickedMenuItemReset");
            reset.Click();

            var clickedMenuItem = Session.FindElementByAccessibilityId("ClickedMenuItem");
            Assert.Equal("None", clickedMenuItem.Text);
        }

        [PlatformFact(TestPlatforms.Windows | TestPlatforms.MacOS)]
        public void Click_Child()
        {
            ClickChildCore();
        }

        [PlatformFact(TestPlatforms.Windows | TestPlatforms.MacOS)]
        public void Click_Grandchild()
        {
            ClickGrandchildCore();
        }

        protected void ClickChildCore()
        {
            ClickChildCore(Session);
        }

        private static void ClickChildCore(AppiumDriver session)
        {
            var childMenuItem = OpenRootMenuAndFindItem(session, "Child1MenuItem");
            childMenuItem.SendClick();

            var clickedMenuItem = session.FindElementByAccessibilityId("ClickedMenuItem");
            Assert.Equal("_Child 1", clickedMenuItem.Text);
        }

        protected void ClickGrandchildCore()
        {
            ClickGrandchildCore(Session);
        }

        private static void ClickGrandchildCore(AppiumDriver session)
        {
            var childMenuItem = OpenRootMenuAndFindItem(session, "Child2MenuItem");
            childMenuItem.SendClick();

            var grandchildMenuItem = FindMenuItemWithFallbackClick(
                session,
                childMenuItem,
                "GrandchildMenuItem");
            grandchildMenuItem.SendClick();

            var clickedMenuItem = session.FindElementByAccessibilityId("ClickedMenuItem");
            Assert.Equal("_Grandchild", clickedMenuItem.Text);
        }

        private static AppiumWebElement OpenRootMenuAndFindItem(AppiumDriver session, string itemAccessibilityId)
        {
            var rootMenuItem = session.FindElementByAccessibilityId("RootMenuItem");
            return FindMenuItemWithFallbackClick(session, rootMenuItem, itemAccessibilityId);
        }

        private static AppiumWebElement FindMenuItemWithFallbackClick(
            AppiumDriver session,
            AppiumWebElement opener,
            string itemAccessibilityId)
        {
            for (var attempt = 0; attempt < 3; ++attempt)
            {
                opener.SendClick();
                if (TryFindMenuItem(session, itemAccessibilityId, out var item))
                    return item;

                // Some backends expose "select" without opening the popup; force a physical click as fallback.
                new Actions(session).MoveToElement(opener).Click().Perform();
                if (TryFindMenuItem(session, itemAccessibilityId, out item))
                    return item;

                Thread.Sleep(100);
            }

            return session.FindElementByAccessibilityId(itemAccessibilityId);
        }

        private static bool TryFindMenuItem(
            AppiumDriver session,
            string itemAccessibilityId,
            out AppiumWebElement menuItem)
        {
            try
            {
                menuItem = session.FindElementByAccessibilityId(itemAccessibilityId);
                return true;
            }
            catch (NoSuchElementException)
            {
                menuItem = null!;
                return false;
            }
        }

        [PlatformFact(TestPlatforms.Windows)]
        public void Select_Child_With_Alt_Arrow_Keys()
        {
            MovePointerOutOfTheWay();

            new Actions(Session)
                .KeyDown(Keys.Alt).KeyUp(Keys.Alt)
                .SendKeys(Keys.Down + Keys.Enter)
                .Perform();

            var clickedMenuItem = Session.FindElementByAccessibilityId("ClickedMenuItem");
            Assert.Equal("_Child 1", clickedMenuItem.Text);
        }

        [PlatformFact(TestPlatforms.Windows)]
        public void Select_Grandchild_With_Alt_Arrow_Keys()
        {
            MovePointerOutOfTheWay();

            new Actions(Session)
                .KeyDown(Keys.Alt).KeyUp(Keys.Alt)
                .SendKeys(Keys.Down + Keys.Down + Keys.Right + Keys.Enter)
                .Perform();

            var clickedMenuItem = Session.FindElementByAccessibilityId("ClickedMenuItem");
            Assert.Equal("_Grandchild", clickedMenuItem.Text);
        }

        [PlatformFact(TestPlatforms.Windows)]
        public void Select_Child_With_Alt_Access_Keys()
        {
            MovePointerOutOfTheWay();

            new Actions(Session)
                .KeyDown(Keys.Alt).KeyUp(Keys.Alt)
                .SendKeys("rc")
                .Perform();

            var clickedMenuItem = Session.FindElementByAccessibilityId("ClickedMenuItem");
            Assert.Equal("_Child 1", clickedMenuItem.Text);
        }

        [PlatformFact(TestPlatforms.Windows)]
        public void Select_Grandchild_With_Alt_Access_Keys()
        {
            MovePointerOutOfTheWay();

            new Actions(Session)
                .KeyDown(Keys.Alt).KeyUp(Keys.Alt)
                .SendKeys("rhg")
                .Perform();

            var clickedMenuItem = Session.FindElementByAccessibilityId("ClickedMenuItem");
            Assert.Equal("_Grandchild", clickedMenuItem.Text);
        }

        [PlatformFact(TestPlatforms.Windows)]
        public void Select_Child_With_Click_Arrow_Keys()
        {
            var rootMenuItem = Session.FindElementByAccessibilityId("RootMenuItem");
            rootMenuItem.SendClick();

            MovePointerOutOfTheWay();

            new Actions(Session)
                .SendKeys(Keys.Down + Keys.Enter)
                .Perform();

            var clickedMenuItem = Session.FindElementByAccessibilityId("ClickedMenuItem");
            Assert.Equal("_Child 1", clickedMenuItem.Text);
        }

        [PlatformFact(TestPlatforms.Windows)]
        public void Select_Grandchild_With_Click_Arrow_Keys()
        {
            var rootMenuItem = Session.FindElementByAccessibilityId("RootMenuItem");
            rootMenuItem.SendClick();

            MovePointerOutOfTheWay();

            new Actions(Session)
                .SendKeys(Keys.Down + Keys.Down + Keys.Right + Keys.Enter)
                .Perform();

            var clickedMenuItem = Session.FindElementByAccessibilityId("ClickedMenuItem");
            Assert.Equal("_Grandchild", clickedMenuItem.Text);
        }

        [PlatformFact(TestPlatforms.Windows)]
        public void Child_AcceleratorKey()
        {
            ChildAcceleratorKeyCore();
        }

        protected void ChildAcceleratorKeyCore()
        {
            ChildAcceleratorKeyCore(Session);
        }

        private static void ChildAcceleratorKeyCore(AppiumDriver session)
        {
            var childMenuItem = OpenRootMenuAndFindItem(session, "Child1MenuItem");
            Assert.Equal("Ctrl+O", childMenuItem.GetAttribute("AcceleratorKey"));
        }

        [PlatformFact(TestPlatforms.Windows)]
        public void PointerOver_Does_Not_Steal_Focus()
        {
            // Issue #7906
            var textBox = Session.FindElementByAccessibilityId("MenuFocusTest");
            textBox.Click();

            Assert.True(textBox.GetIsFocused());

            var rootMenuItem = Session.FindElementByAccessibilityId("RootMenuItem");
            rootMenuItem.MovePointerOver();

            Assert.True(textBox.GetIsFocused());
        }

        private void MovePointerOutOfTheWay()
        {
            // Move the pointer to the menu tab item so that it's not over the menu in preparation
            // for key press tests. This prevents the mouse accidentally selecting the wrong item
            // by hovering.
            var tabs = Session.FindElementByAccessibilityId("Pager");
            var tab = tabs.FindElementByName("Menu");
            tab.MovePointerOver();
        }

        [Collection("Default")]
        public class Default : MenuTests
        {
            public Default(DefaultAppFixture fixture) : base(fixture) { }

            [PlatformFact(TestPlatforms.Linux)]
            public void Linux_Click_Child()
            {
                using var fixture = new DefaultAppFixture();
                var isolated = new Default(fixture);
                isolated.ClickChildCore();
            }

            [PlatformFact(TestPlatforms.Linux)]
            public void Linux_Click_Grandchild()
            {
                using var fixture = new DefaultAppFixture();
                var isolated = new Default(fixture);
                isolated.ClickGrandchildCore();
            }

            [PlatformFact(TestPlatforms.Linux)]
            public void Linux_Child_AcceleratorKey()
            {
                using var fixture = new DefaultAppFixture();
                var isolated = new Default(fixture);
                isolated.ChildAcceleratorKeyCore();
            }

            [PlatformFact(TestPlatforms.Linux)]
            public void Linux_Select_Child_With_Click_Arrow_Keys()
            {
                using var fixture = new DefaultAppFixture();
                var isolated = new Default(fixture);
                isolated.Select_Child_With_Click_Arrow_Keys();
            }

            [PlatformFact(TestPlatforms.Linux)]
            public void Linux_Select_Grandchild_With_Click_Arrow_Keys()
            {
                using var fixture = new DefaultAppFixture();
                var isolated = new Default(fixture);
                isolated.Select_Grandchild_With_Click_Arrow_Keys();
            }

            [PlatformFact(TestPlatforms.Linux)]
            public void Linux_PointerOver_Does_Not_Steal_Focus()
            {
                using var fixture = new DefaultAppFixture();
                var isolated = new Default(fixture);
                isolated.PointerOver_Does_Not_Steal_Focus();
            }

        }

        [Collection("OverlayPopups")]
        public class OverlayPopups : MenuTests
        {
            public OverlayPopups(OverlayPopupsAppFixture fixture) : base(fixture) { }

            [PlatformFact(TestPlatforms.Linux)]
            public void Linux_Click_Child()
            {
                using var fixture = new OverlayPopupsAppFixture();
                var isolated = new OverlayPopups(fixture);
                isolated.ClickChildCore();
            }

            [PlatformFact(TestPlatforms.Linux)]
            public void Linux_Click_Grandchild()
            {
                using var fixture = new OverlayPopupsAppFixture();
                var isolated = new OverlayPopups(fixture);
                isolated.ClickGrandchildCore();
            }

            [PlatformFact(TestPlatforms.Linux)]
            public void Linux_Child_AcceleratorKey()
            {
                using var fixture = new OverlayPopupsAppFixture();
                var isolated = new OverlayPopups(fixture);
                isolated.ChildAcceleratorKeyCore();
            }

            [PlatformFact(TestPlatforms.Linux)]
            public void Linux_Select_Child_With_Click_Arrow_Keys()
            {
                using var fixture = new OverlayPopupsAppFixture();
                var isolated = new OverlayPopups(fixture);
                isolated.Select_Child_With_Click_Arrow_Keys();
            }

            [PlatformFact(TestPlatforms.Linux)]
            public void Linux_Select_Grandchild_With_Click_Arrow_Keys()
            {
                using var fixture = new OverlayPopupsAppFixture();
                var isolated = new OverlayPopups(fixture);
                isolated.Select_Grandchild_With_Click_Arrow_Keys();
            }

            [PlatformFact(TestPlatforms.Linux)]
            public void Linux_PointerOver_Does_Not_Steal_Focus()
            {
                using var fixture = new OverlayPopupsAppFixture();
                var isolated = new OverlayPopups(fixture);
                isolated.PointerOver_Does_Not_Steal_Focus();
            }

        }
    }
}
