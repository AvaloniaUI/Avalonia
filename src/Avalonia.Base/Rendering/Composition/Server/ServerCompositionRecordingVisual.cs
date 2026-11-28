using System;
using Avalonia.Platform;
using Avalonia.Rendering.Composition.Drawing;
using Avalonia.Rendering.Composition.Transport;

namespace Avalonia.Rendering.Composition.Server;

/// <summary>
/// Server-side counterpart of <see cref="CompositionRecordingVisual"/>: renders
/// the render data of a compositor-bound <see cref="DrawingRecording"/>.
/// </summary>
internal class ServerCompositionRecordingVisual : ServerCompositionContainerVisual, IServerRenderResourceObserver
{
    private ServerCompositionRenderData? _renderData;

    public ServerCompositionRecordingVisual(ServerCompositor compositor) : base(compositor)
    {
    }

    public override LtrbRect? ComputeOwnContentBounds() => _renderData?.Bounds;

    protected override void DeserializeChangesCore(BatchStreamReader reader, TimeSpan committedAt)
    {
        if (reader.Read<byte>() == 1)
        {
            // The recording owns its render data; the visual only observes it.
            _renderData?.RemoveObserver(this);
            _renderData = reader.ReadObject<ServerCompositionRenderData?>();
            _renderData?.AddObserver(this);
            InvalidateContent();
        }

        base.DeserializeChangesCore(reader, committedAt);
    }

    protected override void RenderCore(ServerVisualRenderContext context, LtrbRect currentTransformedClip)
    {
        _renderData?.Render(context.Canvas);
    }

    public void DependencyQueuedInvalidate(IServerRenderResource sender) => InvalidateContent();
}
