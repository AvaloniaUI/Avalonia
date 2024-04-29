using System;
using System.Data;
using System.IO;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Rendering.Composition.Transport;
using Avalonia.Utilities;

namespace Avalonia.Rendering.Composition.Server;

internal sealed class ServerCompositionSimpleImageBrush : ServerCompositionSimpleTileBrush, 
    IImageBrush, IImageBrushSource
{
    public IImageBrushSource? Source => this;
    public IRef<IBitmapImpl>? Bitmap { get; private set; }
    
    internal ServerCompositionSimpleImageBrush(ServerCompositor compositor) : base(compositor)
    {
    }

    public override void Dispose()
    {
        Bitmap?.Dispose();
        Bitmap = null;
        base.Dispose();
    }


    protected override void DeserializeChangesCore(BatchStreamReader reader, TimeSpan committedAt)
    {
        base.DeserializeChangesCore(reader, committedAt);
        Bitmap?.Dispose();
        Bitmap = null;
        Bitmap = reader.ReadObject<IRef<IBitmapImpl>>();
    }
}
