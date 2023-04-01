using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using Xunit;

namespace Avalonia.IntegrationTests.Appium
{
    public abstract class ComboBoxTests
    {
        private readonly AppiumDriver<AppiumWebElement> _session;

        public ComboBoxTests(DefaultAppFixture fixture)
        {
            _session = fixture.Session;

            var tabs = _session.FindElementByAccessibilityId("MainTabs");
            var tab = tabs.FindElementByName("ComboBox");
            tab.Click();
        }

        [Fact]
        public void Can_Change_Selection_Using_Mouse()
        {
            var comboBox = _session.FindElementByAccessibilityId("BasicComboBox");

            _session.FindElementByAccessibilityId("ComboBoxSelectFirst").Click();
            Assert.Equal("Item 0", comboBox.GetComboBoxValue());

            comboBox.Click();
            _session.FindElementByName("Item 1").SendClick();

            Assert.Equal("Item 1", comboBox.GetComboBoxValue());
        }

        [Fact]
        public void Can_Change_Selection_From_Unselected_Using_Mouse()
        {
            var comboBox = _session.FindElementByAccessibilityId("BasicComboBox");

            _session.FindElementByAccessibilityId("ComboBoxSelectionClear").Click();
            Assert.Equal(string.Empty, comboBox.GetComboBoxValue());

            comboBox.Click();
            _session.FindElementByName("Item 0").SendClick();

            Assert.Equal("Item 0", comboBox.GetComboBoxValue());
        }

        [PlatformFact(TestPlatforms.Windows)]
        public void Can_Change_Selection_With_Keyboard_When_Closed()
        {
            var comboBox = _session.FindElementByAccessibilityId("BasicComboBox");
            var wrap = _session.FindElementByAccessibilityId("ComboBoxWrapSelection");

            if (wrap.GetIsChecked() != false)
                wrap.Click();

            _session.FindElementByAccessibilityId("ComboBoxSelectionClear").Click();

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
            var comboBox = _session.FindElementByAccessibilityId("BasicComboBox");
            var wrap = _session.FindElementByAccessibilityId("ComboBoxWrapSelection");

            if (wrap.GetIsChecked() != true)
                wrap.Click();

            _session.FindElementByAccessibilityId("ComboBoxSelectionClear").Click();

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
            var comboBox = _session.FindElementByAccessibilityId("BasicComboBox");

            _session.FindElementByAccessibilityId("ComboBoxSelectFirst").Click();
            Assert.Equal("Item 0", comboBox.GetComboBoxValue());

            comboBox.SendKeys(Keys.LeftAlt + Keys.ArrowDown);
            comboBox.SendKeys(Keys.ArrowDown);

            var item = _session.FindElementByName("Item 1");
            item.SendKeys(Keys.Enter);

            Assert.Equal("Item 1", comboBox.GetComboBoxValue());
        }

        [PlatformFact(TestPlatforms.Windows)]
        public void Can_Change_Selection_When_Open_With_Keyboard_From_Unselected()
        {
            var comboBox = _session.FindElementByAccessibilityId("BasicComboBox");

            _session.FindElementByAccessibilityId("ComboBoxSelectionClear").Click();
            Assert.Equal(string.Empty, comboBox.GetComboBoxValue());

            comboBox.SendKeys(Keys.LeftAlt + Keys.ArrowDown);
            comboBox.SendKeys(Keys.ArrowDown);

            var item = _session.FindElementByName("Item 0");
            item.SendKeys(Keys.Enter);

            Assert.Equal("Item 0", comboBox.GetComboBoxValue());
        }

        [PlatformFact(TestPlatforms.Windows)]
        public void Can_Cancel_Keyboard_Selection_With_Escape()
        {
            var comboBox = _session.FindElementByAccessibilityId("BasicComboBox");

            _session.FindElementByAccessibilityId("ComboBoxSelectionClear").Click();
            Assert.Equal(string.Empty, comboBox.GetComboBoxValue());

            comboBox.SendKeys(Keys.LeftAlt + Keys.ArrowDown);
            comboBox.SendKeys(Keys.ArrowDown);

            var item = _session.FindElementByName("Item 0");
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
