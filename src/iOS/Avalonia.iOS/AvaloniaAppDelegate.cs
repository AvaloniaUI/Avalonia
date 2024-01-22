using System;
using Foundation;
using Avalonia.Controls.ApplicationLifetimes;

using UIKit;

namespace Avalonia.iOS
{
    public interface IAvaloniaAppDelegate
    {
        event EventHandler<ActivatedEventArgs> Activated;
        event EventHandler<ActivatedEventArgs> Deactivated;
    }

    public class AvaloniaAppDelegate<TApp> : UIResponder, IUIApplicationDelegate, IAvaloniaAppDelegate
        where TApp : Application, new()
    {
        private EventHandler<ActivatedEventArgs>? _onActivated, _onDeactivated;

        public AvaloniaAppDelegate()
        {
            NSNotificationCenter.DefaultCenter.AddObserver(UIApplication.DidEnterBackgroundNotification, OnEnteredBackground);
            NSNotificationCenter.DefaultCenter.AddObserver(UIApplication.WillEnterForegroundNotification, OnLeavingBackground);
        }

        event EventHandler<ActivatedEventArgs> IAvaloniaAppDelegate.Activated
        {
            add { _onActivated += value; }
            remove { _onActivated -= value; }
        }
        event EventHandler<ActivatedEventArgs> IAvaloniaAppDelegate.Deactivated
        {
            add { _onDeactivated += value; }
            remove { _onDeactivated -= value; }
        }
        
        protected virtual AppBuilder CustomizeAppBuilder(AppBuilder builder) => builder;
        
        [Export("window")]
        public UIWindow? Window { get; set; }

        [Export("application:didFinishLaunchingWithOptions:")]
        public bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
        {
            var builder = AppBuilder.Configure<TApp>().UseiOS();

            var lifetime = new SingleViewLifetime(this);

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

            Window!.MakeKeyAndVisible();

            return true;
        }

        [Export("application:openURL:options:")]
        public bool OpenUrl(UIApplication app, NSUrl url, NSDictionary options)
        {
            if (Uri.TryCreate(url.ToString(), UriKind.Absolute, out var uri))
            {
                _onActivated?.Invoke(this, new ProtocolActivatedEventArgs(ActivationKind.OpenUri, uri));
                return true;
            }

            return false;
        }

        private void OnEnteredBackground(NSNotification notification)
        {
            _onDeactivated?.Invoke(this, new ActivatedEventArgs(ActivationKind.Background));
        }

        private void OnLeavingBackground(NSNotification notification)
        {
            _onActivated?.Invoke(this, new ActivatedEventArgs(ActivationKind.Background));
        }
    }
}
