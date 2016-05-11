using Android.App;
using Android.Content.PM;
using Android.OS;
using Avalonia.Android;
using Avalonia.Android.Platform.Specific;
using Avalonia.Android.Platform.Specific.Helpers;
using Avalonia.Controls.Platform;
using Avalonia.Platform;
using TestApplication;

namespace Avalonia.AndroidTestApplication
{
    [Activity(Label = "Main",
        MainLauncher = true,
        Icon = "@drawable/icon",
        LaunchMode = LaunchMode.SingleInstance/*,
        ScreenOrientation = ScreenOrientation.Landscape*/)]
    public class MainBaseActivity : AvaloniaActivity
    {
        public MainBaseActivity() : base(typeof (App))
        {

        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            App app;
            if (Avalonia.Application.Current != null)
                app = (App)Avalonia.Application.Current;
            else
                app = new App();
           

            MainWindow.RootNamespace = "Avalonia.AndroidTestApplication";
            var window = MainWindow.Create();

            window.Show();
            app.Run(window);
        }
        
    }
}