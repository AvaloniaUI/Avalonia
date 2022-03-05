using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Foundation;
using UIKit;

namespace Avalonia.iOS
{
    public class AvaloniaAppDelegate<TApp> : UIResponder, IUIApplicationDelegate
        where TApp : Application, new()
    {
        protected virtual AppBuilder CustomizeAppBuilder(AppBuilder builder) => builder.UseiOS();
        
        [Export("window")]
        public UIWindow Window { get; set; }

        [Export("application:didFinishLaunchingWithOptions:")]
        public bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
        {
            var builder = AppBuilder.Configure<TApp>();
            CustomizeAppBuilder(builder);
            var lifetime = new Lifetime();
            builder.AfterSetup(_ =>
            {
                Window = new UIWindow();
                var view = new AvaloniaView();
                lifetime.View = view;
                Window.RootViewController = new UIViewController
                {
                    View = view
                };
            });
            
            builder.SetupWithLifetime(lifetime);
            
            Window.Hidden = false;
            return true;
        }

        class Lifetime : ISingleViewApplicationLifetime
        {
            public AvaloniaView View;
            public Control MainView
            {
                get => View.Content;
                set => View.Content = value;
            }
        }
    }
}
