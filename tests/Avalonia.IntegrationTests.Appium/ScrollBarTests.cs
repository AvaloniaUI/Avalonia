using OpenQA.Selenium.Appium;
using Xunit;

namespace Avalonia.IntegrationTests.Appium
{
    [Collection("Default")]
    public class ScrollBarTests : TestBase
    {
        public ScrollBarTests(DefaultAppFixture fixture)
            : base(fixture, "ScrollBar")
        {
        }

        [Fact]
        public void ScrollBar_Increases_Value_By_LargeChange_When_IncreaseButton_Is_Clicked()
        {
            var button = Session.FindElementByAccessibilityId("MyScrollBar");
            Assert.True(double.Parse(button.Text) == 20);

            button.Click();

            // Default LargeChange value is 10 so when clicking the IncreaseButton
            // ScrollBar value should be increased by 10.
            Assert.Equal(30, double.Parse(button.Text));
        }
    }
}
