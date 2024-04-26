using Android.App;
using Android.Content.PM;
using Avalonia;
using Avalonia.Android;
using static Android.Content.Intent;

// leanback and touchscreen are required for the Android TV.
[assembly: UsesFeature("android.software.leanback", Required = false)]
[assembly: UsesFeature("android.hardware.touchscreen", Required = false)]

namespace ControlCatalog.Android
{
    [Activity(Label = "ControlCatalog.Android", Theme = "@style/MyTheme.NoActionBar", Icon = "@drawable/icon", MainLauncher = true, Exported = true, ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
    // CategoryBrowsable and DataScheme are required for Protocol activation.
    // CategoryLeanbackLauncher is required for Android TV.
    [IntentFilter(new [] { ActionView }, Categories = new [] { CategoryDefault, CategoryBrowsable, CategoryLeanbackLauncher }, DataScheme = "avln" )]
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
