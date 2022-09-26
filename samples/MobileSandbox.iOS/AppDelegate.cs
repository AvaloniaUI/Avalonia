using Foundation;
using UIKit;
using Avalonia;
using Avalonia.Controls;
using Avalonia.iOS;
using Avalonia.Logging;
using Avalonia.Media;

namespace MobileSandbox
{
    // The UIApplicationDelegate for the application. This class is responsible for launching the 
    // User Interface of the application, as well as listening (and optionally responding) to 
    // application events from iOS.
    [Register("AppDelegate")]
    public partial class AppDelegate : AvaloniaAppDelegate<App>
    {
        protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
        {
            return builder.LogToTrace(LogEventLevel.Debug, "IOSIME");
        }
    }
}
