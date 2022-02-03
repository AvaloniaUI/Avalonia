using System;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Remote;
using Xunit;
using static Avalonia.IntegrationTests.Appium.TestAppFixture;

namespace Avalonia.IntegrationTests.Appium
{
    [Collection("Default")]
    public class ContextMenuTests
    {
        private readonly AvaloniaWebDriver _session;

        public ContextMenuTests(TestAppFixture fixture)
        {
            _session = fixture.Session;

            var tabs = _session.FindElementByAccessibilityId("MainTabs");
            var tab = tabs.FindElement(By.Name("ContextMenu"));
            tab.Click();
        }

        [Fact]
        public void ContextMenu_Click()
        {
            var rootMenuItem = _session.FindElementByAccessibilityId("ContextMenuTb");
            rootMenuItem.Click();
            //new Actions(rootMenuItem.WrappedDriver).ContextClick().Perform();
        }
    }
}
