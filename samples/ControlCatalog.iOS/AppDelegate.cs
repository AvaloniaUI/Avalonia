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
    public partial class AppDelegate : UIApplicationDelegate
    {
        public override UIWindow Window { get; set; }

        //
        // This method is invoked when the application has loaded and is ready to run. In this 
        // method you should instantiate the window, load the UI into it and then make the window
        // visible.
        //
        // You have 17 seconds to return from this method, or iOS will terminate your application.
        //
        public override bool FinishedLaunching(UIApplication uiapp, NSDictionary options)
        {
            AppBuilder.Configure<App>()
                .UseiOS()
                .UseSkia().SetupWithoutStarting();
            Window = new AvaloniaWindow() {Content = new MainView(), StatusBarColor = Colors.LightSteelBlue};
            Window.MakeKeyAndVisible();
            return true;
        }
    }
}