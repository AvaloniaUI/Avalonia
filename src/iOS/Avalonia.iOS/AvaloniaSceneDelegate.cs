using System;
using Avalonia.Controls.ApplicationLifetimes;
using Foundation;
using UIKit;

namespace Avalonia.iOS;

internal sealed class AvaloniaSceneDelegate : UIResponder, IUIWindowSceneDelegate
{
    [Export("window")]
    public UIWindow? Window { get; set; }

    [Export("scene:willConnectToSession:options:")]
    public void WillConnect(UIScene scene, UISceneSession session, UISceneConnectionOptions connectionOptions)
    {
        // Protect against non-application scenes, which can be created by the system for external displays, CarPlay, etc. 
        if (session.Role != UIWindowSceneSessionRole.Application ||
            session.Configuration.Name is not null ||
            scene is not UIWindowScene windowScene ||
            Application.Current?.ApplicationLifetime is not SingleViewLifetime lifetime)
        {
            return;
        }

        Window = new UIWindow(windowScene);
        InitWindow(Window, lifetime);

        Window.MakeKeyAndVisible();

        // Cold-launch activation: when iOS launches the app to handle a
        // Universal Link, a Handoff, or a custom-scheme URL, the
        // payload is delivered via the connectionOptions parameter —
        // NOT via the warm-path scene:continueUserActivity: or
        // scene:openURLContexts: selectors. Without draining the
        // options here, the URL is silently dropped and subscribers to
        // IActivatableLifetime.Activated only see the base
        // ActivatedEventArgs from the foreground transition. Fixes the
        // cold path of #17600 that PR #18005 only addressed for the
        // warm case.
        DispatchConnectionOptions(connectionOptions);
    }

    [Export("scene:continueUserActivity:")]
    public void ContinueUserActivity(UIScene scene, NSUserActivity userActivity)
    {
        var appDelegate = UIApplication.SharedApplication.Delegate as IAvaloniaAppInternalDelegate;
        appDelegate?.ContinueUserActivity(userActivity);
    }

    [Export("scene:openURLContexts:")]
    public void OpenUrlContexts(UIScene scene, NSSet<UIOpenUrlContext> urlContexts)
    {
        var appDelegate = UIApplication.SharedApplication.Delegate as IAvaloniaAppInternalDelegate;
        if (appDelegate is null || urlContexts is null)
            return;
        foreach (var ctx in urlContexts)
            appDelegate.OpenUrl(ctx.Url);
    }

    private static void DispatchConnectionOptions(UISceneConnectionOptions connectionOptions)
    {
        var appDelegate = UIApplication.SharedApplication.Delegate as IAvaloniaAppInternalDelegate;
        if (appDelegate is null)
            return;

        if (connectionOptions.UserActivities is { } activities)
        {
            foreach (NSUserActivity activity in activities)
                appDelegate.ContinueUserActivity(activity);
        }

        if (connectionOptions.UrlContexts is { } urlContexts)
        {
            foreach (var ctx in urlContexts)
                appDelegate.OpenUrl(ctx.Url);
        }
    }

    internal static void InitWindow(UIWindow window, SingleViewLifetime lifetime)
    {
        var view = new AvaloniaView();
        lifetime.View = view;

        var controller = new DefaultAvaloniaViewController { View = view };
        window.RootViewController = controller;
        view.InitWithController(controller);
    }
}
