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
    private bool _hasChildClip;
    private RoundedRect _childClip;
    private IGeometryImpl? _childClipGeometry;
    
    public ServerCompositionDrawListVisual(ServerCompositor compositor, Visual v) : base(compositor)
    {
#if DEBUG
        UiVisual = v;
#endif
    }

    public override LtrbRect? ComputeOwnContentBounds() => _renderCommands?.Bounds;

    public bool HasChildClip => _hasChildClip;
    public RoundedRect ChildClip => _childClip;
    public IGeometryImpl? ChildClipGeometry => _childClipGeometry;

    protected override void DeserializeChangesCore(BatchStreamReader reader, TimeSpan committedAt)
    {
        if (reader.Read<byte>() == 1)
        {
            _renderCommands?.Dispose();
            _renderCommands = reader.ReadObject<ServerCompositionRenderData?>();
            _renderCommands?.AddObserver(this);
            InvalidateContent();
        }
        if (reader.Read<byte>() == 1)
        {
            _hasChildClip = reader.Read<bool>();
            _childClip = reader.Read<RoundedRect>();
            _childClipGeometry = reader.ReadObject<IGeometryImpl?>();
            InvalidateContent();
        }
        base.DeserializeChangesCore(reader, committedAt);
    }

    protected override void RenderCore(ServerVisualRenderContext context, LtrbRect currentTransformedClip)
    {
        _renderCommands?.Render(context.Canvas);
    }

    public void DependencyQueuedInvalidate(IServerRenderResource sender) => InvalidateContent();
    
#if DEBUG
    public override string ToString()
    {
        return UiVisual.GetType().ToString();
    }
#endif
}
