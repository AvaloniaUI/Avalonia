using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Interactions;
using Xunit;

namespace Avalonia.IntegrationTests.Appium
{
    [Collection("Default")]
    public class ContextMenuTests
    {
        private readonly AppiumDriver<AppiumWebElement> _session;

        public ContextMenuTests(DefaultAppFixture fixture)
        {
            _session = fixture.Session;

            var tabs = _session.FindElementByAccessibilityId("MainTabs");
            var tab = tabs.FindElementByName("ContextMenu");
            tab.Click();
        }
        
        [PlatformFact(TestPlatforms.Windows)]
        public void Select_First_Item_With_Down_Arrow_Key()
        {
            var control = _session.FindElementByAccessibilityId("ShowContextMenu");

            new Actions(_session)
                .ContextClick(control)
                .SendKeys(Keys.ArrowDown)
                .Perform();

            var clickedMenuItem = _session.FindElementByAccessibilityId("ContextMenuItem1");
            Assert.True(clickedMenuItem.GetIsFocused());
        }
    }
}
