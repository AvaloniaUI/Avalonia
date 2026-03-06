using Xunit;

namespace Avalonia.IntegrationTests.Appium
{
    [Collection("Default")]
    public class AutomationTests : TestBase
    {
        public AutomationTests(DefaultAppFixture fixture)
            : base(fixture, "Automation")
        {
        }

        [Fact]
        public void AutomationId()
        {
            // AutomationID can be specified by the Name or AutomationProperties.AutomationId
            // properties, with the latter taking precedence.
            var byName = Session.FindElementByAccessibilityId("TextBlockWithName");
            var byAutomationId = Session.FindElementByAccessibilityId("TextBlockWithNameAndAutomationId");
        }

        [Fact]
        public void LabeledBy()
        {
            var label = Session.FindElementByAccessibilityId("TextBlockAsLabel");
            var labeledTextBox = Session.FindElementByAccessibilityId("LabeledByTextBox");

            Assert.Equal("Label for TextBox", label.Text);
            Assert.Equal("Label for TextBox", labeledTextBox.GetName());
        }
    }
}
