using Android.App;
using Android.Content.PM;
using Avalonia;
using Avalonia.Android;
using static Android.Content.Intent;

namespace ControlCatalog.Android
{
    [Activity(Label = "ControlCatalog.Android", Theme = "@style/MyTheme.NoActionBar", Icon = "@drawable/icon", MainLauncher = true, Exported = true, ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
    // IntentFilter are here to test IActivatableApplicationLifetime 
    [IntentFilter(new [] { ActionView }, Categories = new [] { CategoryDefault, CategoryBrowsable }, DataScheme = "avln" )]
    public class MainActivity : AvaloniaMainActivity<App>
    {
        protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
        {
            return base.CustomizeAppBuilder(builder)
                 .AfterSetup(_ =>
                 {
                     Pages.EmbedSample.Implementation = new EmbedSampleAndroid();
                 });
        }
    }
}
