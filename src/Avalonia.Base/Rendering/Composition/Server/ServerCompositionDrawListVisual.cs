using System;
using System.Numerics;
using Avalonia.Collections.Pooled;
using Avalonia.Platform;
using Avalonia.Rendering.Composition.Drawing;
using Avalonia.Rendering.Composition.Transport;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Utilities;

namespace Avalonia.Rendering.Composition.Server;

/// <summary>
/// Server-side counterpart of <see cref="CompositionDrawListVisual"/>
/// </summary>
internal class ServerCompositionDrawListVisual : ServerCompositionContainerVisual, IServerRenderResourceObserver
{
#if DEBUG
    // This is needed for debugging purposes so we could see inspect the associated visual from debugger
    public readonly Visual UiVisual;
#endif
    private ServerCompositionRenderData? _renderCommands;
    
    public ServerCompositionDrawListVisual(ServerCompositor compositor, Visual v) : base(compositor)
    {
#if DEBUG
        UiVisual = v;
#endif
    }

    public override Rect OwnContentBounds => _renderCommands?.Bounds ?? default;

    protected override void DeserializeChangesCore(BatchStreamReader reader, TimeSpan committedAt)
    {
        if (reader.Read<byte>() == 1)
        {
            _renderCommands?.Dispose();
            _renderCommands = reader.ReadObject<ServerCompositionRenderData?>();
            _renderCommands?.AddObserver(this);
        }
        base.DeserializeChangesCore(reader, committedAt);
    }

    protected override void RenderCore(CompositorDrawingContextProxy canvas, Rect currentTransformedClip)
    {
        if (_renderCommands != null)
        {
            _renderCommands.Render(canvas);
        }
        base.RenderCore(canvas, currentTransformedClip);
    }
    
    public void DependencyQueuedInvalidate(IServerRenderResource sender) => ValuesInvalidated();
    
#if DEBUG
    public override string ToString()
    {
        return UiVisual.GetType().ToString();
    }
#endif
}
