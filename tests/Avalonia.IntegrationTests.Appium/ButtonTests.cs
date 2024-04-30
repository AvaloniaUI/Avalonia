using System.Runtime.InteropServices;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using Xunit;

namespace Avalonia.IntegrationTests.Appium
{
    [Collection("Default")]
    public class ButtonTests
    {
        private readonly AppiumDriver _session;

        public ButtonTests(DefaultAppFixture fixture)
        {
            _session = fixture.Session;

            var tabs = _session.FindElement(MobileBy.AccessibilityId("MainTabs"));
            var tab = tabs.FindElement(MobileBy.Name("Button"));
            tab.Click();
        }

        [Fact]
        public void DisabledButton()
        {
            var button = _session.FindElement(MobileBy.AccessibilityId("DisabledButton"));

            Assert.Equal("Disabled Button", button.Text);
            Assert.False(button.Enabled);
        }

        [Fact]
        public void EffectivelyDisabledButton()
        {
            var button = _session.FindElement(MobileBy.AccessibilityId("EffectivelyDisabledButton"));

            Assert.Equal("Effectively Disabled Button", button.Text);
            Assert.False(button.Enabled);
        }

        [Fact]
        public void BasicButton()
        {
            var button = _session.FindElement(MobileBy.AccessibilityId("BasicButton"));

            Assert.Equal("Basic Button", button.Text);
            Assert.True(button.Enabled);
        }

        [Fact]
        public void ButtonWithTextBlock()
        {
            var button = _session.FindElement(MobileBy.AccessibilityId("ButtonWithTextBlock"));

            Assert.Equal("Button with TextBlock", button.Text);
        }

        [PlatformFact(TestPlatforms.Windows)]
        public void ButtonWithAcceleratorKey()
        {
            var button = _session.FindElement(MobileBy.AccessibilityId("ButtonWithAcceleratorKey"));

            Assert.Equal("Ctrl+B", button.GetAttribute("AcceleratorKey"));
        }
    }
}
