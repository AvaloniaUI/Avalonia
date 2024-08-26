using Xunit;

namespace Avalonia.IntegrationTests.Appium
{
    [Collection("Default")]
    public class CheckBoxTests : TestBase
    {
        public CheckBoxTests(DefaultAppFixture fixture)
            : base(fixture, "CheckBox")
        {
        }

        [Fact]
        public void UncheckedCheckBox()
        {
            var checkBox = Session.FindElementByAccessibilityId("UncheckedCheckBox");

            Assert.Equal("Unchecked", checkBox.GetName());
            Assert.Equal(false, checkBox.GetIsChecked());

            checkBox.Click();
            Assert.Equal(true, checkBox.GetIsChecked());
        }

        [Fact]
        public void CheckedCheckBox()
        {
            var checkBox = Session.FindElementByAccessibilityId("CheckedCheckBox");

            Assert.Equal("Checked", checkBox.GetName());
            Assert.Equal(true, checkBox.GetIsChecked());

            checkBox.Click();
            Assert.Equal(false, checkBox.GetIsChecked());
        }

        [Fact]
        public void ThreeStateCheckBox()
        {
            var checkBox = Session.FindElementByAccessibilityId("ThreeStateCheckBox");

            Assert.Equal("ThreeState", checkBox.GetName());
            Assert.Null(checkBox.GetIsChecked());

            checkBox.Click();
            Assert.Equal(false, checkBox.GetIsChecked());

            checkBox.Click();
            Assert.Equal(true, checkBox.GetIsChecked());

            checkBox.Click();
            Assert.Null(checkBox.GetIsChecked());
        }
    }
}
