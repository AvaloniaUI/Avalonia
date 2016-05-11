using System;
using System.Diagnostics;
using System.Drawing;
using CoreAnimation;
using CoreGraphics;
using CoreMedia;
using Foundation;
using Avalonia.Media;
using Avalonia.Platform;
using UIKit;

namespace Avalonia.Skia.iOS.TestApp
{
    [Register("MainView")]
    public class MainView : SkiaView
    {
        private IRenderTarget _target;
        FormattedText _text;
        public MainView()
        {
            AutoresizingMask = UIViewAutoresizing.All;
            SkiaPlatform.Initialize();
            _target = AvaloniaLocator.Current.GetService<IPlatformRenderInterface>()
                .CreateRenderer(AvaloniaPlatformHandle);
            UpdateText(0);
        }
        double _radians = 0;


        void UpdateText(int fps)
        {
            _text?.Dispose();
            _text = new FormattedText("FPS: " + fps, "Arial", 15, FontStyle.Normal, TextAlignment.Left,
                FontWeight.Normal);
        }

        double _lastFps;
        int _frames;
        Stopwatch St = Stopwatch.StartNew();
        protected override void Draw()
        {
            _radians += 0.02;
            var scale = UIScreen.MainScreen.Scale;
            int width = (int) (Bounds.Width*scale), height = (int) (Bounds.Height*scale);
            using (var ctx = _target.CreateDrawingContext())
            {
                ctx.FillRectangle(Brushes.Green, new Rect(0, 0, width, height));
                ctx.DrawText(Brushes.Red, new Point(50, 50), _text);
                var rc = new Rect(0, 0, width/3, height/3);
                using (ctx.PushPostTransform(
                    Avalonia.Matrix.CreateTranslation(-width/6, -width/6)*
                    Avalonia.Matrix.CreateRotation(_radians)*
                    Avalonia.Matrix.CreateTranslation(width/2, height/2)))
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
            _frames++;
            var now = St.Elapsed.TotalSeconds;
            var elapsed = now - _lastFps;
            if (elapsed > 1)
            {
                UpdateText((int) (_frames/elapsed));
                _frames = 0;
                _lastFps = now;
            }
            DrawOnNextFrame();
        }
    }
}