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
using Avalonia.Controls.Platform.Surfaces;
using Avalonia.Platform;

namespace Avalonia.Skia
{
    partial class PlatformRenderInterface
    {
        public IRenderTarget CreateRenderTarget(IEnumerable<object> surfaces)
        {
            var fb = surfaces?.OfType<IFramebufferPlatformSurface>().FirstOrDefault();
            if (fb == null)
                throw new ArgumentException("Avalonia.Skia.Android is only capable of drawing on framebuffer");
            return new FramebufferRenderTarget(fb);
        }
    }
}