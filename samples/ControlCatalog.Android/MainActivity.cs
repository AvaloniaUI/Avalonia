using Android.App;
using Android.Content.PM;
using Avalonia;
using Avalonia.Android;

namespace ControlCatalog.Android
{
    [Activity(Label = "ControlCatalog.Android", Theme = "@style/MyTheme.Main", Icon = "@drawable/icon", LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
    public class MainActivity : AvaloniaMainActivity
    {
    }
}
