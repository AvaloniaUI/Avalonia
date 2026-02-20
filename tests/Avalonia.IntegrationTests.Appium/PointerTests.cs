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

        [PlatformFact(TestPlatforms.Windows | TestPlatforms.MacOS)]
        public void Pointer_Capture_Is_Released_When_Showing_Dialog()
        {
            PointerCaptureIsReleasedWhenShowingDialogCore();
        }

        [PlatformFact(TestPlatforms.Linux)]
        public void Linux_Pointer_Capture_Is_Released_When_Showing_Dialog()
        {
            using var fixture = new DefaultAppFixture();
            var isolated = new PointerTests(fixture);
            isolated.PointerCaptureIsReleasedWhenShowingDialogCore();
        }

        private void PointerCaptureIsReleasedWhenShowingDialogCore()
        {
            var button = Session.FindElementByAccessibilityId("PointerPageShowDialog");

            button.OpenWindowWithClick().Dispose();

            var status = Session.FindElementByAccessibilityId("PointerCaptureStatus");
            Assert.Equal("None", status.Text);
        }
    }
}
