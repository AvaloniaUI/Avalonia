using System;
using Xunit;

namespace Avalonia.IntegrationTests.Appium
{
    [Collection("Default")]
    public class EmbeddingTests : TestBase
    {
        public EmbeddingTests(DefaultAppFixture fixture)
            : base(fixture, "Embedding")
        {
        }

        [PlatformFact(TestPlatforms.Windows, "Not yet working on macOS")]
        public void Can_Edit_Native_TextBox()
        {
            // Appium has different XPath syntax between Windows and macOS.
            var textBox = OperatingSystem.IsWindows() ?
                Session.FindElementByXPath($"//*[@AutomationId='NativeTextBox']//*[1]") :
                Session.FindElementByXPath($"//*[@identifier='NativeTextBox']//*[1]");

            Assert.Equal("Native text box", textBox.Text);

            textBox.SendKeys("Hello world!");

            // SendKeys behaves differently between Windows and macOS. On Windows it inserts at the start
            // of the text box, on macOS it replaces the text for some reason. Sigh.
            var expected = OperatingSystem.IsWindows() ?
                "Hello world!Native text box" :
                "Hello world!";
            Assert.Equal(expected, textBox.Text);
        }

        [PlatformFact(TestPlatforms.Windows, "Not yet working on macOS")]
        public void Can_Edit_Native_TextBox_In_Popup()
        {
            var checkBox = Session.FindElementByAccessibilityId("EmbeddingPopupOpenCheckBox");
            checkBox.Click();

            try
            {
                // Appium has different XPath syntax between Windows and macOS.
                var textBox = OperatingSystem.IsWindows() ?
                    Session.FindElementByXPath($"//*[@AutomationId='NativeTextBoxInPopup']//*[1]") :
                    Session.FindElementByXPath($"//*[@identifier='NativeTextBoxInPopup']//*[1]");

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
