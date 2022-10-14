using Android.App;
using Android.Content.PM;
using Avalonia.Maui.Platforms.Android;

namespace MauiSample
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : AvaloniaMauiActivity
    {
    }
}