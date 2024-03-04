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
    private PooledList<Rect>? _dirtyRects;
    private bool _inRender;

    public virtual void OnMessage(object message)
    {
        
    }

    public virtual void OnAnimationFrameUpdate()
    {
        
    }

    internal void Render(ImmediateDrawingContext drawingContext)
    {
        _inRender = true;
        try
        {
            OnRender(drawingContext);
        }
        finally
        {
            _inRender = false;
        }

        _dirtyRects?.Dispose();
        _dirtyRects = null;
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

    protected IList<Rect> DirtyRects
    {
        get
        {
            VerifyInRender();
            
            if (_host?.Root == null)
                return Array.Empty<Rect>();
            if (_dirtyRects == null)
            {
                if (!_host.GlobalTransformMatrix.TryInvert(out var inverted))
                    return Array.Empty<Rect>();
                
                _dirtyRects = new();
                foreach (var r in _host.Root.ThisFrameDirtyRects)
                {
                    _dirtyRects.Add(r.ToRectWithDpi(1).TransformToAABB(inverted));
                }
            }
            return _dirtyRects;
        }
    }
}
