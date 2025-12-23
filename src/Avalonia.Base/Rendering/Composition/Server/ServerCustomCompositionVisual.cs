using System;
using System.Collections.Generic;
using System.Numerics;
using Avalonia.Logging;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.Composition.Transport;

namespace Avalonia.Rendering.Composition.Server;

internal sealed class ServerCompositionCustomVisual : ServerCompositionContainerVisual, IServerClockItem
{
    private readonly CompositionCustomVisualHandler _handler;
    private bool _wantsNextAnimationFrameAfterTick;
    internal ServerCompositionCustomVisual(ServerCompositor compositor, CompositionCustomVisualHandler handler) : base(compositor)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        _handler.Attach(this);
    }

    public void DispatchMessages(List<object> messages)
    {
        foreach(var message in messages)
        {
            try
            {
                _handler.OnMessage(message);
            }
            catch (Exception e)
            {
                Logger.TryGet(LogEventLevel.Error, LogArea.Visual)
                    ?.Log(_handler, $"Exception in {_handler.GetType().Name}.{nameof(CompositionCustomVisualHandler.OnMessage)} {{0}}", e);
            }
        }
    }

    public void OnTick()
    {
        _wantsNextAnimationFrameAfterTick = false;
        _handler.OnAnimationFrameUpdate();
        if (!_wantsNextAnimationFrameAfterTick)
            Compositor.Animations.RemoveFromClock(this);
    }

    public override LtrbRect OwnContentBounds => new(_handler.GetRenderBounds());

    protected override void OnAttachedToRoot(ServerCompositionTarget target)
    {
        if (_wantsNextAnimationFrameAfterTick)
            Compositor.Animations.AddToClock(this);
        base.OnAttachedToRoot(target);
    }

    protected override void OnDetachedFromRoot(ServerCompositionTarget target)
    {
        Compositor.Animations.RemoveFromClock(this);
        base.OnDetachedFromRoot(target);
    }

    internal void HandlerInvalidate() => ValuesInvalidated();

    internal void HandlerInvalidate(Rect rc)
    {
        Root?.AddDirtyRect(new LtrbRect(rc).TransformToAABB(GlobalTransformMatrix));
    }
    
    internal void HandlerRegisterForNextAnimationFrameUpdate()
    {
        _wantsNextAnimationFrameAfterTick = true;
        if (Root != null)
            Compositor.Animations.AddToClock(this);
    }

    protected override void RenderCore(ServerVisualRenderContext ctx, LtrbRect currentTransformedClip)
    {
        ctx.Canvas.AutoFlush = true;
        using var context = new ImmediateDrawingContext(ctx.Canvas, GlobalTransformMatrix, false);
        try
        {
            _handler.Render(context, currentTransformedClip.ToRect());
        }
        catch (Exception e)
        {
            Logger.TryGet(LogEventLevel.Error, LogArea.Visual)
                ?.Log(_handler, $"Exception in {_handler.GetType().Name}.{nameof(CompositionCustomVisualHandler.OnRender)} {{0}}", e);
        }

        ctx.Canvas.AutoFlush = false;
    }
}
