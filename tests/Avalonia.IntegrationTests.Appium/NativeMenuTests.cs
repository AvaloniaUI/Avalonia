using OpenQA.Selenium.Appium;
using Xunit;

namespace Avalonia.IntegrationTests.Appium
{
    [Collection("Default")]
    public class NativeMenuTests
    {
        private readonly AppiumDriver<AppiumWebElement> _driver;

        public NativeMenuTests(DefaultAppFixture fixture)
        {
            _driver = fixture.Driver;

            var tabs = _driver.FindElementByAccessibilityId("MainTabs");
            var tab = tabs.FindElementByName("Automation");
            tab.Click();
        }

        [PlatformFact(TestPlatforms.MacOS)]
        public void View_Menu_Select_Button_Tab()
        {
            var tabs = _driver.FindElementByAccessibilityId("MainTabs");
            var buttonTab = tabs.FindElementByName("Button");
            var menuBar = _driver.FindElementByXPath("/XCUIElementTypeApplication/XCUIElementTypeMenuBar");
            var viewMenu = menuBar.FindElementByName("View");
            
            Assert.False(buttonTab.Selected);
            
            viewMenu.Click();
            var buttonMenu = viewMenu.FindElementByName("Button");
            buttonMenu.Click();

            Assert.True(buttonTab.Selected);
        }
    }
}
