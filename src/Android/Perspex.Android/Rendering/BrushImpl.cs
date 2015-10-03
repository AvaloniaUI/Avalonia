using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
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
        private AColor _nativeColor;

        public SolidColorBrushImpl(Media.SolidColorBrush brush)
        {
            _nativeColor = brush?.Color.ToAndroidGraphics() ?? new AColor(0,0,0,255);
        }

        public override void Apply(Paint context, BrushUsage usage)
        {
            context.Color = _nativeColor;
            context.SetStyle(usage.ToAndroidGraphics());
        }
    }
}