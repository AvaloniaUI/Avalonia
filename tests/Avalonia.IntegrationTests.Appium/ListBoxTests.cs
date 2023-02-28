using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Interactions;
using Xunit;

namespace Avalonia.IntegrationTests.Appium
{
    [Collection("Default")]
    public class ListBoxTests
    {
        private readonly AppiumDriver<AppiumWebElement> _session;

        public ListBoxTests(DefaultAppFixture fixture)
        {
            _session = fixture.Session;

            var tabs = _session.FindElementByAccessibilityId("MainTabs");
            var tab = tabs.FindElementByName("ListBox");
            tab.Click();
        }

        [Fact]
        public void Can_Select_Item_By_Clicking()
        {
            var listBox = GetTarget();
            var item2 = listBox.FindElementByName("Item 2");
            var item4 = listBox.FindElementByName("Item 4");

            Assert.False(item2.Selected);
            Assert.False(item4.Selected);

            item2.SendClick();
            Assert.True(item2.Selected);
            Assert.False(item4.Selected);

            item4.SendClick();
            Assert.False(item2.Selected);
            Assert.True(item4.Selected);
        }

        [Fact(Skip = "WinAppDriver seems unable to consistently send a Ctrl key and appium-mac2-driver just hangs")]
        public void Can_Select_Items_By_Ctrl_Clicking()
        {
            var listBox = GetTarget();
            var item2 = listBox.FindElementByName("Item 2");
            var item4 = listBox.FindElementByName("Item 4");

            Assert.False(item2.Selected);
            Assert.False(item4.Selected);
            
            new Actions(_session)
                .Click(item2)
                .KeyDown(Keys.Control)
                .Click(item4)
                .KeyUp(Keys.Control)
                .Perform();

            Assert.True(item2.Selected);
            Assert.True(item4.Selected);
        }

        // appium-mac2-driver just hangs
        [PlatformFact(TestPlatforms.Windows)]
        public void Can_Select_Range_By_Shift_Clicking()
        {
            var listBox = GetTarget();
            var item2 = listBox.FindElementByName("Item 2");
            var item3 = listBox.FindElementByName("Item 3");
            var item4 = listBox.FindElementByName("Item 4");

            Assert.False(item2.Selected);
            Assert.False(item3.Selected);
            Assert.False(item4.Selected);

            new Actions(_session)
                .Click(item2)
                .KeyDown(Keys.Shift)
                .Click(item4)
                .KeyUp(Keys.Shift)
                .Perform();

            Assert.True(item2.Selected);
            Assert.True(item3.Selected);
            Assert.True(item4.Selected);
        }

        [Fact]
        public void Is_Virtualized()
        {
            var listBox = GetTarget();
            var children = listBox.GetChildren();

            Assert.True(children.Count < 100);
        }

        private AppiumWebElement GetTarget()
        {
            _session.FindElementByAccessibilityId("ListBoxSelectionClear").Click();
            return _session.FindElementByAccessibilityId("BasicListBox");
        }
    }
}
