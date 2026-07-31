using System;
using System.Collections.Generic;
using Avalonia.Platform;

namespace Avalonia.Rendering.Composition.Server;

internal class ServerVisualRenderContext
{
    public IDrawingContextImpl Canvas { get; }

    public ServerVisualRenderContext(IDrawingContextImpl canvas)
    {
        Canvas = canvas;
    }
}
