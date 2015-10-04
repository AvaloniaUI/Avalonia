using Android.App;
using Android.Content.PM;
using Android.OS;
using Perspex.Android;
using APoint = Android.Graphics.Point;

namespace Perspex.AndroidTestApplication
{
    [Activity(Label = "Main", 
        MainLauncher = true, 
        Icon = "@drawable/icon", 
        LaunchMode = LaunchMode.SingleInstance, 
        ScreenOrientation = ScreenOrientation.Portrait)]
    public class MainBaseActivity : PerspexActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            App app;
            if (Perspex.Application.Current != null)
                app = (App) Perspex.Application.Current;
            else
                app = new App();

            var window = app.BuildGridWithSomeButtonsAndStuff();
            window.Show();
            app.Run(window);
        }
    }
}