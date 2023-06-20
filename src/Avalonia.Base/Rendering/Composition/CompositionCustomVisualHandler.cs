using System;
using System.Numerics;
using Avalonia.Media;
using Avalonia.Rendering.Composition.Server;

namespace Avalonia.Rendering.Composition;

public abstract class CompositionCustomVisualHandler
{
    private ServerCompositionCustomVisual? _host;

    public virtual void OnMessage(object message)
    {
        
    }

    public virtual void OnAnimationFrameUpdate()
    {
        
    }
    
    public abstract void OnRender(ImmediateDrawingContext drawingContext);

    void VerifyAccess()
    {
        if (_host == null)
            throw new InvalidOperationException("Object is not yet attached to the compositor");
        _host.Compositor.VerifyAccess();
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

    protected void RegisterForNextAnimationFrameUpdate()
    {
        VerifyAccess();
        _host!.HandlerRegisterForNextAnimationFrameUpdate();
    }
}
