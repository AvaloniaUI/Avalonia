using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Foundation;
using Perspex.Controls;
using Perspex.iOS;
using Perspex.Media;
using Perspex.Threading;
using TestApplication;
using UIKit;

namespace Perspex.iOSTestApplication
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
            var app = new App();

            app.UseiOS();
            app.UseSkiaViewHost();
            app.UseSkia();

            // looking for this URI fails: "TestApplication.github_icon.png"
            var asm = typeof(App).Assembly;
            app.UseAssetAssembly(asm);

            //MainWindow.RootNamespace = "Perspex.iOSTestApplication";
            //var window = MainWindow.Create();
            ////var window = Create();
            //window.Show();
            //app.Run(window);
            app.Run();

            return true;
        }

        // This provides a simple UI tree for testing input handling
        public static Window Create()
        {
            Window window = new Window
            {
                Title = "Perspex Test Application",
                //Width = 900,
                //Height = 480,
                Background = Brushes.Red,
                Content = new StackPanel
                {
                    Margin = new Thickness(30),
                    Background = Brushes.Yellow,
                    Children = new Controls.Controls
                    {
                        new TextBlock
                        {
                            Text = "TEXT BLOCK",
                            Width = 300,
                            Height = 40,
                            Background = Brushes.White,
                            Foreground = Brushes.Black
                        },

                        new Button
                        {
                            Content = "BUTTON",
                            Width = 150,
                            Height = 40,
                            Background = Brushes.LightGreen,
                            Foreground = Brushes.Black
                        }

                    }
                }
            };

            return window;
        }
    }


}