using System;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Util;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia;

namespace Avalonia.Skia.Android.TestApp
{
    [Activity(Label = "Avalonia.Skia.Android.TestApp", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
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

                var rc = new Rect(0, 0, Width/3, Height/3);
                using (ctx.PushPostTransform(
                    Avalonia.Matrix.CreateTranslation(-Width/6, -Width/6)*
                    Avalonia.Matrix.CreateRotation(_radians)*
                                             Avalonia.Matrix.CreateTranslation(Width/2, Height/2)))
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

