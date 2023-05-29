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
    private bool _animationsAreWaitingForComposition;
    private const double MaxSecondsWithoutInput = 1;
    private readonly Action _render;
    private readonly Action _inputMarkerHandler;
    private readonly HashSet<Compositor> _requestedCommits = new();
    private readonly Dictionary<Compositor, Batch> _pendingCompositionBatches = new();
    private record  TopLevelInfo(Compositor Compositor, CompositingRenderer Renderer, ILayoutManager LayoutManager);
    private readonly HashSet<LayoutManager> _queuedLayoutManagers = new();

    private Dictionary<object, TopLevelInfo> _topLevels = new();

    private MediaContext()
    {
        _render = Render;
        _inputMarkerHandler = InputMarkerHandler;
        _clock = new(this);
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
            RenderCore();
        }
        finally
        {
            _nextRenderOp = null;
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
            _clock.HasNewSubscriptions = false;
            //TODO: Integrate LayoutManager's attempt limit here
            foreach (var layout in _queuedLayoutManagers.ToArray())
                layout.ExecuteQueuedLayoutPass();
            _queuedLayoutManagers.Clear();
            
            if (_clock.HasNewSubscriptions)
            {
                _clock.Pulse(now);
                continue;
            }

            break;
        }

        // We are currently using compositor commit callbacks to drive animations
        // Later we should use WPF's approach that asks the animation system for the next tick time
        // and use some timer if the next animation frame is not needed to be sent to the compositor immediately
        if (_requestedCommits.Count > 0 || _clock.HasSubscriptions)
        {
            _animationsAreWaitingForComposition = true;
            CommitCompositorsWithThrottling();
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

    public void QueueLayoutPass(LayoutManager layoutManager)
    {
        _queuedLayoutManagers.Add(layoutManager);
        ScheduleRender(true);
    }
}