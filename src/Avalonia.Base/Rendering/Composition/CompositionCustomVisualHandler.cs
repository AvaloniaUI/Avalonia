using System;
using System.Collections.Generic;
using System.Numerics;
using Avalonia.Collections.Pooled;
using Avalonia.Media;
using Avalonia.Rendering.Composition.Server;

namespace Avalonia.Rendering.Composition;

public abstract class CompositionCustomVisualHandler
{
    private ServerCompositionCustomVisual? _host;
    private bool _inRender;
    private Rect _currentTransformedClip;

    public virtual void OnMessage(object message)
    {
        
    }

    public virtual void OnAnimationFrameUpdate()
    {
        
    }

    internal void Render(ImmediateDrawingContext drawingContext, Rect currentTransformedClip)
    {
        _inRender = true;
        _currentTransformedClip = currentTransformedClip;
        try
        {
            OnRender(drawingContext);
        }
        finally
        {
            _inRender = false;
        }
    }

    public abstract void OnRender(ImmediateDrawingContext drawingContext);

    void VerifyAccess()
    {
        if (_host == null)
            throw new InvalidOperationException("Object is not yet attached to the compositor");
        _host.Compositor.VerifyAccess();
    }

    void VerifyInRender()
    {
        VerifyAccess();
        if (!_inRender)
            throw new InvalidOperationException("This API is only available from OnRender");
    }

    protected Vector EffectiveSize
    {
        get
        {
            VerifyAccess();
            return _host!.Size;
        }
    }

    protected TimeSpan CompositionNow
    {
        get
        {
            VerifyAccess();
            return _host!.Compositor.ServerNow;
        }
    }

    public virtual Rect GetRenderBounds() =>
        new(0, 0, EffectiveSize.X, EffectiveSize.Y);

    internal void Attach(ServerCompositionCustomVisual visual) => _host = visual;

    protected void Invalidate()
    {
        VerifyAccess();
        _host!.HandlerInvalidate();
    }

    protected void Invalidate(Rect rc)
    {
        VerifyAccess();
        _host!.HandlerInvalidate(rc);
    }

    protected void RegisterForNextAnimationFrameUpdate()
    {
        VerifyAccess();
        _host!.HandlerRegisterForNextAnimationFrameUpdate();
    }

    protected bool RenderClipContains(Point pt)
    {
        VerifyInRender();
        pt *= _host!.GlobalTransformMatrix;
        return _currentTransformedClip.Contains(pt) && _host.Root!.DirtyRects.Contains(pt);
    }

    protected bool RenderClipIntersectes(Rect rc)
    {
        VerifyInRender();
        rc = rc.TransformToAABB(_host!.GlobalTransformMatrix);
        return _currentTransformedClip.Intersects(rc) && _host.Root!.DirtyRects.Intersects(new (rc));
    }
}
