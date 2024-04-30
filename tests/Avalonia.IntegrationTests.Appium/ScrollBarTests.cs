using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using Xunit;

namespace Avalonia.IntegrationTests.Appium
{
    [Collection("Default")]
    public class ScrollBarTests
    {
        private readonly AppiumDriver _session;

        public ScrollBarTests(DefaultAppFixture fixture)
        {
            _session = fixture.Session;

            var tabs = _session.FindElement(MobileBy.AccessibilityId("MainTabs"));
            var tab = tabs.FindElement(MobileBy.Name("ScrollBar"));
            tab.Click();
        }

        [Fact]
        public void ScrollBar_Increases_Value_By_LargeChange_When_IncreaseButton_Is_Clicked()
        {
            var button = _session.FindElement(MobileBy.AccessibilityId("MyScrollBar"));
            Assert.True(double.Parse(button.Text) == 20);

            button.Click();

            // Default LargeChange value is 10 so when clicking the IncreaseButton
            // ScrollBar value should be increased by 10.
            Assert.Equal(30, double.Parse(button.Text));
        }
    }
}
