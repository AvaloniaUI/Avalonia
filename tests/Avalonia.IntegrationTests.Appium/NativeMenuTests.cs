using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using Xunit;
using static Avalonia.IntegrationTests.Appium.TestAppFixture;

namespace Avalonia.IntegrationTests.Appium
{
    [Collection("Default")]
    public class NativeMenuTests
    {
        private readonly AvaloniaWebDriver _session;

        public NativeMenuTests(TestAppFixture fixture)
        {
            _session = fixture.Session;

            var tabs = _session.FindElementByAccessibilityId("MainTabs");
            var tab = tabs.FindElement(By.Name("Automation"));
            tab.Click();
        }

        [PlatformFact(SkipOnWindows = true)]
        public void View_Menu_Select_Button_Tab()
        {
            var tabs = _session.FindElementByAccessibilityId("MainTabs");
            var buttonTab = tabs.FindElement(By.Name("Button"));
            var menuBar = _session.FindElementByXPath("/XCUIElementTypeApplication/XCUIElementTypeMenuBar");
            var viewMenu = menuBar.FindElement(By.Name("View"));
            
            Assert.False(buttonTab.Selected);
            
            viewMenu.Click();
            var buttonMenu = viewMenu.FindElement(By.Name("Button"));
            buttonMenu.Click();

            Assert.True(buttonTab.Selected);
        }
    }
}
