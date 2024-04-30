using System;
using System.Globalization;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Interactions;
using Xunit;

namespace Avalonia.IntegrationTests.Appium
{
    [Collection("Default")]
    public class SliderTests
    {
        private readonly AppiumDriver<AppiumWebElement> _session;

        public SliderTests(DefaultAppFixture fixture)
        {
            _session = fixture.Session;

            var tabs = _session.FindElementByAccessibilityId("MainTabs");
            var tab = tabs.FindElementByName("Slider");
            tab.Click();

            var reset = _session.FindElementByAccessibilityId("ResetSliders");
            reset.Click();
        }

        [Fact]
        public void Horizontal_Changes_Value_Dragging_Thumb_Right()
        {
            var slider = _session.FindElementByAccessibilityId("HorizontalSlider");
            var thumb = slider.FindElementByAccessibilityId("thumb");
            var initialThumbRect = thumb.Rect;

            new Actions(_session).ClickAndHold(thumb).MoveByOffset(100, 0).Release().Perform();

            var value = Math.Round(double.Parse(slider.Text, CultureInfo.InvariantCulture));
            var boundValue = double.Parse(
                _session.FindElementByAccessibilityId("HorizontalSliderValue").Text,
                CultureInfo.InvariantCulture);

            Assert.True(value > 50);
            Assert.Equal(value, boundValue);

            var currentThumbRect = thumb.Rect;
            Assert.True(currentThumbRect.Left > initialThumbRect.Left);
        }

        [Fact]
        public void Horizontal_Changes_Value_Dragging_Thumb_Left()
        {
            var slider = _session.FindElementByAccessibilityId("HorizontalSlider");
            var thumb = slider.FindElementByAccessibilityId("thumb");
            var initialThumbRect = thumb.Rect;

            new Actions(_session).ClickAndHold(thumb).MoveByOffset(-100, 0).Release().Perform();

            var value = Math.Round(double.Parse(slider.Text, CultureInfo.InvariantCulture));
            var boundValue = double.Parse(
                _session.FindElementByAccessibilityId("HorizontalSliderValue").Text,
                CultureInfo.InvariantCulture);

            Assert.True(value < 50);
            Assert.Equal(value, boundValue);

            var currentThumbRect = thumb.Rect;
            Assert.True(currentThumbRect.Left < initialThumbRect.Left);
        }

        [Fact]
        public void Horizontal_Changes_Value_When_Clicking_Increase_Button()
        {
            var slider = _session.FindElementByAccessibilityId("HorizontalSlider");
            var thumb = slider.FindElementByAccessibilityId("thumb");
            var initialThumbRect = thumb.Rect;

            new Actions(_session).MoveToElement(slider, 100, 0, MoveToElementOffsetOrigin.Center).Click().Perform();

            var value = Math.Round(double.Parse(slider.Text, CultureInfo.InvariantCulture));
            var boundValue = double.Parse(
                _session.FindElementByAccessibilityId("HorizontalSliderValue").Text,
                CultureInfo.InvariantCulture);

            Assert.True(value > 50);
            Assert.Equal(value, boundValue);

            var currentThumbRect = thumb.Rect;
            Assert.True(currentThumbRect.Left > initialThumbRect.Left);
        }

        [Fact]
        public void Horizontal_Changes_Value_When_Clicking_Decrease_Button()
        {
            var slider = _session.FindElementByAccessibilityId("HorizontalSlider");
            var thumb = slider.FindElementByAccessibilityId("thumb");
            var initialThumbRect = thumb.Rect;

            new Actions(_session).MoveToElement(slider, -100, 0, MoveToElementOffsetOrigin.Center).Click().Perform();

            var value = Math.Round(double.Parse(slider.Text, CultureInfo.InvariantCulture));
            var boundValue = double.Parse(
                _session.FindElementByAccessibilityId("HorizontalSliderValue").Text,
                CultureInfo.InvariantCulture);

            Assert.True(value < 50);
            Assert.Equal(value, boundValue);

            var currentThumbRect = thumb.Rect;
            Assert.True(currentThumbRect.Left < initialThumbRect.Left);
        }
    }
}
