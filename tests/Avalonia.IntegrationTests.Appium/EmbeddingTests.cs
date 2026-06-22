using System;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using Xunit;

namespace Avalonia.IntegrationTests.Appium
{
    [Collection("Default")]
    public class EmbeddingTests : TestBase
    {
        public EmbeddingTests(DefaultAppFixture fixture)
            : base(fixture, "Embedding")
        {
            var reset = Session.FindElementByAccessibilityId("Reset");
            reset.Click();
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

        [PlatformFact(TestPlatforms.Windows, "Not yet working on macOS")]
        public void Showing_ToolTip_Does_Not_Steal_Focus_From_Native_TextBox()
        {
            // Appium has different XPath syntax between Windows and macOS.
            var textBox = OperatingSystem.IsWindows() ?
                Session.FindElementByXPath($"//*[@AutomationId='NativeTextBox']//*[1]") :
                Session.FindElementByXPath($"//*[@identifier='NativeTextBox']//*[1]");

            // Clicking on the text box causes the cursor to hover over it, opening the tooltip.
            textBox.Click();
            Thread.Sleep(1000);

            // Ensure the tooltip has opened.
            Session.FindElementByAccessibilityId("NativeTextBoxToolTip");

            // The tooltip should not have stolen focus from the text box, so text entry should work.
            new Actions(Session).SendKeys("Hello world!").Perform();

            // SendKeys behaves differently between Windows and macOS. On Windows it inserts at the start
            // of the text box, on macOS it replaces the text for some reason. Sigh.
            var expected = OperatingSystem.IsWindows() ?
                "Native text boxHello world!" :
                "Hello world!";

            Assert.Equal(expected, textBox.Text);
        }

        [PlatformFact(TestPlatforms.Windows, "Not yet working on macOS")]
        public void Showing_ContextMenu_Steals_Focus_From_Native_TextBox()
        {
            // Appium has different XPath syntax between Windows and macOS.
            var textBox = OperatingSystem.IsWindows() ?
                Session.FindElementByXPath($"//*[@AutomationId='NativeTextBox']//*[1]") :
                Session.FindElementByXPath($"//*[@identifier='NativeTextBox']//*[1]");

            // Click on the text box the right-click to show the context menu.
            textBox.Click();
            new Actions(Session).ContextClick(textBox).Perform();

            // Ensure the context menu has opened.
            Session.FindElementByAccessibilityId("NativeTextBoxContextMenu");

            // Select the first menu item with the keyboard.
            new Actions(Session)
                .SendKeys(Keys.ArrowDown)
                .SendKeys(Keys.Enter)
                .Perform();

            Assert.Equal("Context menu item clicked", textBox.Text);
        }
    }
}
