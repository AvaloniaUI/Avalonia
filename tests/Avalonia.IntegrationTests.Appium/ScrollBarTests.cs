using OpenQA.Selenium.Appium;
using Xunit;

namespace Avalonia.IntegrationTests.Appium
{
    [Collection("Default")]
    public class ScrollBarTests
    {
        private readonly AppiumDriver<AppiumWebElement> _session;

        public ScrollBarTests(DefaultAppFixture fixture)
        {
            _session = fixture.Session;

            var tabs = _session.FindElementByAccessibilityId("MainTabs");
            var tab = tabs.FindElementByName("ScrollBar");
            tab.Click();
        }

        [Fact]
        public void ScrollBar_Increases_Value_By_LargeChange_When_IncreaseButton_Is_Clicked()
        {
            var button = _session.FindElementByAccessibilityId("MyScrollBar");
            Assert.True(double.Parse(button.Text) == 20);

            button.Click();

            // Default LargeChange value is 10 so when clicking the IncreaseButton
            // ScrollBar value should be increased by 10.
            Assert.Equal(30, double.Parse(button.Text));
        }
    }
}
