
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using Xunit;
using static Avalonia.IntegrationTests.Appium.TestAppFixture;

namespace Avalonia.IntegrationTests.Appium
{
    [Collection("Default")]
    public class MenuTests
    {
        private readonly AvaloniaWebDriver _session;

        public MenuTests(TestAppFixture fixture)
        {
            _session = fixture.Session;

            var tabs = _session.FindElementByAccessibilityId("MainTabs");
            var tab = tabs.FindElement(By.Name("Menu"));
            tab.Click();
        }

        [Fact]
        public void Click_Child()
        {
            var rootMenuItem = _session.FindElementByAccessibilityId("RootMenuItem");
            
            rootMenuItem.SendClick();

            var childMenuItem = _session.FindElementByAccessibilityId("Child1MenuItem");
            childMenuItem.SendClick();

            var clickedMenuItem = _session.FindElementByAccessibilityId("ClickedMenuItem");
            Assert.Equal("_Child 1", clickedMenuItem.Text);
        }

        [Fact]
        public void Click_Grandchild()
        {
            var rootMenuItem = _session.FindElementByAccessibilityId("RootMenuItem");
            
            rootMenuItem.SendClick();

            var childMenuItem = _session.FindElementByAccessibilityId("Child2MenuItem");
            childMenuItem.SendClick();

            var grandchildMenuItem = _session.FindElementByAccessibilityId("GrandchildMenuItem");
            grandchildMenuItem.SendClick();

            var clickedMenuItem = _session.FindElementByAccessibilityId("ClickedMenuItem");
            Assert.Equal("_Grandchild", clickedMenuItem.Text);
        }

        [PlatformFact(SkipOnOSX = true)]
        public void Child_AcceleratorKey()
        {
            var rootMenuItem = _session.FindElementByAccessibilityId("RootMenuItem");
            
            rootMenuItem.SendClick();

            var childMenuItem = _session.FindElementByName("_Child 1");

            Assert.Equal("Ctrl+O", childMenuItem.GetAttribute("AcceleratorKey"));
        }
    }
}
