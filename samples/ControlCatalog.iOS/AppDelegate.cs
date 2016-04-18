using Foundation;
using UIKit;
using Perspex;

namespace ControlCatalog
{
    // The UIApplicationDelegate for the application. This class is responsible for launching the 
    // User Interface of the application, as well as listening (and optionally responding) to 
    // application events from iOS.
    [Register("AppDelegate")]
    public partial class AppDelegate : UIApplicationDelegate
    {
        //
        // This method is invoked when the application has loaded and is ready to run. In this 
        // method you should instantiate the window, load the UI into it and then make the window
        // visible.
        //
        // You have 17 seconds to return from this method, or iOS will terminate your application.
        //
        public override bool FinishedLaunching(UIApplication uiapp, NSDictionary options)
        {
            new App()
                .UseiOS()
                .UseSkiaViewHost()
                .UseSkia()
                .UseAssetAssembly(typeof(App).Assembly)
                .LoadFromXaml()
                .RunWithMainWindow<MainWindow>();

            return true;
        }
    }


}