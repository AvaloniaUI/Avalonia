using Foundation;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

using UIKit;

namespace Avalonia.iOS
{
    public class AvaloniaAppDelegate<TApp> : UIResponder, IUIApplicationDelegate
        where TApp : Application, new()
    {
        class SingleViewLifetime : ISingleViewApplicationLifetime
        {
            public AvaloniaView View;

            public Control MainView
            {
                get => View.Content;
                set => View.Content = value;
            }
        }

        protected virtual AppBuilder CustomizeAppBuilder(AppBuilder builder) => builder;
        
        [Export("window")]
        public UIWindow Window { get; set; }

        [Export("application:didFinishLaunchingWithOptions:")]
        public bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
        {
            var builder = AppBuilder.Configure<TApp>().UseiOS();

            var lifetime = new SingleViewLifetime();

            builder.AfterSetup(_ =>
            {
                Window = new UIWindow();

                var view = new AvaloniaView();
                lifetime.View = view;
                var controller = new DefaultAvaloniaViewController
                {
                    View = view
                };
                Window.RootViewController = controller;
                view.InitWithController(controller);
            });

            CustomizeAppBuilder(builder);

            builder.SetupWithLifetime(lifetime);

            Window.MakeKeyAndVisible();

            return true;
        }
    }
}
