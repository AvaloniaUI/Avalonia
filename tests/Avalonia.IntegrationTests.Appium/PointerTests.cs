using OpenQA.Selenium.Interactions;
using Xunit;

namespace Avalonia.IntegrationTests.Appium
{
    [Collection("Default")]
    public class PointerTests
    {
        private readonly AppiumDriver _session;

        public PointerTests(DefaultAppFixture fixture)
        {
            _session = fixture.Session;

            var tabs = _session.FindElementByAccessibilityId("MainTabs");
            var tab = tabs.FindElementByName("Pointer");
            tab.Click();
        }

        [Fact]
        public void Pointer_Capture_Is_Released_When_Showing_Dialog()
        {
            var button = _session.FindElementByAccessibilityId("PointerPageShowDialog");

            button.OpenWindowWithClick().Dispose();

            var status = _session.FindElementByAccessibilityId("PointerCaptureStatus");
            Assert.Equal("None", status.Text);
        }
    }
}
