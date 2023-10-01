using System;
using System.Numerics;
using Avalonia.Logging;
using Avalonia.Media;
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

    protected override void DeserializeChangesCore(BatchStreamReader reader, TimeSpan committedAt)
    {
        base.DeserializeChangesCore(reader, committedAt);
        var count = reader.Read<int>();
        for (var c = 0; c < count; c++)
        {
            try
            {
                _handler.OnMessage(reader.ReadObject()!);
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
            Compositor.RemoveFromClock(this);
    }

    public override Rect OwnContentBounds => _handler.GetRenderBounds();

    protected override void OnAttachedToRoot(ServerCompositionTarget target)
    {
        if (_wantsNextAnimationFrameAfterTick)
            Compositor.AddToClock(this);
        base.OnAttachedToRoot(target);
    }

    protected override void OnDetachedFromRoot(ServerCompositionTarget target)
    {
        Compositor.RemoveFromClock(this);
        base.OnDetachedFromRoot(target);
    }

    internal void HandlerInvalidate() => ValuesInvalidated();
    
    internal void HandlerRegisterForNextAnimationFrameUpdate()
    {
        _wantsNextAnimationFrameAfterTick = true;
        if (Root != null)
            Compositor.AddToClock(this);
    }

    protected override void RenderCore(CompositorDrawingContextProxy canvas, Rect currentTransformedClip)
    {
        using var context = new ImmediateDrawingContext(canvas, false);
        try
        {
            _handler.OnRender(context);
        }
        catch (Exception e)
        {
            Logger.TryGet(LogEventLevel.Error, LogArea.Visual)
                ?.Log(_handler, $"Exception in {_handler.GetType().Name}.{nameof(CompositionCustomVisualHandler.OnRender)} {{0}}", e);
        }
    }
}
