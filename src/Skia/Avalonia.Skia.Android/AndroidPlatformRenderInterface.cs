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
using Avalonia.Platform;

namespace Avalonia.Skia
{
    partial class PlatformRenderInterface
    {
        public IRenderTarget CreateRenderTarget(IEnumerable<object> surfaces)
        {
            var surfaceView = surfaces?.OfType<SurfaceView>().FirstOrDefault();
            if (surfaceView == null)
                throw new ArgumentException("Avalonia.Skia.Android is only capable to draw on SurfaceView");
            return new WindowRenderTarget(surfaceView);
        }
    }
}