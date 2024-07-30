using System;
using System.Globalization;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Interactions;
using Xunit;
using Xunit.Abstractions;

namespace Avalonia.IntegrationTests.Appium
{
    [Collection("Default")]
    public class SliderTests
    {
        private readonly AppiumDriver _session;
        private readonly ITestOutputHelper _output;

        public SliderTests(DefaultAppFixture fixture, ITestOutputHelper output)
        {
            _session = fixture.Session;

            var tabs = _session.FindElementByAccessibilityId("MainTabs");
            var tab = tabs.FindElementByName("Slider");
            tab.Click();

            var reset = _session.FindElementByAccessibilityId("ResetSliders");
            reset.Click();
            _output = output;
        }

        [Fact(Skip = "Flaky test, slider value is sometimes off by 1 or 2 steps.")]
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

        [Fact(Skip = "Flaky test, slider value is sometimes off by 1 or 2 steps.")]
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

            new Actions(_session).MoveToElementCenter(slider, 100, 0).Click().Perform();

            _output.WriteLine(_session.PageSource);
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

            new Actions(_session).MoveToElementCenter(slider, -100, 0).Click().Perform();

            _output.WriteLine(_session.PageSource);
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
