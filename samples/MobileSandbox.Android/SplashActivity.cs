using Android.App;
using Android.Content;
using Android.OS;

namespace MobileSandbox.Android
{
    [Activity(Theme = "@style/MyTheme.Splash", MainLauncher = true, NoHistory = true)]
    public class SplashActivity : Activity
    {
        protected override void OnResume()
        {
            base.OnResume();

            StartActivity(new Intent(Application.Context, typeof(MainActivity)));
        }
    }
}
