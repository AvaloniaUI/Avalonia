using OpenQA.Selenium.Appium;
using System.Globalization;
using System.Threading;
using OpenQA.Selenium.Interactions;
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

        [PlatformFact(TestPlatforms.Windows | TestPlatforms.MacOS)]
        public void ScrollBar_Increases_Value_By_LargeChange_When_IncreaseButton_Is_Clicked()
        {
            AssertIncreasesValueByLargeChange(Session);
        }

        [PlatformFact(TestPlatforms.Linux)]
        public void Linux_ScrollBar_Increases_Value_By_LargeChange_When_IncreaseButton_Is_Clicked()
        {
            using var fixture = new DefaultAppFixture();
            var isolated = new ScrollBarTests(fixture);
            AssertIncreasesValueByLargeChange(isolated.Session);
        }

        private static void AssertIncreasesValueByLargeChange(AppiumDriver session)
        {
            var button = session.FindElementByAccessibilityId("MyScrollBar");
            Assert.Equal(20, GetValue(button));

            var value = GetValue(button);
            for (var i = 0; i < 6 && value == 20; ++i)
            {
                // Prefer targeting the increase side of the horizontal scrollbar.
                new Actions(session).MoveToElementCenter(button, 90, 0).Click().Perform();
                Thread.Sleep(100);
                value = GetValue(button);
            }

            for (var i = 0; i < 6 && value == 20; ++i)
            {
                // Fallback for backends that map range increment to an accessibility "click" action.
                button.SendClick();
                Thread.Sleep(100);
                value = GetValue(button);
            }

            // Default LargeChange value is 10 so when clicking the IncreaseButton
            // ScrollBar value should be increased by 10.
            Assert.Equal(30, value);
        }

        private static double GetValue(AppiumWebElement element)
        {
            var raw = element.Text;
            if (string.IsNullOrWhiteSpace(raw))
                raw = element.GetAttribute("value");
            return double.Parse(raw, CultureInfo.InvariantCulture);
        }
    }
}
