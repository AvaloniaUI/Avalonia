using System;
using Avalonia.Platform;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Rendering.Composition.Transport;
using Avalonia.Utilities;

namespace Avalonia.Rendering.Composition.Drawing;

class ServerCompositionRenderData : SimpleServerRenderResource
{
    private RenderDataStream? _stream;
    private PooledInlineList<IServerRenderResource> _referencedResources;
    private LtrbRect? _bounds;
    private bool _boundsValid;

    public ServerCompositionRenderData(ServerCompositor compositor) : base(compositor)
    {
    }

    protected override void DeserializeChangesCore(BatchStreamReader reader, TimeSpan committedAt)
    {
        Reset();

        _stream = new RenderDataStream();
        _stream.DeserializeFrom(reader);

        for (var i = 0; i < _stream.ResourceCount; i++)
        {
            if (_stream.GetResource(i) is IServerRenderResource resource)
            {
                _referencedResources.Add(resource);
                resource.AddObserver(this);
            }
        }

        base.DeserializeChangesCore(reader, committedAt);
    }

    public LtrbRect? Bounds
    {
        get
        {
            if (!_boundsValid)
            {
                _bounds = CalculateRenderBounds();
                _boundsValid = true;
            }
            return _bounds;
        }
    }

    private LtrbRect? CalculateRenderBounds()
    {
        var bounds = _stream?.CalculateBounds();
        return bounds.HasValue ? ApplyRenderBoundsRounding(new LtrbRect(bounds.Value)) : null;
    }

    public static Rect? ApplyRenderBoundsRounding(Rect? rect)
    {
        if (rect == null)
            return null;
        return ApplyRenderBoundsRounding(new LtrbRect(rect.Value))?.ToRect();
    }

    public static LtrbRect? ApplyRenderBoundsRounding(LtrbRect? rect)
    {
        if (rect != null)
        {
            var r = rect.Value;
            // I don't believe that it's correct to do here (rather than in CompositionVisual),
            // but it's the old behavior, so I'm keeping it for now
            return new LtrbRect(Math.Floor(r.Left), Math.Floor(r.Top),
                Math.Ceiling(r.Right), Math.Ceiling(r.Bottom));
        }

        return null;
    }

    public override void DependencyQueuedInvalidate(IServerRenderResource sender)
    {
        _boundsValid = false;
        base.DependencyQueuedInvalidate(sender);
    }

    public void Render(IDrawingContextImpl context) => _stream?.Replay(context);

    private void Reset()
    {
        _bounds = null;
        _boundsValid = false;

        foreach (var r in _referencedResources)
            r.RemoveObserver(this);
        _referencedResources.Dispose();

        if (_stream != null)
        {
            _stream.DisposeResources();
            _stream.Dispose();
            _stream = null;
        }
    }

    public override void Dispose()
    {
        Reset();
        base.Dispose();
    }
}
