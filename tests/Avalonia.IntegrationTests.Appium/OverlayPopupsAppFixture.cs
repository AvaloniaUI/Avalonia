using OpenQA.Selenium.Appium;

namespace Avalonia.IntegrationTests.Appium
{
    public class OverlayPopupsAppFixture : DefaultAppFixture
    {
        protected override void ConfigureWin32Options(AppiumOptions options, string? app = null)
        {
            base.ConfigureWin32Options(options, app);
            options.AddAdditionalCapability("appArguments", "--overlayPopups");
        }

        protected override void ConfigureMacOptions(AppiumOptions options, string? app = null)
        {
            base.ConfigureMacOptions(options, app);
            options.AddAdditionalCapability("appium:arguments", new[] { "--overlayPopups" });
        }
    }
}
