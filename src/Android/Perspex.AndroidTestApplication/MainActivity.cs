using Android.App;
using Android.Content.PM;
using Android.OS;
using Perspex.Android;
using Perspex.Android.Platform.Specific;
using Perspex.Android.Platform.Specific.Helpers;
using Perspex.Controls;
using Perspex.Controls.Presenters;
using Perspex.Controls.Primitives;
using Perspex.Controls.Templates;
using Perspex.Markup.Xaml.Data;
using Perspex.Platform;
using Perspex.Styling;
using ReactiveUI;
using System;
using System.Reactive.Linq;
using System.Windows.Input;

namespace Perspex.AndroidTestApplication
{
    [Activity(Label = "Main",
        MainLauncher = true,
        Icon = "@drawable/icon",
        LaunchMode = LaunchMode.SingleInstance/*,
        ScreenOrientation = ScreenOrientation.Landscape*/)]
    public class MainBaseActivity : PerspexActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            //set some parameters to android platform
            AndroidPlatform.Instance.DrawDebugInfo = true;
            //AndroidPlatform.Instance.DefaultViewDrawType = ViewDrawType.BitmapOnPreDraw;
            AndroidPlatform.Instance.DefaultViewDrawType = ViewDrawType.CanvasOnDraw;
            //AndroidPlatform.Instance.DefaultViewDrawType = ViewDrawType.BitmapBackgroundRender;
            //AndroidPlatform.Instance.DefaultViewDrawType = ViewDrawType.SurfaceViewCanvasOnDraw;
            AndroidPlatform.Instance.DefaultPointUnit = Android.Platform.CanvasPlatform.PointUnit.DP;
            //AndroidPlatform.Instance.DefaultPointUnit = Android.Platform.CanvasPlatform.PointUnit.Pixel;
            AndroidPlatform.Instance.OverrideAnimateFramesPerSecond = 16;

            App app;
            if (Perspex.Application.Current != null)
                app = (App)Perspex.Application.Current;
            else
                app = new App();
            
            var window = TestUI.TestUIBuilder.BuildTestUI();
            //var window = BuildPlatforSetup();

            window.Show();
            app.Run(window);
        }
        public static void StartAppFromSetup()
        {
            PerspexLocator.Current.GetService<IWindowImpl>().Dispose();

            AndroidPlatform.Instance.RegisterViewDrawType();
            AndroidPlatform.Instance.RegisterViewPointUnits();

            var window = TestUI.TestUIBuilder.BuildTestUI();

            window.Show();
            Perspex.Application.Current.Run(window);
        }
    }
}