using OpenQA.Selenium.Appium;
using Xunit;

namespace Avalonia.IntegrationTests.Appium
{
    [Collection("Default")]
    public class AutomationTests
    {
        private readonly AppiumDriver<AppiumWebElement> _driver;

        public AutomationTests(DefaultAppFixture fixture)
        {
            _driver = fixture.Driver;

            var tabs = _driver.FindElementByAccessibilityId("MainTabs");
            var tab = tabs.FindElementByName("Automation");
            tab.Click();
        }

        [Fact]
        public void AutomationId()
        {
            // AutomationID can be specified by the Name or AutomationProperties.AutomationId
            // properties, with the latter taking precedence.
            var byName = _driver.FindElementByAccessibilityId("TextBlockWithName");
            var byAutomationId = _driver.FindElementByAccessibilityId("TextBlockWithNameAndAutomationId");
        }

        [Fact]
        public void LabeledBy()
        {
            var label = _driver.FindElementByAccessibilityId("TextBlockAsLabel");
            var labeledTextBox = _driver.FindElementByAccessibilityId("LabeledByTextBox");

            Assert.Equal("Label for TextBox", label.Text);
            Assert.Equal("Label for TextBox", labeledTextBox.GetName());
        }
    }
}
