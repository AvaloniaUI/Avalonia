using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using Xunit;

namespace Avalonia.IntegrationTests.Appium
{
    [Collection("Default")]
    public class CheckBoxTests
    {
        private readonly AppiumDriver _session;

        public CheckBoxTests(DefaultAppFixture fixture)
        {
            _session = fixture.Session;

            var tabs = _session.FindElement(MobileBy.AccessibilityId("MainTabs"));
            var tab = tabs.FindElement(MobileBy.Name("CheckBox"));
            tab.Click();
        }

        [Fact]
        public void UncheckedCheckBox()
        {
            var checkBox = _session.FindElement(MobileBy.AccessibilityId("UncheckedCheckBox"));

            Assert.Equal("Unchecked", checkBox.GetName());
            Assert.Equal(false, checkBox.GetIsChecked());

            checkBox.Click();
            Assert.Equal(true, checkBox.GetIsChecked());
        }

        [Fact]
        public void CheckedCheckBox()
        {
            var checkBox = _session.FindElement(MobileBy.AccessibilityId("CheckedCheckBox"));

            Assert.Equal("Checked", checkBox.GetName());
            Assert.Equal(true, checkBox.GetIsChecked());

            checkBox.Click();
            Assert.Equal(false, checkBox.GetIsChecked());
        }

        [Fact]
        public void ThreeStateCheckBox()
        {
            var checkBox = _session.FindElement(MobileBy.AccessibilityId("ThreeStateCheckBox"));

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
