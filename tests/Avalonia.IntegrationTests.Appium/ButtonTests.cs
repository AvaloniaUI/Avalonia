using System.Reflection;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using Xunit;
using static Avalonia.IntegrationTests.Appium.TestAppFixture;

namespace Avalonia.IntegrationTests.Appium
{
    [Collection("Default")]
    public class ButtonTests
    {
        private readonly AvaloniaWebDriver _session;

        public ButtonTests(TestAppFixture fixture)
        {
            _session = fixture.Session;
            var tabs = _session.FindElementByAccessibilityId("MainTabs");
            var tab = tabs.FindElement(By.Name("Button"));
            tab.Click();
        }

        [Fact]
        public void DisabledButton()
        {
            var button = _session.FindElementByAccessibilityId("DisabledButton");

            Assert.Equal("Disabled Button", button.Text);
            Assert.False(button.Enabled);
        }

        [Fact]
        public void BasicButton()
        {
            var button = _session.FindElementByAccessibilityId("BasicButton");

            Assert.Equal("Basic Button", button.Text);
            Assert.True(button.Enabled);
        }

        [Fact]
        public void RepeatButton()
        {
            var button = _session.FindElementByAccessibilityId("RepeatButton");
            button.Click();
            Assert.Equal("Repeat Button: 1", button.Text);
            button.Click();
            Assert.Equal("Repeat Button: 2", button.Text);
        }

        [Fact]
        public void ButtonWithTextBlock()
        {
            var button = _session.FindElementByAccessibilityId("ButtonWithTextBlock");

            Assert.Equal("Button with TextBlock", button.Text);
        }

        [PlatformFact(SkipOnOSX = true)]
        public void ButtonWithAcceleratorKey()
        {
            var button = _session.FindElementByAccessibilityId("ButtonWithAcceleratorKey");

            Assert.Equal("Ctrl+B", button.GetAttribute("AcceleratorKey"));
        }
    }
}
