using Android.App;
using Android.Content.PM;
using Android.OS;
using Perspex.Android;
using Perspex.Android.Platform.Specific;
using Perspex.Android.Platform.Specific.Helpers;
using Perspex.Controls.Platform;
using Perspex.Platform;

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
            //skia is the default rendering method on android so no need to set it
            AndroidPlatform.Instance.DefaultViewDrawType = ViewDrawType.Skia;
            //AndroidPlatform.Instance.DefaultViewDrawType = ViewDrawType.BitmapOnPreDraw;
            //AndroidPlatform.Instance.DefaultViewDrawType = ViewDrawType.CanvasOnDraw;
            //AndroidPlatform.Instance.DefaultViewDrawType = ViewDrawType.BitmapBackgroundRender;
            //AndroidPlatform.Instance.DefaultViewDrawType = ViewDrawType.SurfaceViewCanvasOnDraw;
            //AndroidPlatform.Instance.DefaultPointUnit = Android.Platform.CanvasPlatform.PointUnit.DP;
            //AndroidPlatform.Instance.DefaultPointUnit = Android.Platform.CanvasPlatform.PointUnit.Pixel;

            //60 fps animation are causing user interface in animations to stop responding
            AndroidPlatform.Instance.OverrideAnimateFramesPerSecond = 16;

            App app;
            if (Perspex.Application.Current != null)
                app = (App)Perspex.Application.Current;
            else
                app = new App();

            if (AndroidPlatform.Instance.DefaultPointUnit == Android.Platform.CanvasPlatform.PointUnit.DP &&
                AndroidPlatform.Instance.DefaultViewDrawType == ViewDrawType.Skia)
            {
                double scale = Resources.DisplayMetrics.ScaledDensity;
                //make it DiP in skia
                PerspexLocator.Current.GetService<PlatformSettings>().LayoutScalingFactor = scale;
                PerspexLocator.Current.GetService<PlatformSettings>().RenderScalingFactor = scale;
            }

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