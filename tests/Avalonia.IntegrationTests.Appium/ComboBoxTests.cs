using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Mac;
using Xunit;

namespace Avalonia.IntegrationTests.Appium
{
    [Collection("Default")]
    public class ComboBoxTests
    {
        private readonly AppiumDriver<AppiumWebElement> _session;

        public ComboBoxTests(TestAppFixture fixture)
        {
            _session = fixture.Session;

            var tabs = _session.FindElementByAccessibilityId("MainTabs");
            var tab = tabs.FindElementByName("ComboBox");
            tab.Click();
        }

        [Fact]
        public void UnselectedComboBox()
        {
            var comboBox = _session.FindElementByAccessibilityId("UnselectedComboBox");

            Assert.Equal(string.Empty, comboBox.Text);

            ((MacElement)comboBox).Click();
            _session.FindElementByName("Bar").SendClick();

            Assert.Equal("Bar", comboBox.Text);
        }

        [Fact]
        public void SelectedIndex0ComboBox()
        {
            var comboBox = _session.FindElementByAccessibilityId("SelectedIndex0ComboBox");

            Assert.Equal("Foo", comboBox.Text);
        }

        [Fact]
        public void SelectedIndex1ComboBox()
        {
            var comboBox = _session.FindElementByAccessibilityId("SelectedIndex1ComboBox");

            Assert.Equal("Bar", comboBox.Text);
        }
    }
}
