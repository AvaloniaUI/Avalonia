using System.Runtime.InteropServices;
using OpenQA.Selenium.Appium;
using Xunit;

namespace Avalonia.IntegrationTests.Appium
{
    [Collection("Default")]
    public class ButtonTests
    {
        private readonly AppiumDriver<AppiumWebElement> _session;

        public ButtonTests(DefaultAppFixture fixture)
        {
            _session = fixture.Session;

            var tabs = _session.FindElementByAccessibilityId("MainTabs");
            var tab = tabs.FindElementByName("Button");
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
        public void EffectivelyDisabledButton()
        {
            var button = _session.FindElementByAccessibilityId("EffectivelyDisabledButton");

            Assert.Equal("Effectively Disabled Button", button.Text);
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
        public void ButtonWithTextBlock()
        {
            var button = _session.FindElementByAccessibilityId("ButtonWithTextBlock");

            Assert.Equal("Button with TextBlock", button.Text);
        }

        [PlatformFact(TestPlatforms.Windows)]
        public void ButtonWithAcceleratorKey()
        {
            var button = _session.FindElementByAccessibilityId("ButtonWithAcceleratorKey");

            Assert.Equal("Ctrl+B", button.GetAttribute("AcceleratorKey"));
        }
    }
}
