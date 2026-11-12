using Android.App;
using Android.Content.PM;
using Avalonia.Android;

namespace ImpellerSandbox.Android;

[Activity(Label = "ImpellerSandbox", Theme = "@style/MyTheme.NoActionBar", Icon = "@drawable/icon", MainLauncher = true,
    Exported = true, ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity
{
}
