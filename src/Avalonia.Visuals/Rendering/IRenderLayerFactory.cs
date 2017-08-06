using System;
using Avalonia.Platform;
using Avalonia.VisualTree;

namespace Avalonia.Rendering
{
    public interface IRenderLayerFactory
    {
        IRenderTargetBitmapImpl CreateLayer(IVisual layerRoot, Size size, double dpiX, double dpiY);
    }
}
