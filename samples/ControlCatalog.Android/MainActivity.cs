using Android.App;
using Android.Content.PM;
using Android.OS;
using Avalonia;
using Avalonia.Android;
using static Android.Content.Intent;

// leanback and touchscreen are required for the Android TV.
[assembly: UsesFeature("android.software.leanback", Required = false)]
[assembly: UsesFeature("android.hardware.touchscreen", Required = false)]

namespace ControlCatalog.Android
{
    [Activity(Name = "com.Avalonia.ControlCatalog.MainActivity", Label = "ControlCatalog.Android", Theme = "@style/MyTheme.NoActionBar", Icon = "@drawable/icon", MainLauncher = true, Exported = true, ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
    // CategoryLeanbackLauncher is required for Android TV.
    [IntentFilter(new [] { ActionView }, Categories = new [] { CategoryDefault, CategoryLeanbackLauncher })]
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

    /// <summary>
    /// Special activity to handle OpenUri activation.
    /// `AvaloniaActivity` internally will redirect parameters to the Avalonia Application.
    /// </summary>
    [Activity(NoHistory = true, LaunchMode = LaunchMode.SingleTop, Exported = true, Theme = "@android:style/Theme.NoDisplay")]
    [IntentFilter(new[] {ActionView}, Categories = new[] {CategoryDefault, CategoryBrowsable}, DataScheme = "avln")]
    public class DataSchemeActivity : AvaloniaActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Finish();
        }
    }
}
