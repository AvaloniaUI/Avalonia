using Xunit;

namespace Avalonia.IntegrationTests.Appium
{
    [Collection("Default")]
    public class ButtonTests : TestBase
    {
        public ButtonTests(DefaultAppFixture fixture)
            : base(fixture, "Button")
        {
        }

        [Fact]
        public void DisabledButton()
        {
            var button = Session.FindElementByAccessibilityId("DisabledButton");

            Assert.Equal("Disabled Button", button.Text);
            Assert.False(button.Enabled);
        }

        [Fact]
        public void EffectivelyDisabledButton()
        {
            var button = Session.FindElementByAccessibilityId("EffectivelyDisabledButton");

            Assert.Equal("Effectively Disabled Button", button.Text);
            Assert.False(button.Enabled);
        }

        [Fact]
        public void BasicButton()
        {
            var button = Session.FindElementByAccessibilityId("BasicButton");

            Assert.Equal("Basic Button", button.Text);
            Assert.True(button.Enabled);
        }

        [Fact]
        public void ButtonWithTextBlock()
        {
            var button = Session.FindElementByAccessibilityId("ButtonWithTextBlock");

            Assert.Equal("Button with TextBlock", button.Text);
        }

        [PlatformFact(TestPlatforms.Windows)]
        public void ButtonWithAcceleratorKey()
        {
            var button = Session.FindElementByAccessibilityId("ButtonWithAcceleratorKey");

            Assert.Equal("Ctrl+B", button.GetAttribute("AcceleratorKey"));
        }
    }
}
