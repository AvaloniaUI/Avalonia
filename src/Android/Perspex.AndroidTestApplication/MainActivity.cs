using Android.App;
using Android.Content.PM;
using Android.OS;
using Perspex.Android;
using Perspex.Android.Platform.Specific;
using Perspex.Android.Platform.Specific.Helpers;
using Perspex.Controls.Platform;
using Perspex.Platform;
using TestApplication;

namespace Perspex.AndroidTestApplication
{
    [Activity(Label = "Main",
        MainLauncher = true,
        Icon = "@drawable/icon",
        LaunchMode = LaunchMode.SingleInstance/*,
        ScreenOrientation = ScreenOrientation.Landscape*/)]
    public class MainBaseActivity : PerspexActivity
    {
        public MainBaseActivity() : base(typeof (App))
        {

        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            App app;
            if (Perspex.Application.Current != null)
                app = (App)Perspex.Application.Current;
            else
                app = new App();
           

            MainWindow.RootNamespace = "Perspex.AndroidTestApplication";
            var window = MainWindow.Create();

            window.Show();
            app.Run(window);
        }
        
    }
}