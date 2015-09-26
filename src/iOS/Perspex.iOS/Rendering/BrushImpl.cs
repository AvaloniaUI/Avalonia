using CoreGraphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace Perspex.iOS.Rendering
{
    public enum BrushUsage
    {
        Fill,
        Stroke,
        Both
    };

    public abstract class BrushImpl : IDisposable
    {
        //public CGPattern PlatformBrush { get; set; }

        public virtual void Dispose()
        {
            //if (PlatformBrush != null)
            //{
            //    PlatformBrush.Dispose();
            //}
        }

        public abstract void Apply(CGContext context, BrushUsage usage);
    }

    public class SolidColorBrushImpl : BrushImpl
    {
        CGColor _nativeColor;

        public SolidColorBrushImpl(Perspex.Media.SolidColorBrush brush, double opacityOverride = 1.0f)
        {
            _nativeColor = brush?.Color.ToCoreGraphics() ?? new CGColor(0, 0, 0);

            //if (brush != null)
            //    _nativeColor.A = Math.Min(brush.Opacity, color.A);

            //if (opacityOverride < 1.0f)
            //    _nativeColor.A = Math.Min(opacityOverride, color.A);

            //this.PlatformBrush = new SolidPattern(color);
        }

        public override void Apply(CGContext context, BrushUsage usage)
        {
            if (usage == BrushUsage.Fill || usage == BrushUsage.Both)
            {
                context.SetFillColor(_nativeColor);
            }

            if (usage == BrushUsage.Stroke || usage == BrushUsage.Both)
            {
                context.SetStrokeColor(_nativeColor);
            }
        }

    }
}
