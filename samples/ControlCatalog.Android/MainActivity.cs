using Android.App;
using Android.Content.PM;
using Avalonia;
using Avalonia.Android;

namespace ControlCatalog.Android
{
    [Activity(Label = "ControlCatalog.Android", Theme = "@style/MyTheme.NoActionBar", Icon = "@drawable/icon", MainLauncher = true, ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
    public class MainActivity : AvaloniaMainActivity<App>
    {
        protected override Avalonia.AppBuilder CustomizeAppBuilder(Avalonia.AppBuilder builder)
        {
            return base.CustomizeAppBuilder(builder)
                 .AfterSetup(_ =>
                 {
                     Pages.EmbedSample.Implementation = new EmbedSampleAndroid();
                 });
        }
    }
}
