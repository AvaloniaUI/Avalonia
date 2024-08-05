using Xunit;

namespace Avalonia.IntegrationTests.Appium
{
    [Collection("Default")]
    public class PointerTests : TestBase
    {
        public PointerTests(DefaultAppFixture fixture)
            : base(fixture, "Pointer")
        {
        }

        [Fact]
        public void Pointer_Capture_Is_Released_When_Showing_Dialog()
        {
            var button = Session.FindElementByAccessibilityId("PointerPageShowDialog");

            button.OpenWindowWithClick().Dispose();

            var status = Session.FindElementByAccessibilityId("PointerCaptureStatus");
            Assert.Equal("None", status.Text);
        }
    }
}
