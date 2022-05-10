using Android.App;
using Android.Content.PM;

using Avalonia;
using Avalonia.Android;

namespace NativeEmbedSample.Android;

[Activity(Label = "NativeEmbedSample", Theme = "@style/MyTheme.NoActionBar", Icon = "@drawable/icon", LaunchMode = LaunchMode.SingleInstance, ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
public class MainActivity : AvaloniaActivity<App>
{
}
