using System;
using Avalonia.Platform;

namespace Avalonia.Direct2D1
{
    internal interface ICreateLayer
    {
        IRenderTargetBitmapImpl CreateLayer(int pixelWidth, int pixelHeight);
    }
}
