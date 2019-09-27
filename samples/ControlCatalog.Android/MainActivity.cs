using System;
using Android.App;
using Android.OS;
using Android.Content.PM;
using Avalonia.Android;
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
        protected override void OnCreate(Bundle savedInstanceState)
        {
            if (Avalonia.Application.Current == null)           
            {
                AppBuilder.Configure<App>()
                    .UseAndroid()
                    .SetupWithoutStarting();
                Content = new MainView();
            }
            base.OnCreate(savedInstanceState);
        }
    }
}

