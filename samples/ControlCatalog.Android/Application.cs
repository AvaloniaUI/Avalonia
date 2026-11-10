using Android.App;
using Android.Runtime;
using Avalonia;
using Avalonia.Android;

namespace ControlCatalog.Android
{
    [Application]
    public class Application : AvaloniaAndroidApplication<App>
    {
        protected Application(nint javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
            RegisterWindowActivityType(typeof(WindowActivity));
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
