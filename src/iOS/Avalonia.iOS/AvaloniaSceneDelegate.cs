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
        if (session.Configuration.Name is not null ||
            scene is not UIWindowScene windowScene ||
            Application.Current?.ApplicationLifetime is not SingleViewLifetime lifetime)
        {
            return;
        }

        Window = new UIWindow(windowScene);
        InitWindow(Window, lifetime);

        Window.MakeKeyAndVisible();
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
