using System;
using System.Drawing;
using CoreAnimation;
using CoreGraphics;
using Foundation;
using Perspex.Media;
using Perspex.Platform;
using UIKit;

namespace Perspex.Skia.iOS.TestApp
{
    [Register("MainView")]
    public class MainView : UIView
    {
        CAEAGLLayer _layer = new CAEAGLLayer();
        private IRenderTarget _target;

        public MainView()
        {
            Initialize();
        }

        public MainView(RectangleF bounds) : base(bounds)
        {
            Initialize();
        }
        

        void Initialize()
        {
            AutoresizingMask = UIViewAutoresizing.All;
            Layer.AddSublayer(_layer);
            
            SkiaPlatform.Initialize();
            _target = PerspexLocator.Current.GetService<IPlatformRenderInterface>()
                .CreateRenderer(new PlatformHandle(_layer.DangerousRetain().Handle, "Layer"));
        }

        double _radians = 0;

        public override void TouchesMoved(NSSet touches, UIEvent evt)
        {
            foreach (var touch in touches)
            {
                var loc = ((UITouch) touch).LocationInView(this);
                _radians = (loc.X + loc.Y)/100;
            }
            SetNeedsDisplay();
            base.TouchesMoved(touches, evt);
        }

        public override void Draw(CGRect rect)
        {
            _layer.Bounds = new CGRect(0, 0, Bounds.Width, Bounds.Height);
            var scale = UIScreen.MainScreen.Scale;
            int width = (int) (Bounds.Width*scale), height = (int) (Bounds.Height*scale);
            using (var ctx = _target.CreateDrawingContext())
            {
                ctx.FillRectangle(Brushes.Green, new Rect(0, 0, width, height));

                var rc = new Rect(0, 0, width/3, height/3);
                using (ctx.PushPostTransform(
                    Perspex.Matrix.CreateTranslation(-width/6, -width/6)*
                    Perspex.Matrix.CreateRotation(_radians)*
                    Perspex.Matrix.CreateTranslation(width/2, height/2)))
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

        }
    }
}