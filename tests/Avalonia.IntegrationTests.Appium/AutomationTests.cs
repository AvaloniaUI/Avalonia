using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using Xunit;

namespace Avalonia.IntegrationTests.Appium
{
    [Collection("Default")]
    public class AutomationTests
    {
        private readonly AppiumDriver _session;

        public AutomationTests(DefaultAppFixture fixture)
        {
            _session = fixture.Session;

            var tabs = _session.FindElement(MobileBy.AccessibilityId("MainTabs"));
            var tab = tabs.FindElement(MobileBy.Name("Automation"));
            tab.Click();
        }

        [Fact]
        public void AutomationId()
        {
            // AutomationID can be specified by the Name or AutomationProperties.AutomationId
            // properties, with the latter taking precedence.
            var byName = _session.FindElement(MobileBy.AccessibilityId("TextBlockWithName"));
            var byAutomationId = _session.FindElement(MobileBy.AccessibilityId("TextBlockWithNameAndAutomationId"));
        }

        [Fact]
        public void LabeledBy()
        {
            var label = _session.FindElement(MobileBy.AccessibilityId("TextBlockAsLabel"));
            var labeledTextBox = _session.FindElement(MobileBy.AccessibilityId("LabeledByTextBox"));

            Assert.Equal("Label for TextBox", label.Text);
            Assert.Equal("Label for TextBox", labeledTextBox.GetName());
        }
    }
}
