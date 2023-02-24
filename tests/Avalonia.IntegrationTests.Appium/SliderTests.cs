using System;
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
            var tab = tabs.FindElementByName("SliderTab");
            tab.Click();
        }

        [Fact]
        public void Changes_Value_When_Clicking_Increase_Button()
        {
            var slider = _session.FindElementByAccessibilityId("Slider");

            // slider.Text gets the Slider value
            Assert.True(double.Parse(slider.Text) == 30);

            new Actions(_session).Click(slider).Perform();

            Assert.Equal(50, Math.Round(double.Parse(slider.Text)));
        }
    }
}
