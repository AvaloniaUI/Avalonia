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

    protected Vector2 EffectiveSize => _host?.Size ?? default;

    protected TimeSpan CompositionNow => _host?.Compositor.ServerNow ?? default;
    
    public virtual Rect GetRenderBounds() =>
        new(0, 0, EffectiveSize.X, EffectiveSize.Y);

    internal void Attach(ServerCompositionCustomVisual visual) => _host = visual;

    protected void Invalidate() => _host?.HandlerInvalidate();

    protected void RegisterForNextAnimationFrameUpdate() => _host?.HandlerRegisterForNextAnimationFrameUpdate();
}
