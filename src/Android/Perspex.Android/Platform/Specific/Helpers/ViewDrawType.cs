using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace Perspex.Android.Platform.Specific.Helpers
{
    public enum ViewDrawType
    {
        Skia,
        CanvasOnDraw,
        BitmapOnPreDraw,
        BitmapBackgroundRender,
        SurfaceViewCanvasOnDraw
    }
}