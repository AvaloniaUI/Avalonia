using System;
using OpenQA.Selenium.Appium;
using Xunit;

namespace Avalonia.IntegrationTests.Appium
{
    [Collection("Default")]
    public class EmbeddingTests
    {
        private readonly AppiumDriver<AppiumWebElement> _session;

        public EmbeddingTests(DefaultAppFixture fixture)
        {
            _session = fixture.Session;

            var tabs = _session.FindElementByAccessibilityId("MainTabs");
            var tab = tabs.FindElementByName("Embedding");
            tab.Click();
        }

        [Fact(Skip = "Crashes on CI")]
        public void Can_Edit_Native_TextBox()
        {
            // Appium has different XPath syntax between Windows and macOS.
            var textBox = OperatingSystem.IsWindows() ?
                _session.FindElementByXPath($"//*[@AutomationId='NativeTextBox']//*[1]") :
                _session.FindElementByXPath($"//*[@identifier='NativeTextBox']//*[1]");

            Assert.Equal("Native text box", textBox.Text);

            textBox.SendKeys("Hello world!");

            // SendKeys behaves differently between Windows and macOS. On Windows it inserts at the start
            // of the text box, on macOS it replaces the text for some reason. Sigh.
            var expected = OperatingSystem.IsWindows() ?
                "Hello world!Native text box" :
                "Hello world!";
            Assert.Equal(expected, textBox.Text);
        }

        [Fact(Skip = "Crashes on CI")]
        public void Can_Edit_Native_TextBox_In_Popup()
        {
            var checkBox = _session.FindElementByAccessibilityId("EmbeddingPopupOpenCheckBox");
            checkBox.Click();

            try
            {
                // Appium has different XPath syntax between Windows and macOS.
                var textBox = OperatingSystem.IsWindows() ?
                    _session.FindElementByXPath($"//*[@AutomationId='NativeTextBoxInPopup']//*[1]") :
                    _session.FindElementByXPath($"//*[@identifier='NativeTextBoxInPopup']//*[1]");

                Assert.Equal("Native text box", textBox.Text);

                textBox.SendKeys("Hello world!");

                // SendKeys behaves differently between Windows and macOS. On Windows it inserts at the start
                // of the text box, on macOS it replaces the text for some reason. Sigh.
                var expected = OperatingSystem.IsWindows() ?
                    "Hello world!Native text box" :
                    "Hello world!";
                Assert.Equal(expected, textBox.Text);
            }
            finally
            {
                checkBox.Click();
            }
        }
    }
}
