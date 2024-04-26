using System.Threading;
using OpenQA.Selenium.Appium;
using Xunit;

namespace Avalonia.IntegrationTests.Appium
{
    [Collection("Default")]
    public class EmbeddingTests_Windows
    {
        private readonly AppiumDriver<AppiumWebElement> _session;

        public EmbeddingTests_Windows(DefaultAppFixture fixture)
        {
            _session = fixture.Session;

            var tabs = _session.FindElementByAccessibilityId("MainTabs");
            var tab = tabs.FindElementByName("Embedding");
            tab.Click();
        }

        [PlatformFact(TestPlatforms.Windows)]
        public void Can_Edit_Native_TextBox()
        {
            var textBox = _session.FindElementByXPath($"//*[@AutomationId='NativeTextBox']//*[1]");

            Assert.Equal("Win32 EDIT", textBox.Text);

            textBox.SendKeys("Hello, World!");

            Assert.Equal("Hello, World!Win32 EDIT", textBox.Text);
        }
    }
}
