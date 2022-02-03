using System;
using System.Threading.Tasks;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Interactions;
using Xunit;

namespace Avalonia.IntegrationTests.Appium
{
    [Collection("Default")]
    public class ContextMenuTests
    {
        private readonly AppiumDriver<AppiumWebElement> _session;

        public ContextMenuTests(TestAppFixture fixture)
        {
            _session = fixture.Session;

            var tabs = _session.FindElementByAccessibilityId("MainTabs");
            var tab = tabs.FindElementByName("ContextMenu");
            tab.Click();
        }

        [Fact]
        public void ContextMenu_Click()
        {
            var rootMenuItem = _session.FindElementByAccessibilityId("ContextMenuTb");
            rootMenuItem.Click();
            new Actions(rootMenuItem.WrappedDriver).ContextClick().Perform();
        }
    }
}
