using OpenQA.Selenium.Appium.Windows;
using Xunit;

namespace Avalonia.IntegrationTests.Win32
{
    [Collection("IntegrationTestApp collection")]
    public class ButtonTests
    {
        private WindowsDriver<WindowsElement> _session;
        public ButtonTests(TestAppFixture fixture) => _session = fixture.Session;

        [Fact]
        public void BasicButton()
        {
            SelectButtonTab();
            
            var button = _session.FindElementByAccessibilityId("BasicButton");

            Assert.Equal("Basic Button", button.Text);
        }

        [Fact]
        public void ButtonWithTextBlock()
        {
            SelectButtonTab();

            var button = _session.FindElementByAccessibilityId("ButtonWithTextBlock");

            Assert.Equal("Button with TextBlock", button.Text);
        }

        private WindowsElement SelectButtonTab()
        {
            var tabs = _session.FindElementByAccessibilityId("MainTabs");
            var buttonTab = tabs.FindElementByName("Button");
            buttonTab.Click();
            return (WindowsElement)buttonTab;
        }
    }
}
