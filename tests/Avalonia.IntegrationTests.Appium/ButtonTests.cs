using System.Runtime.InteropServices;
using OpenQA.Selenium.Appium;
using Xunit;

namespace Avalonia.IntegrationTests.Appium
{
    [Collection("Default")]
    public class ButtonTests
    {
        private readonly AppiumDriver<AppiumWebElement> _driver;

        public ButtonTests(DefaultAppFixture fixture)
        {
            _driver = fixture.Driver;

            var tabs = _driver.FindElementByAccessibilityId("MainTabs");
            var tab = tabs.FindElementByName("Button");
            tab.Click();
        }

        [Fact]
        public void DisabledButton()
        {
            var button = _driver.FindElementByAccessibilityId("DisabledButton");

            Assert.Equal("Disabled Button", button.Text);
            Assert.False(button.Enabled);
        }

        [Fact]
        public void BasicButton()
        {
            var button = _driver.FindElementByAccessibilityId("BasicButton");

            Assert.Equal("Basic Button", button.Text);
            Assert.True(button.Enabled);
        }

        [Fact]
        public void ButtonWithTextBlock()
        {
            var button = _driver.FindElementByAccessibilityId("ButtonWithTextBlock");

            Assert.Equal("Button with TextBlock", button.Text);
        }

        [PlatformFact(TestPlatforms.Windows)]
        public void ButtonWithAcceleratorKey()
        {
            var button = _driver.FindElementByAccessibilityId("ButtonWithAcceleratorKey");

            Assert.Equal("Ctrl+B", button.GetAttribute("AcceleratorKey"));
        }
    }
}
