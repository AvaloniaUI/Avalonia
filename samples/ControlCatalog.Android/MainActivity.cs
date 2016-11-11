using System;
using Android.App;

using Android.OS;
using Android.Content.PM;
using Avalonia.Android.Platform.Specific;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Default;
using Avalonia;

namespace ControlCatalog.Android
{
    [Activity(Label = "ControlCatalog.Android", MainLauncher = true, Icon = "@drawable/icon", LaunchMode = LaunchMode.SingleInstance)]
    public class MainActivity : AvaloniaActivity
    {
        public MainActivity() : base(typeof (App))
        {

        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            App app;
            if (Avalonia.Application.Current != null)
                app = (App)Avalonia.Application.Current;
            else
            {
                app = new App();
                AppBuilder.Configure(app)
                    .UseAndroid()
                    .UseSkia()
                    .SetupWithoutStarting();
            }

            var mainWindow = new MainWindow();
            app.Run(mainWindow);
        }
    }
}

