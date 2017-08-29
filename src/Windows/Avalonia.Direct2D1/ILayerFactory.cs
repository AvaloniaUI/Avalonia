using System;
using Avalonia.Platform;

namespace Avalonia.Direct2D1
{
    public interface ILayerFactory
    {
        IRenderTargetBitmapImpl CreateLayer(Size size);
    }
}
