using OpenQA.Selenium.Appium.Windows;
using Xunit;

namespace Avalonia.IntegrationTests.Win32
{
    [Collection("IntegrationTestApp collection")]
    public class ComboBoxTests
    {
        private WindowsDriver<WindowsElement> _session;
        public ComboBoxTests(TestAppFixture fixture) => _session = fixture.Session;

        [Fact]
        public void UnselectedComboBox()
        {
            SelectTab();

            var comboBox = _session.FindElementByAccessibilityId("UnselectedComboBox");

            Assert.Equal(string.Empty, comboBox.Text);

            comboBox.Click();
            comboBox.FindElementByName("Bar").Click();

            Assert.Equal("Bar", comboBox.Text);
        }

        [Fact]
        public void SelectedIndex0ComboBox()
        {
            SelectTab();

            var comboBox = _session.FindElementByAccessibilityId("SelectedIndex0ComboBox");

            Assert.Equal("Foo", comboBox.Text);
        }

        [Fact]
        public void SelectedIndex1ComboBox()
        {
            SelectTab();

            var comboBox = _session.FindElementByAccessibilityId("SelectedIndex1ComboBox");

            Assert.Equal("Bar", comboBox.Text);
        }

        private WindowsElement SelectTab()
        {
            var tabs = _session.FindElementByAccessibilityId("MainTabs");
            var tab = tabs.FindElementByName("ComboBox");
            tab.Click();
            return (WindowsElement)tab;
        }
    }
}
