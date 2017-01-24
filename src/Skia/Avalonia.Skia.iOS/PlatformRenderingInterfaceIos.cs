using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Avalonia.Platform;
using Foundation;
using UIKit;

namespace Avalonia.Skia
{
    partial class PlatformRenderInterface
    {
        public IRenderTarget CreateRenderTarget(IEnumerable<object> surfaces)
        {
            return new WindowRenderTarget();
        }
    }
}