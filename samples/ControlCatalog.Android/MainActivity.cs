using Android.App;
using Android.Content.PM;
using Avalonia;
using Avalonia.Android;

namespace ControlCatalog.Android
{
    [Activity(Label = "ControlCatalog.Android", Theme = "@style/MyTheme.NoActionBar", Icon = "@drawable/icon", ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
    public class MainActivity : AvaloniaActivity
    {
    }
}
