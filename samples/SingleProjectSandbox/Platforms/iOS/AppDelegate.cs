using Foundation;
using Avalonia;
using Avalonia.iOS;
using UIKit;

namespace SingleProjectSandbox;

// The UIApplicationDelegate for the application. This class is responsible for launching the 
// User Interface of the application, as well as listening (and optionally responding) to 
// application events from iOS.
[Register("AppDelegate")]
public class AppDelegate : AvaloniaAppDelegate<App>
{
    protected override AppBuilder CreateAppBuilder() =>
        App.BuildAvaloniaApp().UseiOS();

    // This is the main entry point of the application.
    internal static void Main(string[] args)
    {
        // if you want to use a different Application Delegate class from "AppDelegate"
        // you can specify it here.
        UIApplication.Main(args, null, typeof(AppDelegate));
    }
}
