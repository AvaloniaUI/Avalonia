using Avalonia;
using Avalonia.Controls;
using Avalonia.iOS;
using Foundation;

namespace ControlCatalog;

// The UIApplicationDelegate for the application. This class is responsible for launching the 
// User Interface of the application, as well as listening (and optionally responding) to 
// application events from iOS.
public abstract class ControlCatalogAppDelegate<TApp>(AppViewControllerKind appViewControllerKind) : AvaloniaAppDelegate<TApp>(appViewControllerKind)
where TApp : Application, new()
{
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        return base.CustomizeAppBuilder(builder)
            .AfterSetup(_ =>
            {
                Pages.EmbedSample.Implementation = new EmbedSampleIOS();
            });
    }
}

[Register(nameof(SingleViewAppDelegate))]
public partial class SingleViewAppDelegate() : ControlCatalogAppDelegate<App>(AppViewControllerKind.Avalonia);

[Register(nameof(PlatformViewAppDelegate))]
public partial class PlatformViewAppDelegate() : ControlCatalogAppDelegate<PlatformViewAppDelegate.PlatformViewApp>(AppViewControllerKind.Platform)
{
    public class PlatformViewApp : App
    {
        protected override void CreateRootControl(object viewModel)
        {
            var view = new AvaloniaView();
            var controller = new DefaultAvaloniaViewController() { View = view };
            view.InitWithController(controller);
            ((IUIViewControllerApplicationLifetime)ApplicationLifetime).PlatformView = controller;

            view.Content = new MainView { DataContext = viewModel };
        }
    }
}
