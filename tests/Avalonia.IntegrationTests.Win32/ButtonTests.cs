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
        public void DisabledButton()
        {
            SelectTab();

            var button = _session.FindElementByAccessibilityId("DisabledButton");

            Assert.Equal("Disabled Button", button.Text);
            Assert.False(button.Enabled);
        }

        [Fact]
        public void BasicButton()
        {
            SelectTab();
            
            var button = _session.FindElementByAccessibilityId("BasicButton");

            Assert.Equal("Basic Button", button.Text);
            Assert.True(button.Enabled);
        }

        [Fact]
        public void ButtonWithTextBlock()
        {
            SelectTab();

            var button = _session.FindElementByAccessibilityId("ButtonWithTextBlock");

            Assert.Equal("Button with TextBlock", button.Text);
        }

        private WindowsElement SelectTab()
        {
            var tabs = _session.FindElementByAccessibilityId("MainTabs");
            var tab = tabs.FindElementByName("Button");
            tab.Click();
            return (WindowsElement)tab;
        }
    }
}
