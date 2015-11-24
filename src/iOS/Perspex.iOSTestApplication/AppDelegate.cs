using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Foundation;
using Perspex.iOS;
using Perspex.Threading;
using TestApplication;
using UIKit;

namespace Perspex.iOSTestApplication
{
    // The UIApplicationDelegate for the application. This class is responsible for launching the 
    // User Interface of the application, as well as listening (and optionally responding) to 
    // application events from iOS.
    [Register("AppDelegate")]
    public partial class AppDelegate : PerspexAppDelegate
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
            InitPerspex (typeof (App));

            var app = new App();

            MainWindow.RootNamespace = "Perspex.iOSTestApplication";
            var window = MainWindow.Create();
            window.Show();
            app.Run(window);

            return true;
        }
    }
}