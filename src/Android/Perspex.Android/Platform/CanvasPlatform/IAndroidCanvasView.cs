using Android.Graphics;
using Perspex.Android.Platform.Specific;
using System;
using System.Collections.Generic;

namespace Perspex.Android.Platform.CanvasPlatform
{
    public interface IAndroidCanvasView : IAndroidView
    {
        Canvas Canvas { get; }

        Dictionary<object, IDisposable> VisualCaches { get; }
    }
}