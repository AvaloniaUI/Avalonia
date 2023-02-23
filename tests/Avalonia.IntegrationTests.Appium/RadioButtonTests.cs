using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OpenQA.Selenium.Appium;
using Xunit;

namespace Avalonia.IntegrationTests.Appium
{
    [Collection("Default")]
    public class RadioButtonTests
    {
        private readonly AppiumDriver<AppiumWebElement> _session;

        public RadioButtonTests(TestAppFixture fixture)
        {
            _session = fixture.Session;

            var tabs = _session.FindElementByAccessibilityId("MainTabs");
            tabs.FindElementByName("RadioButton").Click();
        }


        [Fact]
        public void RadioButton_IsChecked_True_When_Clicked()
        {
            var button = _session.FindElementByAccessibilityId("BasicRadioButton");
            Assert.False(button.GetIsChecked());
            button.Click();
            Assert.True(button.GetIsChecked());
        }

        [Fact]
        public void ThreeState_RadioButton_IsChecked_False_When_Other_ThreeState_RadioButton_Checked()
        {
            var button1 = _session.FindElementByAccessibilityId("ThreeStatesRadioButton1");
            var button2 = _session.FindElementByAccessibilityId("ThreeStatesRadioButton2");
            Assert.True(button1.GetIsChecked());
            Assert.False(button2.GetIsChecked());
            button2.Click();
            Assert.False(button1.GetIsChecked());
            Assert.True(button2.GetIsChecked());
        }

    }
}
