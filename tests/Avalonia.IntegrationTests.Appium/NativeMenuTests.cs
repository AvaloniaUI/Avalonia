using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using Xunit;

namespace Avalonia.IntegrationTests.Appium
{
    [Collection("Default")]
    public class NativeMenuTests
    {
        private readonly AppiumDriver _session;

        public NativeMenuTests(TestAppFixture fixture)
        {
            _session = fixture.Session;

            var tabs = _session.FindElement(MobileBy.AccessibilityId("MainTabs"));
            var tab = tabs.FindElement(MobileBy.Name("Automation"));
            tab.Click();
        }

        [PlatformFact(TestPlatforms.MacOS)]
        public void View_Menu_Select_Button_Tab()
        {
            var tabs = _session.FindElement(MobileBy.AccessibilityId("MainTabs"));
            var buttonTab = tabs.FindElement(MobileBy.Name("Button"));
            var menuBar = _session.FindElement(By.XPath("/XCUIElementTypeApplication/XCUIElementTypeMenuBar"));
            var viewMenu = menuBar.FindElement(MobileBy.Name("View"));
            
            Assert.False(buttonTab.Selected);
            
            viewMenu.Click();
            var buttonMenu = viewMenu.FindElement(MobileBy.Name("Button"));
            buttonMenu.Click();

            Assert.True(buttonTab.Selected);
        }
    }
}
