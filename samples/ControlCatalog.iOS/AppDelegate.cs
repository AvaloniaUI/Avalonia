using Foundation;
using UIKit;
using Avalonia;
using Avalonia.Controls;
using Avalonia.iOS;
using Avalonia.Media;

namespace ControlCatalog
{
    // The UIApplicationDelegate for the application. This class is responsible for launching the 
    // User Interface of the application, as well as listening (and optionally responding) to 
    // application events from iOS.
    [Register("AppDelegate")]
    public partial class AppDelegate : AvaloniaAppDelegate<App>
    {
        protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
        {
            return base.CustomizeAppBuilder(builder)
                .AfterSetup(_ =>
                {
                    Pages.EmbedSample.Implementation = new EmbedSampleIOS();

                    // Read once per top level when it is created, so it has to be bound before the
                    // view is set up. There is no command line here, so the sample asks for the
                    // widest gamut outright to exercise the path. See the "Wide Gamut" page.
                    AvaloniaLocator.CurrentMutable.Bind<PresentationOptions>().ToConstant(
                        new PresentationOptions { PreferredColorSpace = PresentationColorSpace.WideGamut });
                });
        }
    }
}
