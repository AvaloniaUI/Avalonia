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
            var comboBox = Session.FindElementByAccessibilityId("BasicComboBox");
            var wrap = Session.FindElementByAccessibilityId("ComboBoxWrapSelection");

            if (wrap.GetIsChecked() != false)
                wrap.Click();

            Session.FindElementByAccessibilityId("ComboBoxSelectionClear").Click();

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
            var comboBox = Session.FindElementByAccessibilityId("BasicComboBox");
            var wrap = Session.FindElementByAccessibilityId("ComboBoxWrapSelection");

            if (wrap.GetIsChecked() != true)
                wrap.Click();

            Session.FindElementByAccessibilityId("ComboBoxSelectionClear").Click();

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
            var comboBox = Session.FindElementByAccessibilityId("BasicComboBox");

            Session.FindElementByAccessibilityId("ComboBoxSelectFirst").Click();
            Assert.Equal("Item 0", comboBox.GetComboBoxValue());

            comboBox.SendKeys(Keys.LeftAlt + Keys.ArrowDown);
            comboBox.SendKeys(Keys.ArrowDown);

            var item = Session.FindElementByName("Item 1");
            item.SendKeys(Keys.Enter);

            Assert.Equal("Item 1", comboBox.GetComboBoxValue());
        }

        [PlatformFact(TestPlatforms.Windows)]
        public void Can_Change_Selection_When_Open_With_Keyboard_From_Unselected()
        {
            var comboBox = Session.FindElementByAccessibilityId("BasicComboBox");

            Session.FindElementByAccessibilityId("ComboBoxSelectionClear").Click();
            Assert.Equal(string.Empty, comboBox.GetComboBoxValue());

            comboBox.SendKeys(Keys.LeftAlt + Keys.ArrowDown);
            comboBox.SendKeys(Keys.ArrowDown);

            var item = Session.FindElementByName("Item 0");
            item.SendKeys(Keys.Enter);

            Assert.Equal("Item 0", comboBox.GetComboBoxValue());
        }

        [PlatformFact(TestPlatforms.Windows)]
        public void Can_Cancel_Keyboard_Selection_With_Escape()
        {
            var comboBox = Session.FindElementByAccessibilityId("BasicComboBox");

            Session.FindElementByAccessibilityId("ComboBoxSelectionClear").Click();
            Assert.Equal(string.Empty, comboBox.GetComboBoxValue());

            comboBox.SendKeys(Keys.LeftAlt + Keys.ArrowDown);
            comboBox.SendKeys(Keys.ArrowDown);

            var item = Session.FindElementByName("Item 0");
            item.SendKeys(Keys.Escape);

            Assert.Equal(string.Empty, comboBox.GetComboBoxValue());
        }

        [Collection("Default")]
        public class Default : ComboBoxTests
        {
            public Default(DefaultAppFixture fixture) : base(fixture) { }
        }

        [Collection("OverlayPopups")]
        public class OverlayPopups : ComboBoxTests
        {
            public OverlayPopups(OverlayPopupsAppFixture fixture) : base(fixture) { }
        }
    }
}
