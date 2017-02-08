using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Avalonia.Controls.Platform.Surfaces;
using Avalonia.Platform;
using Foundation;
using UIKit;

namespace Avalonia.Skia
{
    partial class PlatformRenderInterface
    {
        public IRenderTarget CreateRenderTarget(IEnumerable<object> surfaces)
        {
            var fb = surfaces?.OfType<IFramebufferPlatformSurface>().FirstOrDefault();
            if (fb == null)
                throw new Exception("Avalonia.Skia.Deskop currently only supports framebuffer render target");
            return new FramebufferRenderTarget(fb);
        }
    }
}