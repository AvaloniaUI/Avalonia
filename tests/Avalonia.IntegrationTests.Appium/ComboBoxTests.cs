using OpenQA.Selenium;
using Xunit;

namespace Avalonia.IntegrationTests.Appium
{
    public abstract class ComboBoxTests : TestBase
    {
        public ComboBoxTests(DefaultAppFixture fixture)
            : base(fixture, "ComboBox")
        {
        }

        [Fact]
        public void Can_Change_Selection_Using_Mouse()
        {
            var comboBox = Session.FindElementByAccessibilityId("BasicComboBox");

            Session.FindElementByAccessibilityId("ComboBoxSelectFirst").Click();
            Assert.Equal("Item 0", comboBox.GetComboBoxValue());

            comboBox.Click();
            Session.FindElementByName("Item 1").SendClick();

            Assert.Equal("Item 1", comboBox.GetComboBoxValue());
        }

        [Fact]
        public void Can_Change_Selection_From_Unselected_Using_Mouse()
        {
            var comboBox = Session.FindElementByAccessibilityId("BasicComboBox");

            Session.FindElementByAccessibilityId("ComboBoxSelectionClear").Click();
            Assert.Equal(string.Empty, comboBox.GetComboBoxValue());

            comboBox.Click();
            Session.FindElementByName("Item 0").SendClick();

            Assert.Equal("Item 0", comboBox.GetComboBoxValue());
        }

        [PlatformFact(TestPlatforms.Windows)]
        public void Can_Change_Selection_With_Keyboard_When_Closed()
        {
            CanChangeSelectionWithKeyboardWhenClosedCore();
        }

        protected void CanChangeSelectionWithKeyboardWhenClosedCore()
        {
            CanChangeSelectionWithKeyboardWhenClosedCore(Session);
        }

        private static void CanChangeSelectionWithKeyboardWhenClosedCore(AppiumDriver session)
        {
            var comboBox = session.FindElementByAccessibilityId("BasicComboBox");
            var wrap = session.FindElementByAccessibilityId("ComboBoxWrapSelection");

            if (wrap.GetIsChecked() != false)
                wrap.Click();

            session.FindElementByAccessibilityId("ComboBoxSelectionClear").Click();

            comboBox.SendKeys(Keys.ArrowDown);
            Assert.Equal("Item 0", comboBox.GetComboBoxValue());

            comboBox.SendKeys(Keys.ArrowDown);
            Assert.Equal("Item 1", comboBox.GetComboBoxValue());

            comboBox.SendKeys(Keys.ArrowDown);
            Assert.Equal("Item 1", comboBox.GetComboBoxValue());

            comboBox.SendKeys(Keys.ArrowUp);
            Assert.Equal("Item 0", comboBox.GetComboBoxValue());

            comboBox.SendKeys(Keys.ArrowUp);
            Assert.Equal("Item 0", comboBox.GetComboBoxValue());
        }

        [PlatformFact(TestPlatforms.Windows)]
        public void Can_Change_Wrapping_Selection_With_Keyboard_When_Closed()
        {
            CanChangeWrappingSelectionWithKeyboardWhenClosedCore();
        }

        protected void CanChangeWrappingSelectionWithKeyboardWhenClosedCore()
        {
            CanChangeWrappingSelectionWithKeyboardWhenClosedCore(Session);
        }

        private static void CanChangeWrappingSelectionWithKeyboardWhenClosedCore(AppiumDriver session)
        {
            var comboBox = session.FindElementByAccessibilityId("BasicComboBox");
            var wrap = session.FindElementByAccessibilityId("ComboBoxWrapSelection");

            if (wrap.GetIsChecked() != true)
                wrap.Click();

            session.FindElementByAccessibilityId("ComboBoxSelectionClear").Click();

            comboBox.SendKeys(Keys.ArrowDown);
            Assert.Equal("Item 0", comboBox.GetComboBoxValue());

            comboBox.SendKeys(Keys.ArrowDown);
            Assert.Equal("Item 1", comboBox.GetComboBoxValue());

            comboBox.SendKeys(Keys.ArrowDown);
            Assert.Equal("Item 0", comboBox.GetComboBoxValue());

            comboBox.SendKeys(Keys.ArrowDown);
            Assert.Equal("Item 1", comboBox.GetComboBoxValue());

            comboBox.SendKeys(Keys.ArrowUp);
            Assert.Equal("Item 0", comboBox.GetComboBoxValue());

            comboBox.SendKeys(Keys.ArrowUp);
            Assert.Equal("Item 1", comboBox.GetComboBoxValue());
        }

        [PlatformFact(TestPlatforms.Windows)]
        public void Can_Change_Selection_When_Open_With_Keyboard()
        {
            CanChangeSelectionWhenOpenWithKeyboardCore();
        }

        protected void CanChangeSelectionWhenOpenWithKeyboardCore()
        {
            CanChangeSelectionWhenOpenWithKeyboardCore(Session);
        }

        private static void CanChangeSelectionWhenOpenWithKeyboardCore(AppiumDriver session)
        {
            var comboBox = session.FindElementByAccessibilityId("BasicComboBox");

            session.FindElementByAccessibilityId("ComboBoxSelectFirst").Click();
            Assert.Equal("Item 0", comboBox.GetComboBoxValue());

            comboBox.SendKeys(Keys.LeftAlt + Keys.ArrowDown);
            comboBox.SendKeys(Keys.ArrowDown);

            var item = session.FindElementByName("Item 1");
            item.SendKeys(Keys.Enter);

            Assert.Equal("Item 1", comboBox.GetComboBoxValue());
        }

        [PlatformFact(TestPlatforms.Windows)]
        public void Can_Change_Selection_When_Open_With_Keyboard_From_Unselected()
        {
            CanChangeSelectionWhenOpenWithKeyboardFromUnselectedCore();
        }

