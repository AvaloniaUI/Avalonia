using Avalonia.IntegrationTests.Appium.Crapium;
using OpenQA.Selenium.Appium;
using Xunit;

namespace Avalonia.IntegrationTests.Appium
{
    [Collection("Default")]
    public class AutomationTests
    {
        private readonly ISession _session;
        private readonly IWindowElement _window;

        public AutomationTests(DefaultAppFixture fixture)
        {
            _session = fixture.Session;
            _window = _session.GetWindow("MainWindow");

            var tabs = _window.FindElement<ITabControlElement>("MainTabs");
            var tab = tabs.FindItemByName<IElement>("Automation");
            tab.Click();
        }

        [Fact]
        public void AutomationId()
        {
            // AutomationID can be specified by the Name or AutomationProperties.AutomationId
            // properties, with the latter taking precedence.
            var byName = _window.FindElementByName<IElement>("TextBlockWithName");
            var byAutomationId = _window.FindElement<IElement>("TextBlockWithNameAndAutomationId");
        }

        [Fact]
        public void LabeledBy()
        {
            var label = _window.FindElement<ITextElement>("TextBlockAsLabel");
            var labeledTextBox = _window.FindElement<ITextElement>("LabeledByTextBox");

            Assert.Equal("Label for TextBox", label.Text);
            Assert.Equal("Label for TextBox", labeledTextBox.Name);
        }
    }
}
