﻿using System;
using System.Globalization;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Interactions;
using Xunit;

namespace Avalonia.IntegrationTests.Appium
{
    [Collection("Default")]
    public class SliderTests : TestBase
    {
        public SliderTests(DefaultAppFixture fixture)
            : base(fixture, "Slider")
        {
            var reset = Session.FindElementByAccessibilityId("ResetSliders");
            reset.Click();
        }

        [Fact(Skip = "Flaky test, slider value is sometimes off by 1 or 2 steps.")]
        public void Horizontal_Changes_Value_Dragging_Thumb_Right()
        {
            var slider = Session.FindElementByAccessibilityId("HorizontalSlider");
            var thumb = slider.FindElementByAccessibilityId("thumb");
            var initialThumbRect = thumb.Rect;

            new Actions(Session).ClickAndHold(thumb).MoveByOffset(100, 0).Release().Perform();

            var value = double.Parse(slider.Text, CultureInfo.InvariantCulture);
            var boundValue = double.Parse(
                Session.FindElementByAccessibilityId("HorizontalSliderValue").Text,
                CultureInfo.InvariantCulture);

            Assert.True(value > 50);
            Assert.True(Math.Abs(value - boundValue) < 2.0, $"Expected: {value}, Actual: {boundValue}");

            var currentThumbRect = thumb.Rect;
            Assert.True(currentThumbRect.Left > initialThumbRect.Left);
        }

        [Fact(Skip = "Flaky test, slider value is sometimes off by 1 or 2 steps.")]
        public void Horizontal_Changes_Value_Dragging_Thumb_Left()
        {
            var slider = Session.FindElementByAccessibilityId("HorizontalSlider");
            var thumb = slider.FindElementByAccessibilityId("thumb");
            var initialThumbRect = thumb.Rect;

            new Actions(Session).ClickAndHold(thumb).MoveByOffset(-100, 0).Release().Perform();

            var value = double.Parse(slider.Text, CultureInfo.InvariantCulture);
            var boundValue = double.Parse(
                Session.FindElementByAccessibilityId("HorizontalSliderValue").Text,
                CultureInfo.InvariantCulture);

            Assert.True(value < 50);
            Assert.True(Math.Abs(value - boundValue) < 2.0, $"Expected: {value}, Actual: {boundValue}");

            var currentThumbRect = thumb.Rect;
            Assert.True(currentThumbRect.Left < initialThumbRect.Left);
        }

        [Fact]
        public void Horizontal_Changes_Value_When_Clicking_Increase_Button()
        {
            var slider = Session.FindElementByAccessibilityId("HorizontalSlider");
            var thumb = slider.FindElementByAccessibilityId("thumb");
            var initialThumbRect = thumb.Rect;

            new Actions(Session).MoveToElementCenter(slider, 100, 0).Click().Perform();

            var value = double.Parse(slider.Text, CultureInfo.InvariantCulture);
            var boundValue = double.Parse(
                Session.FindElementByAccessibilityId("HorizontalSliderValue").Text,
                CultureInfo.InvariantCulture);

            Assert.True(value > 50);
            Assert.True(Math.Abs(value - boundValue) < 2.0, $"Expected: {value}, Actual: {boundValue}");

            var currentThumbRect = thumb.Rect;
            Assert.True(currentThumbRect.Left > initialThumbRect.Left);
        }

        [Fact]
        public void Horizontal_Changes_Value_When_Clicking_Decrease_Button()
        {
            var slider = Session.FindElementByAccessibilityId("HorizontalSlider");
            var thumb = slider.FindElementByAccessibilityId("thumb");
            var initialThumbRect = thumb.Rect;

            new Actions(Session).MoveToElementCenter(slider, -100, 0).Click().Perform();

            var value = double.Parse(slider.Text, CultureInfo.InvariantCulture);
            var boundValue = double.Parse(
                Session.FindElementByAccessibilityId("HorizontalSliderValue").Text,
                CultureInfo.InvariantCulture);

            Assert.True(value < 50);
            Assert.True(Math.Abs(value - boundValue) < 2.0, $"Expected: {value}, Actual: {boundValue}");

            var currentThumbRect = thumb.Rect;
            Assert.True(currentThumbRect.Left < initialThumbRect.Left);
        }
    }
}
