using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Animation;
using Avalonia.Layout;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Transport;
using Avalonia.Threading;

namespace Avalonia.Media;

internal partial class MediaContext : ICompositorScheduler
{
    private DispatcherOperation? _nextRenderOp;
    private DispatcherOperation? _inputMarkerOp;
    private TimeSpan _inputMarkerAddedAt;
    private bool _isRendering;
    private bool _animationsAreWaitingForComposition;
    private const double MaxSecondsWithoutInput = 1;
    private readonly Action _render;
    private readonly Action _inputMarkerHandler;
    private readonly HashSet<Compositor> _requestedCommits = new();
    private readonly Dictionary<Compositor, Batch> _pendingCompositionBatches = new();
    private record  TopLevelInfo(Compositor Compositor, CompositingRenderer Renderer, ILayoutManager LayoutManager);

    private List<Action>? _invokeOnRenderCallbacks;
    private readonly Stack<List<Action>> _invokeOnRenderCallbackListPool = new();

    private DispatcherTimer _animationsTimer = new(DispatcherPriority.Render)
    {
        // Since this timer is used to drive animations that didn't contribute to the previous frame at all
        // We can safely use 16ms interval until we fix our animation system to actually report the next expected 
        // frame
        Interval = TimeSpan.FromMilliseconds(16)
    };

    private Dictionary<object, TopLevelInfo> _topLevels = new();

    private MediaContext()
    {
        _render = Render;
        _inputMarkerHandler = InputMarkerHandler;
        _clock = new(this);
        _animationsTimer.Tick += (_, _) =>
        {
            _animationsTimer.Stop();
            ScheduleRender(false);
        };
    }
    
    public static MediaContext Instance
    {
        get
        {
            // Technically it's supposed to be a thread-static singleton, but we don't have multiple threads
            // and need to do a full reset for unit tests
            var context = AvaloniaLocator.Current.GetService<MediaContext>();
            if (context == null)
                AvaloniaLocator.CurrentMutable.Bind<MediaContext>().ToConstant(context = new());
            return context;
        }
    }
    
    /// <summary>
    /// Schedules the next render operation, handles render throttling for input processing
    /// </summary>
    private void ScheduleRender(bool now)
    {
        // Already scheduled, nothing to do
        if (_nextRenderOp != null)
        {
            if (now)
                _nextRenderOp.Priority = DispatcherPriority.Render;
            return;
        }
        // Sometimes our animation, layout and render passes might be taking more than a frame to complete
        // which can cause a "freeze"-like state when UI is being updated, but input is never being processed
        // So here we inject an operation with Input priority to check if Input wasn't being processed
        // for a long time. If that's the case the next rendering operation will be scheduled to happen after all pending input
        
        var priority = DispatcherPriority.Render;
        
        if (_inputMarkerOp == null)
        {
            _inputMarkerOp = Dispatcher.UIThread.InvokeAsync(_inputMarkerHandler, DispatcherPriority.Input);
            _inputMarkerAddedAt = _time.Elapsed;
        }
        else if (!now && (_time.Elapsed - _inputMarkerAddedAt).TotalSeconds > MaxSecondsWithoutInput)
        {
            priority = DispatcherPriority.Input;
        }
        

        _nextRenderOp = Dispatcher.UIThread.InvokeAsync(_render, priority);
    }
    
    /// <summary>
    /// This handles the _inputMarkerOp message.  We're using
    /// _inputMarkerOp to determine if input priority dispatcher ops
    /// have been processes.
    /// </summary>
    private void InputMarkerHandler()
    {
        //set the marker to null so we know that input priority has been processed
        _inputMarkerOp = null;
    }

    private void Render()
    {
        try
        {
            _isRendering = true;
            RenderCore();
        }
        finally
        {
            _nextRenderOp = null;
            _isRendering = false;
        }
    }
    
    private void RenderCore()
    {
        var now = _time.Elapsed;
        if (!_animationsAreWaitingForComposition)
            _clock.Pulse(now);

        // Since new animations could be started during the layout and can affect layout/render
        // We are doing several iterations when it happens
        for (var c = 0; c < 10; c++)
        {
            FireInvokeOnRenderCallbacks();
            
            if (_clock.HasNewSubscriptions)
            {
                _clock.PulseNewSubscriptions();
                continue;
            }

            break;
        }
        
        if (_requestedCommits.Count > 0 || _clock.HasSubscriptions)
        {
            _animationsAreWaitingForComposition = CommitCompositorsWithThrottling();
            if (!_animationsAreWaitingForComposition && _clock.HasSubscriptions) 
                _animationsTimer.Start();
        }
    }

    // Used for unit tests
    public bool IsTopLevelActive(object key) => _topLevels.ContainsKey(key);

    public void AddTopLevel(object key, ILayoutManager layoutManager, IRenderer renderer)
    {
        if(_topLevels.ContainsKey(key))
            return;
        var render = (CompositingRenderer)renderer;
        _topLevels.Add(key, new TopLevelInfo(render.Compositor, render, layoutManager));
        render.Start();
        ScheduleRender(true);
    }

    public void RemoveTopLevel(object key)
    {
        if (_topLevels.TryGetValue(key, out var info))
        {
            _topLevels.Remove(key);
            info.Renderer.Stop();
        }
    }

    /// <summary>
    /// Calls all _invokeOnRenderCallbacks until no more are added
    /// </summary>
    private void FireInvokeOnRenderCallbacks()
    {
        int callbackLoopCount = 0;
        int count = _invokeOnRenderCallbacks?.Count ?? 0;

        // This outer loop is to re-run layout in case the app causes a layout to get enqueued in response
        // to a Loaded event. In this case we would like to re-run layout before we allow render.
        do
        {
            while (count > 0)
            {
                callbackLoopCount++;
                if (callbackLoopCount > 153)
                    throw new InvalidOperationException("Infinite layout loop detected");

                var callbacks = _invokeOnRenderCallbacks!;
                _invokeOnRenderCallbacks = null;

                for (int i = 0; i < count; i++) 
                    callbacks[i].Invoke();
                
                callbacks.Clear();
                _invokeOnRenderCallbackListPool.Push(callbacks);

                count = _invokeOnRenderCallbacks?.Count ?? 0;
            }

            // TODO: port the rest of the Loaded logic later
            // Fire all the pending Loaded events before Render happens
            // but after the layout storm has subsided
            // FireLoadedPendingCallbacks();

            count =  _invokeOnRenderCallbacks?.Count ?? 0;
        }
        while (count > 0);
    }

    /// <summary>
    /// Executes the <paramref name="callback">callback</paramref> in the next iteration of the current UI-thread
    /// render loop / layout pass that.
    /// </summary>
    /// <param name="callback"></param>
    public void BeginInvokeOnRender(Action callback)
    {
        if (_invokeOnRenderCallbacks == null)
            _invokeOnRenderCallbacks =
                _invokeOnRenderCallbackListPool.Count > 0 ? _invokeOnRenderCallbackListPool.Pop() : new();
        
        _invokeOnRenderCallbacks.Add(callback);

        if (!_isRendering) 
            ScheduleRender(true);
    }
}
