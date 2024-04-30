using OpenQA.Selenium.Appium;

namespace Avalonia.IntegrationTests.Appium
{
    public class OverlayPopupsAppFixture : DefaultAppFixture
    {
        protected override void ConfigureWin32Options(AppiumOptions options)
        {
            base.ConfigureWin32Options(options);
            options.AddAdditionalAppiumOption("appArguments", "--overlayPopups");
        }

        protected override void ConfigureMacOptions(AppiumOptions options)
        {
            base.ConfigureMacOptions(options);
            options.AddAdditionalAppiumOption("appium:arguments", new[] { "--overlayPopups" });
        }
    }
}
