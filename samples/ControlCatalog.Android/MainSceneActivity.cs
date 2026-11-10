using Android.App;
using Android.Content.PM;
using Avalonia.Android;
using Avalonia.Android.Scene;
using Android.OS;

namespace ControlCatalog.Android
{

    [Activity(Label = "ControlCatalog.Android", Theme = "@style/MyTheme.NoActionBar", Icon = "@drawable/icon", MainLauncher = true, Exported = true, ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode | ConfigChanges.SmallestScreenSize)]
    internal class MainSceneActivity : SceneActivity<MainScene>
    {
        override protected void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
        }

        public static void RegisterInManager()
        {
            RegisterInManager<MainScene>(typeof(MainSceneActivity));
        }
    }
}
