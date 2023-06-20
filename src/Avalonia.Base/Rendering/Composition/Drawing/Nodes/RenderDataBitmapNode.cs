using System;
using Avalonia.Platform;
using Avalonia.Utilities;

namespace Avalonia.Rendering.Composition.Drawing.Nodes;

class RenderDataBitmapNode : IRenderDataItem, IDisposable
{
    public IRef<IBitmapImpl>? Bitmap { get; set; }
    public double Opacity { get; set; }
    public Rect SourceRect { get; set; }
    public Rect DestRect { get; set; }
    
    public bool HitTest(Point p) => DestRect.Contains(p);

    public void Invoke(ref RenderDataNodeRenderContext context)
    {
        if (Bitmap != null)
            context.Context.DrawBitmap(Bitmap.Item, Opacity, SourceRect, DestRect);
    }

    public Rect? Bounds => DestRect;
    public void Dispose()
    {
        Bitmap?.Dispose();
        Bitmap = null;
    }
}