using OpenQA.Selenium.Appium;
using Xunit;

namespace Avalonia.IntegrationTests.Appium
{
    [Collection("Default")]
    public class RadioButtonTests : TestBase
    {
        public RadioButtonTests(DefaultAppFixture fixture)
            : base(fixture, "RadioButton")
        {
        }

        [Fact]
        public void RadioButton_IsChecked_True_When_Clicked()
        {
            var button = Session.FindElementByAccessibilityId("BasicRadioButton");
            Assert.False(button.GetIsChecked());
            button.Click();
            Assert.True(button.GetIsChecked());
        }

        [Fact]
        public void ThreeState_RadioButton_IsChecked_False_When_Other_ThreeState_RadioButton_Checked()
        {
            var button1 = Session.FindElementByAccessibilityId("ThreeStatesRadioButton1");
            var button2 = Session.FindElementByAccessibilityId("ThreeStatesRadioButton2");
            Assert.True(button1.GetIsChecked());
            Assert.False(button2.GetIsChecked());
            button2.Click();
            Assert.False(button1.GetIsChecked());
            Assert.True(button2.GetIsChecked());
        }

    }
}
