using System;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.Runtime;
using Android.Widget;
using Android.OS;
using Perspex.Android;
using Perspex.Android.Rendering;
using Perspex.Controls;
using Perspex.Media;
using APoint = Android.Graphics.Point;
using Window = Android.Views.Window;

namespace Perspex.AndroidTestApplication
{
    [Activity(Label = "Main", MainLauncher = true, Icon = "@drawable/icon", LaunchMode = LaunchMode.SingleInstance )]
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

