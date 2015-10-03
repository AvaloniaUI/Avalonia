using System;
using Android.Graphics;
using Perspex.Media;
using AColor = Android.Graphics.Color;

namespace Perspex.Android.Rendering
{
    public enum BrushUsage
    {
        Fill,
        Stroke,
        Both
    };

    public abstract class BrushImpl : IDisposable
    {
        public virtual void Dispose()
        {
        }

        public abstract void Apply(Paint context, BrushUsage usage);
    }

    public class SolidColorBrushImpl : BrushImpl
    {
        private readonly AColor _nativeColor;

        public SolidColorBrushImpl(SolidColorBrush brush)
        {
            _nativeColor = brush?.Color.ToAndroidGraphics() ?? new AColor(0, 0, 0, 255);
        }

        public override void Apply(Paint context, BrushUsage usage)
        {
            context.Color = _nativeColor;
            context.SetStyle(usage.ToAndroidGraphics());
        }
    }
}