using Android.App;
using Android.Content.PM;
using Avalonia.Android;
using Avalonia.Android.Scene;
using Android.OS;

namespace ControlCatalog.Android
{

    [Activity(Label = "Sample Scene", Theme = "@style/MyTheme.NoActionBar", Icon = "@drawable/icon", Exported = true, ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode | ConfigChanges.SmallestScreenSize)]
    internal class SampleSceneActivity : SceneActivity<SampleScene>
    {
        override protected void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
        }

        public static void RegisterInManager()
        {
            RegisterInManager<SampleScene>(typeof(SampleSceneActivity));
        }
    }
}
