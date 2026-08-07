using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using Xunit;

namespace Avalonia.IntegrationTests.Appium
{
    [Collection("Default")]
    public class KeyboardTests : TestBase
    {
        public KeyboardTests(DefaultAppFixture fixture)
            : base(fixture, "Keyboard")
        {
            var reset = Session.FindElementByAccessibilityId("ResetKeyboard");
            reset.Click();
        }

        [Fact]
        public void KeyBinding_Without_Modifier_Is_Raised_While_TextBox_Is_Focused()
        {
            var textBox = Session.FindElementByAccessibilityId("GestureTextBox");
            var lastKeyBinding = Session.FindElementByAccessibilityId("LastKeyBinding");

            textBox.Click();
            new Actions(Session).SendKeys(Keys.Space).Perform();

            Assert.Equal("Space", lastKeyBinding.Text);
        }

        [Fact]
        public void KeyBinding_On_Character_Key_Is_Raised_While_TextBox_Is_Focused()
        {
            var textBox = Session.FindElementByAccessibilityId("GestureTextBox");
            var lastKeyBinding = Session.FindElementByAccessibilityId("LastKeyBinding");

            textBox.Click();
            new Actions(Session).SendKeys("a").Perform();

            Assert.Equal("A", lastKeyBinding.Text);
        }

        [Fact]
        public void KeyBinding_With_Modifier_Is_Raised_While_TextBox_Is_Focused()
        {
            var textBox = Session.FindElementByAccessibilityId("GestureTextBox");
            var lastKeyBinding = Session.FindElementByAccessibilityId("LastKeyBinding");

            textBox.Click();
            new Actions(Session)
                .KeyDown(Keys.Control)
                .SendKeys("g")
                .KeyUp(Keys.Control)
                .Perform();

            Assert.Equal("Ctrl+G", lastKeyBinding.Text);
        }

        [Fact]
        public void Handled_KeyBinding_Does_Not_Insert_Text()
        {
            var textBox = Session.FindElementByAccessibilityId("GestureTextBox");
            var content = Session.FindElementByAccessibilityId("GestureTextBoxContent");

            textBox.Click();
            new Actions(Session).SendKeys(Keys.Space).Perform();

            // A matched KeyBinding marks the key event as handled, which must prevent the
            // platform from producing text for it.
            Assert.Equal(string.Empty, content.Text);
        }

        [Fact]
        public void Unhandled_Key_Still_Produces_Text()
        {
            var textBox = Session.FindElementByAccessibilityId("KeyDownTextBox");
            var lastKeyDown = Session.FindElementByAccessibilityId("LastKeyDown");
            var lastTextInput = Session.FindElementByAccessibilityId("LastTextInput");

            textBox.Click();
            new Actions(Session).SendKeys("b").Perform();

            Assert.Equal("b", textBox.Text);
            Assert.Equal("B|B|[b]", lastKeyDown.Text);
            Assert.Equal("[b]", lastTextInput.Text);
        }

        [Fact]
        public void Unhandled_Space_Produces_Exactly_One_KeyDown_And_Text()
        {
            var textBox = Session.FindElementByAccessibilityId("KeyDownTextBox");
            var lastKeyDown = Session.FindElementByAccessibilityId("LastKeyDown");
            var keyDownCount = Session.FindElementByAccessibilityId("KeyDownCount");
            var lastTextInput = Session.FindElementByAccessibilityId("LastTextInput");

            textBox.Click();
            new Actions(Session).SendKeys(Keys.Space).Perform();

            // The input context must not produce a second key down for the same NSEvent.
            Assert.Equal("1", keyDownCount.Text);
            Assert.Equal("Space|Space|[ ]", lastKeyDown.Text);
            Assert.Equal("[ ]", lastTextInput.Text);
        }
    }
}
