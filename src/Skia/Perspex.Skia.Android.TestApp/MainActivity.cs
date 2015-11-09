using System;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Util;
using Perspex.Media;
using Perspex.Platform;
using Perspex;

namespace Perspex.Skia.Android.TestApp
{
    [Activity(Label = "Perspex.Skia.Android.TestApp", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(new MainView(this));
        }

        class MainView : SkiaView
        {
            float _radians = 0;
            public MainView(Activity context) : base(context)
            {
            }

            protected override void OnRender(DrawingContext ctx)
            {
                ctx.FillRectangle(Brushes.Green, new Rect(0, 0, Width, Height));

                var rc = new Rect(0, 0, Width/3, Height/3);
                using (ctx.PushPostTransform(
                    Perspex.Matrix.CreateTranslation(-Width/6, -Width/6)*
                    Perspex.Matrix.CreateRotation(_radians)*
                                             Perspex.Matrix.CreateTranslation(Width/2, Height/2)))
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
                    _radians = (e.RawY + e.RawY)/100;
                    Invalidate();
                    return true;
                }
                return base.OnTouchEvent(e);
            }
        }
    }
}