        protected void CanChangeSelectionWhenOpenWithKeyboardFromUnselectedCore()
        {
            CanChangeSelectionWhenOpenWithKeyboardFromUnselectedCore(Session);
        }

        private static void CanChangeSelectionWhenOpenWithKeyboardFromUnselectedCore(AppiumDriver session)
        {
            var comboBox = session.FindElementByAccessibilityId("BasicComboBox");

            session.FindElementByAccessibilityId("ComboBoxSelectionClear").Click();
            Assert.Equal(string.Empty, comboBox.GetComboBoxValue());

            comboBox.SendKeys(Keys.LeftAlt + Keys.ArrowDown);
            comboBox.SendKeys(Keys.ArrowDown);

            var item = session.FindElementByName("Item 0");
            item.SendKeys(Keys.Enter);

            Assert.Equal("Item 0", comboBox.GetComboBoxValue());
        }

        [PlatformFact(TestPlatforms.Windows)]
        public void Can_Cancel_Keyboard_Selection_With_Escape()
        {
            CanCancelKeyboardSelectionWithEscapeCore();
        }

        protected void CanCancelKeyboardSelectionWithEscapeCore()
        {
            CanCancelKeyboardSelectionWithEscapeCore(Session);
        }

        private static void CanCancelKeyboardSelectionWithEscapeCore(AppiumDriver session)
        {
            var comboBox = session.FindElementByAccessibilityId("BasicComboBox");

            session.FindElementByAccessibilityId("ComboBoxSelectionClear").Click();
            Assert.Equal(string.Empty, comboBox.GetComboBoxValue());

            comboBox.SendKeys(Keys.LeftAlt + Keys.ArrowDown);
            comboBox.SendKeys(Keys.ArrowDown);

            var item = session.FindElementByName("Item 0");
            item.SendKeys(Keys.Escape);

            Assert.Equal(string.Empty, comboBox.GetComboBoxValue());
        }

        [Collection("Default")]
        public class Default : ComboBoxTests
        {
            public Default(DefaultAppFixture fixture) : base(fixture) { }

            [PlatformFact(TestPlatforms.Linux)]
            public void Linux_Can_Change_Selection_With_Keyboard_When_Closed()
            {
                using var fixture = new DefaultAppFixture();
                var isolated = new Default(fixture);
                isolated.CanChangeSelectionWithKeyboardWhenClosedCore();
            }

            [PlatformFact(TestPlatforms.Linux)]
            public void Linux_Can_Change_Wrapping_Selection_With_Keyboard_When_Closed()
            {
                using var fixture = new DefaultAppFixture();
                var isolated = new Default(fixture);
                isolated.CanChangeWrappingSelectionWithKeyboardWhenClosedCore();
            }

            [PlatformFact(TestPlatforms.Linux)]
            public void Linux_Can_Change_Selection_When_Open_With_Keyboard()
            {
                using var fixture = new DefaultAppFixture();
                var isolated = new Default(fixture);
                isolated.CanChangeSelectionWhenOpenWithKeyboardCore();
            }

            [PlatformFact(TestPlatforms.Linux)]
            public void Linux_Can_Change_Selection_When_Open_With_Keyboard_From_Unselected()
            {
                using var fixture = new DefaultAppFixture();
                var isolated = new Default(fixture);
                isolated.CanChangeSelectionWhenOpenWithKeyboardFromUnselectedCore();
            }

            [PlatformFact(TestPlatforms.Linux)]
            public void Linux_Can_Cancel_Keyboard_Selection_With_Escape()
            {
                using var fixture = new DefaultAppFixture();
                var isolated = new Default(fixture);
                isolated.CanCancelKeyboardSelectionWithEscapeCore();
            }

        }

        [Collection("OverlayPopups")]
        public class OverlayPopups : ComboBoxTests
        {
            public OverlayPopups(OverlayPopupsAppFixture fixture) : base(fixture) { }

            [PlatformFact(TestPlatforms.Linux)]
            public void Linux_Can_Change_Selection_With_Keyboard_When_Closed()
            {
                using var fixture = new OverlayPopupsAppFixture();
                var isolated = new OverlayPopups(fixture);
                isolated.CanChangeSelectionWithKeyboardWhenClosedCore();
            }

            [PlatformFact(TestPlatforms.Linux)]
            public void Linux_Can_Change_Wrapping_Selection_With_Keyboard_When_Closed()
            {
                using var fixture = new OverlayPopupsAppFixture();
                var isolated = new OverlayPopups(fixture);
                isolated.CanChangeWrappingSelectionWithKeyboardWhenClosedCore();
            }

            [PlatformFact(TestPlatforms.Linux)]
            public void Linux_Can_Change_Selection_When_Open_With_Keyboard()
            {
                using var fixture = new OverlayPopupsAppFixture();
                var isolated = new OverlayPopups(fixture);
                isolated.CanChangeSelectionWhenOpenWithKeyboardCore();
            }

            [PlatformFact(TestPlatforms.Linux)]
            public void Linux_Can_Change_Selection_When_Open_With_Keyboard_From_Unselected()
            {
                using var fixture = new OverlayPopupsAppFixture();
                var isolated = new OverlayPopups(fixture);
                isolated.CanChangeSelectionWhenOpenWithKeyboardFromUnselectedCore();
            }

            [PlatformFact(TestPlatforms.Linux)]
            public void Linux_Can_Cancel_Keyboard_Selection_With_Escape()
            {
                using var fixture = new OverlayPopupsAppFixture();
                var isolated = new OverlayPopups(fixture);
                isolated.CanCancelKeyboardSelectionWithEscapeCore();
            }

        }
    }
}
