using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Foundation;
using Avalonia.Controls;
using Avalonia.iOS;
using Avalonia.Media;
using Avalonia.Threading;
using UIKit;

namespace Avalonia.iOSTestApplication
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
            AppBuilder.Configure<SimpleApp>().UseSkia().UseiOS().SetupWithoutStarting();
            Window  = new AvaloniaWindow { Content = new SimpleControl()};
            Window.MakeKeyAndVisible();
            return true;
        }
    }


}