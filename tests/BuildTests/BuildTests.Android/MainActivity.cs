using Android.App;
using Android.Content.PM;
using Avalonia.Android;

namespace BuildTests.Android;

[Activity(
    Label = "BuildTests.Android",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity<App>;
