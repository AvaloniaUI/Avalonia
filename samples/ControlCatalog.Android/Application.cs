using Android.App;
using Android.Runtime;
using Avalonia;
using Avalonia.Android;
using Avalonia.Controls;

namespace ControlCatalog.Android
{
    [Application]
    [SceneApp<App>]
    public class Application : AvaloniaAndroidApplication<App>
    {
        protected Application(nint javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
        {
            return base.CustomizeAppBuilder(builder)
                 .UseScenes()
                 .AfterSetup(_ =>
                 {
                     Pages.EmbedSample.Implementation = new EmbedSampleAndroid();
                 });
        }
    }
}
