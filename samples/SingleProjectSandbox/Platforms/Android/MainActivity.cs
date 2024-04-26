using Android.App;
using Android.Content.PM;
using Avalonia;
using Avalonia.Android;

namespace SingleProjectSandbox;

[Activity(Label = "SingleProjectSandbox.Android", Theme = "@style/Theme.AppCompat.NoActionBar", MainLauncher = true, ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
public class MainActivity : AvaloniaMainActivity<App>
{
    protected override AppBuilder CreateAppBuilder()
    {
        return App.BuildAvaloniaApp().UseAndroid();
    }
}
