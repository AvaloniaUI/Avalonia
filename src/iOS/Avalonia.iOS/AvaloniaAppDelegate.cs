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

    public enum AppViewControllerKind
    {
        /// <summary>
        /// Requests that an <see cref="ISingleViewApplicationLifetime"/> be assigned to <see cref="Application.ApplicationLifetime"/>.
        /// </summary>
        Avalonia,
        /// <summary>
        /// Requests that an <see cref="IPlatformSingleViewApplicationLifetime"/> be assigned to <see cref="Application.ApplicationLifetime"/>.
        /// </summary>
        Platform,
    }

    public class AvaloniaAppDelegate<TApp> : UIResponder, IUIApplicationDelegate, IAvaloniaAppDelegate
        where TApp : Application, new()
    {
        private readonly AppViewControllerKind _appViewControllerKind;
        private EventHandler<ActivatedEventArgs>? _onActivated, _onDeactivated;

        public AvaloniaAppDelegate() : this(AppViewControllerKind.Avalonia) { }

        public AvaloniaAppDelegate(AppViewControllerKind appViewControllerKind)
        {
            _appViewControllerKind = appViewControllerKind;
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

        protected virtual AppBuilder CreateAppBuilder() => AppBuilder.Configure<TApp>().UseiOS();
        protected virtual AppBuilder CustomizeAppBuilder(AppBuilder builder) => builder;

        [Export("window")]
        public UIWindow? Window { get; set; }

        [Export("application:didFinishLaunchingWithOptions:")]
        public bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
        {
            var builder = CreateAppBuilder();
            builder = CustomizeAppBuilder(builder);
                
            IApplicationLifetime lifetime;
            
            var window = new UIWindow();
            builder.AfterApplicationSetup(b => Window = window);

            switch (_appViewControllerKind)
            {
                case AppViewControllerKind.Avalonia:
                    lifetime = new SingleViewLifetime();

                    builder.AfterApplicationSetup(b =>
                    {
                        var view = new AvaloniaView();
                        ((SingleViewLifetime)lifetime).View = view;
                        var controller = new DefaultAvaloniaViewController
                        {
                            View = view
                        };
                        window.RootViewController = controller;
                        view.InitWithController(controller);
                    });
                    break;
                case AppViewControllerKind.Platform:
                    lifetime = new UIViewControllerLifetime { Window = window };
                    break;

                default:
                    throw new NotImplementedException();
            }

            builder.SetupWithLifetime(lifetime);

            window.MakeKeyAndVisible();

            return true;
        }

        [Export("application:openURL:options:")]
        public bool OpenUrl(UIApplication app, NSUrl url, NSDictionary options)
        {
            if (Uri.TryCreate(url.ToString(), UriKind.Absolute, out var uri))
            {
#if !TVOS
                if (uri.Scheme == Uri.UriSchemeFile)
                {
                    _onActivated?.Invoke(this, new FileActivatedEventArgs(new[] { Storage.IOSStorageItem.CreateItem(url) }));
                }
                else
#endif
                {
                    _onActivated?.Invoke(this, new ProtocolActivatedEventArgs(uri));   
                }
                return true;
            }

            return false;
        }

        [Export("application:continueUserActivity:restorationHandler:")]
        public bool ContinueUserActivity(UIApplication application, NSUserActivity userActivity, UIApplicationRestorationHandler completionHandler)
        {
            if (userActivity.ActivityType == NSUserActivityType.BrowsingWeb && Uri.TryCreate(userActivity.WebPageUrl?.ToString(), UriKind.RelativeOrAbsolute, out var uri))
            {
                // Activation using a univeral link or web browser-to-native app Handoff
                _onActivated?.Invoke(this, new ProtocolActivatedEventArgs(uri));
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
