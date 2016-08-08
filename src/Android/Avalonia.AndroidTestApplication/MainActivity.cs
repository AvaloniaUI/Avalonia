using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Avalonia.Android;
using Avalonia.Android.Platform.Specific;
using Avalonia.Android.Platform.Specific.Helpers;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Skia.Android;
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

            /*
            App app;
            if (Avalonia.Application.Current != null)
                app = (App)Avalonia.Application.Current;
            else
                app = new App();
           

            MainWindow.RootNamespace = "Avalonia.AndroidTestApplication";
            var window = MainWindow.Create();

            window.Show();
            app.Run(window);
            */



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

            SetContentView(new MainView(this));


        }





        class MainView : SkiaRenderView
        {
            float _radians = 0;
            public MainView(Activity context) : base(context)
            {
            }

            protected override void OnRender(DrawingContext ctx)
            {
                ctx.FillRectangle(Brushes.Green, new Rect(0, 0, Width, Height));

                var rc = new Rect(0, 0, Width / 3, Height / 3);
                using (ctx.PushPostTransform(
                    Avalonia.Matrix.CreateTranslation(-Width / 6, -Width / 6) *
                    Avalonia.Matrix.CreateRotation(_radians) *
                                             Avalonia.Matrix.CreateTranslation(Width / 2, Height / 2)))
                {
                    ctx.FillRectangle(new LinearGradientBrush()
                    {
                        GradientStops =
                        {
                            new GradientStop() {Color = Colors.Blue},
                            new GradientStop(Colors.Red, 1)
                        }
                    }, rc, 5);
                }


            }

            public override bool OnTouchEvent(MotionEvent e)
            {
                if (e.Action == MotionEventActions.Down)
                    return true;
                if (e.Action == MotionEventActions.Move)
                {
                    _radians = (e.RawY + e.RawY) / 100;
                    Invalidate();
                    return true;
                }
                return base.OnTouchEvent(e);
            }
        }










    }
}