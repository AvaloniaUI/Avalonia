using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using Avalonia.Diagnostics;
using Avalonia.Logging;
using Avalonia.Media;
using Avalonia.Rendering;
using Avalonia.Threading;
using Avalonia.Utilities;
using Avalonia.VisualTree;

namespace Avalonia.Layout
{
    /// <summary>
    /// Manages measuring and arranging of controls.
    /// </summary>
    internal class LayoutManager : IBringIntoViewLayoutManager
    {
        private const int MaxPasses = 10;
        private readonly ILayoutRoot _owner;
        private readonly LayoutQueue<Layoutable> _toMeasure = new LayoutQueue<Layoutable>(v => !v.IsMeasureValid);
        private readonly LayoutQueue<Layoutable> _toArrange = new LayoutQueue<Layoutable>(v => !v.IsArrangeValid);
        private readonly List<Layoutable> _toArrangeAfterMeasure = new();
        private List<EffectiveViewportChangedListener>? _effectiveViewportChangedListeners;
        private List<BringIntoViewRequest>? _bringIntoViewRequests;
        private bool _disposed;
        private bool _queued;
        private bool _running;
        private bool _processingBringIntoViewRequests;
        private int _totalPassCount;
        private readonly Action _invokeOnRender;

        public LayoutManager(ILayoutRoot owner)
        {
            _owner = owner;
            _invokeOnRender = ExecuteQueuedLayoutPass;
        }

        public virtual event EventHandler? LayoutUpdated;

        public bool IsInLayoutPass => _running;

        internal Action<LayoutPassTiming>? LayoutPassTimed { get; set; }

        /// <inheritdoc/>
        public virtual void InvalidateMeasure(Layoutable control)
        {
            control = control ?? throw new ArgumentNullException(nameof(control));
            Dispatcher.UIThread.VerifyAccess();

            if (_disposed)
            {
                return;
            }

            if (!control.IsAttachedToVisualTree)
            {
#if DEBUG
                throw new AvaloniaInternalException(
                    "LayoutManager.InvalidateMeasure called on a control that is detached from the visual tree.");
#else
                return;
#endif
            }

            if (control.GetLayoutRoot() != _owner)
            {
                throw new ArgumentException("Attempt to call InvalidateMeasure on wrong LayoutManager.");
            }

            _toMeasure.Enqueue(control);
            QueueLayoutPass();
        }

        /// <inheritdoc/>
        public virtual void InvalidateArrange(Layoutable control)
        {
            control = control ?? throw new ArgumentNullException(nameof(control));
            Dispatcher.UIThread.VerifyAccess();

            if (_disposed)
            {
                return;
            }

            if (!control.IsAttachedToVisualTree)
            {
#if DEBUG
                throw new AvaloniaInternalException(
                    "LayoutManager.InvalidateArrange called on a control that is detached from the visual tree.");
#else
                return;
#endif
            }

            if (control.GetLayoutRoot() != _owner)
            {
                throw new ArgumentException("Attempt to call InvalidateArrange on wrong LayoutManager.");
            }

            _toArrange.Enqueue(control);
            QueueLayoutPass();
        }

        internal void ExecuteQueuedLayoutPass()
        {
            if (!_queued)
            {
                return;
            }
            
            ExecuteLayoutPass();
        }

        /// <inheritdoc/>
        public virtual void ExecuteLayoutPass()
        {
            Dispatcher.UIThread.VerifyAccess();

            if (_disposed)
            {
                return;
            }

            if (!_running)
            {
                const LogEventLevel timingLogLevel = LogEventLevel.Information;
                var captureTiming = LayoutPassTimed is not null || Logger.IsEnabled(timingLogLevel, LogArea.Layout);
                var startingTimestamp = 0L;

                if (captureTiming)
                {
                    Logger.TryGet(timingLogLevel, LogArea.Layout)?.Log(
                        this,
                        "Started layout pass. To measure: {Measure} To arrange: {Arrange}",
                        _toMeasure.Count,
                        _toArrange.Count);

                    startingTimestamp = Stopwatch.GetTimestamp();
                }

                _toMeasure.BeginLoop(MaxPasses);
                _toArrange.BeginLoop(MaxPasses);

                try
                {
                    _running = true;
                    ++_totalPassCount;

                    for (var pass = 0; pass < MaxPasses; ++pass)
                    {
                        InnerLayoutPass();

                        if (RaiseEffectiveViewportChanged())
                        {
                            continue;
                        }

                        // The layout is now stable: bring-into-view requests can execute against final bounds.
                        // Executing them typically changes scroll offsets, which invalidates layout again.
                        if (!ProcessBringIntoViewRequests() || (_toMeasure.Count == 0 && _toArrange.Count == 0))
                        {
                            break;
                        }
                    }
                }
                finally
                {
                    _running = false;
                }

                _toMeasure.EndLoop();
                _toArrange.EndLoop();

                if (captureTiming)
                {
                    var elapsed = StopwatchHelper.GetElapsedTime(startingTimestamp);
                    LayoutPassTimed?.Invoke(new LayoutPassTiming(_totalPassCount, elapsed));

                    Logger.TryGet(timingLogLevel, LogArea.Layout)?.Log(this, "Layout pass finished in {Time}", elapsed);
                }
            }
            else if (_processingBringIntoViewRequests)
            {
                // A layout pass forced while executing a bring-into-view request is part of the enclosing pass:
                // run inner passes inline, and let the enclosing pass raise LayoutUpdated once all requests have been processed.
                for (var pass = 0; pass < MaxPasses; ++pass)
                {
                    InnerLayoutPass();

                    if (!RaiseEffectiveViewportChanged())
                    {
                        break;
                    }
                }

                return;
            }

            _queued = false;

            LayoutUpdated?.Invoke(this, EventArgs.Empty);
        }

        /// <inheritdoc />
        public void EnqueueBringIntoView(BringIntoViewRequest request)
        {
            Dispatcher.UIThread.VerifyAccess();

            if (_disposed)
                return;

            var requests = _bringIntoViewRequests ??= new();
            var replaced = false;

            for (var i = 0; i < requests.Count; ++i)
            {
                if (requests[i].Target == request.Target)
                {
                    requests[i] = request;
                    replaced = true;
                    break;
                }
            }

            if (!replaced)
                requests.Add(request);

            // The request will usually be consumed by the already pending layout pass that will lay out its target,
            // but make sure a pass is scheduled in case there is none.
            QueueLayoutPass();
        }

        /// <summary>
        /// Attempts to execute each pending bring-into-view request once.
        /// </summary>
        /// <returns>
        /// true if at least one request was executed;
        /// false if there was nothing to do or no request could make progress.
        /// </returns>
        private bool ProcessBringIntoViewRequests()
        {
            if (_processingBringIntoViewRequests || _bringIntoViewRequests is not { Count: > 0 } requests)
                return false;

            _processingBringIntoViewRequests = true;

            try
            {
                var executedAny = false;
                var i = 0;

                while (i < requests.Count)
                {
                    var request = requests[i];

                    // The target has been detached, abort.
                    if (request.Target.GetLayoutRoot() != _owner)
                    {
                        requests.RemoveAt(i);
                        continue;
                    }

                    var executed = request.TryExecute();
                    executedAny |= executed;

                    // Executing the request may have replaced it with a new one for the same target.
                    if (executed && i < requests.Count && requests[i] == request)
                        requests.RemoveAt(i);
                    else
                        ++i;
                }

                return executedAny;
            }
            finally
            {
                _processingBringIntoViewRequests = false;
            }
        }

        /// <inheritdoc/>
        public virtual void ExecuteInitialLayoutPass()
        {
            if (_disposed)
            {
                return;
            }

            try
            {
                if (_owner?.RootVisual == null)
                    return;
                var root = _owner.RootVisual;
                _running = true;
                Measure(root);
                Arrange(root);
            }
            finally
            {
                _running = false;
            }

            // Running the initial layout pass may have caused some control to be invalidated
            // so run a full layout pass now (this usually due to scrollbars; its not known
            // whether they will need to be shown until the layout pass has run and if the
            // first guess was incorrect the layout will need to be updated).
            ExecuteLayoutPass();
        }

        public void Dispose()
        {
            _disposed = true;
            _toMeasure.Dispose();
            _toArrange.Dispose();
            _bringIntoViewRequests = null;
        }

        void ILayoutManager.RegisterEffectiveViewportListener(Layoutable control)
        {
            _effectiveViewportChangedListeners ??= new List<EffectiveViewportChangedListener>();
            _effectiveViewportChangedListeners.Add(new EffectiveViewportChangedListener(control));
        }

        void ILayoutManager.UnregisterEffectiveViewportListener(Layoutable control)
        {
            if (_effectiveViewportChangedListeners is object)
            {
                for (var i = _effectiveViewportChangedListeners.Count - 1; i >= 0; --i)
                {
                    if (_effectiveViewportChangedListeners[i].Listener == control)
                    {
                        _effectiveViewportChangedListeners.RemoveAt(i);
                    }
                }
            }
        }

        private void InnerLayoutPass()
        {
            for (var pass = 0; pass < MaxPasses; ++pass)
            {
                ExecuteMeasurePass();
                ExecuteArrangePass();

                if (_toMeasure.Count == 0)
                {
                    break;
                }
            }
        }

        private void ExecuteMeasurePass()
        {
            using var _ = Diagnostic.BeginLayoutMeasurePass();
            while (_toMeasure.Count > 0)
            {
                var control = _toMeasure.Dequeue();

                if (!control.IsMeasureValid)
                {
                    Measure(control);
                }

                _toArrange.Enqueue(control);
            }
        }

        private void ExecuteArrangePass()
        {
            using var _ = Diagnostic.BeginLayoutArrangePass();
            while (_toArrange.Count > 0)
            {
                var control = _toArrange.Dequeue();

                if (!control.IsArrangeValid)
                {
                    if (Arrange(control) == ArrangeResult.AncestorMeasureInvalid)
                        _toArrangeAfterMeasure.Add(control);
                }
            }

            foreach (var i in _toArrangeAfterMeasure)
                InvalidateArrange(i);
            _toArrangeAfterMeasure.Clear();
        }

        private bool Measure(Layoutable control)
        {
            if (!control.IsVisible || !control.IsAttachedToVisualTree)
                return false;

            // Controls closest to the visual root need to be arranged first. We don't try to store
            // ordered invalidation lists, instead we traverse the tree upwards, measuring the
            // controls closest to the root first. This has been shown by benchmarks to be the
            // fastest and most memory-efficient algorithm.
            if (control.VisualParent is Layoutable parent)
            {
                if (!Measure(parent))
                    return false;
            }

            // If the control being measured has IsMeasureValid == true here then its measure was
            // handed by an ancestor and can be ignored. The measure may have also caused the
            // control to be removed.
            if (!control.IsMeasureValid)
            {
                if (control.GetLayoutRoot()?.RootVisual == control)
                {
                    control.Measure(Size.Infinity);
                }
                else if (control.PreviousMeasure.HasValue)
                {
                    control.Measure(control.PreviousMeasure.Value);
                }
            }

            return true;
        }

        private ArrangeResult Arrange(Layoutable control)
        {
            if (!control.IsVisible || !control.IsAttachedToVisualTree)
                return ArrangeResult.NotVisible;

            if (control.VisualParent is Layoutable parent)
            {
                if (Arrange(parent) is var parentResult && parentResult != ArrangeResult.Arranged)
                    return parentResult;
            }

            if (!control.IsMeasureValid)
                return ArrangeResult.AncestorMeasureInvalid;

            if (!control.IsArrangeValid)
            {
                if (control.GetLayoutRoot()?.RootVisual == control)
                    control.Arrange(new Rect(control.DesiredSize));
                else if (control.PreviousArrange != null)
                {
                    // Has been observed that PreviousArrange sometimes is null, probably a bug somewhere else.
                    // Condition observed: control.VisualParent is Scrollbar, control is Border.
                    control.Arrange(control.PreviousArrange.Value);
                }
            }

            return ArrangeResult.Arranged;
        }

        private void QueueLayoutPass()
        {
            if (!_queued && !_running)
            {
                _queued = true;
                MediaContext.Instance.BeginInvokeOnRender(_invokeOnRender);
            }
        }

        private bool RaiseEffectiveViewportChanged()
        {
            var startCount = _toMeasure.Count + _toArrange.Count;

            if (_effectiveViewportChangedListeners is object)
            {
                var count = _effectiveViewportChangedListeners.Count;
                var pool = ArrayPool<EffectiveViewportChangedListener>.Shared;
                var listeners = pool.Rent(count);

                _effectiveViewportChangedListeners.CopyTo(listeners);

                try
                {
                    for (var i = 0; i < count; ++i)
                    {
                        var l = listeners[i];

                        if (!l.Listener.IsAttachedToVisualTree)
                        {
                            continue;
                        }

                        var viewport = CalculateEffectiveViewport(l.Listener);

                        if (viewport != l.Viewport)
                        {
                            l.Listener.RaiseEffectiveViewportChanged(new EffectiveViewportChangedEventArgs(viewport));
                            l.Viewport = viewport;
                        }
                    }
                }
                finally
                {
                    pool.Return(listeners, clearArray: true);
                }
            }

            return startCount != _toMeasure.Count + _toArrange.Count;
        }

        private Rect CalculateEffectiveViewport(Visual control)
        {
            var viewport = new Rect(0, 0, double.PositiveInfinity, double.PositiveInfinity);
            CalculateEffectiveViewport(control, control, ref viewport);
            return viewport;
        }

        private void CalculateEffectiveViewport(Visual target, Visual control, ref Rect viewport)
        {
            // Recurse until the top level control.
            if (control.VisualParent is object)
            {
                CalculateEffectiveViewport(target, control.VisualParent, ref viewport);
            }
            else
            {
                viewport = new Rect(control.Bounds.Size);
            }

            // Apply the control clip bounds if it's not the target control. We don't apply it to
            // the target control because it may itself be clipped to bounds and if so the viewport
            // we calculate would be of no use.
            if (control != target && control.ClipToBounds)
            {
                viewport = control.Bounds.Intersect(viewport);
            }

            // Translate the viewport into this control's coordinate space.
            viewport = viewport.Translate(-control.Bounds.Position);

            if (control != target && control.RenderTransform is { } transform)
            {
                if (transform.Value.TryInvert(out var invertedMatrix))
                {
                    var origin = control.RenderTransformOrigin.ToPixels(control.Bounds.Size);
                    var offset = Matrix.CreateTranslation(origin);
                    viewport = viewport.TransformToAABB(-offset * invertedMatrix * offset);
                }
                else
                    viewport = default;
            }
        }

        private class EffectiveViewportChangedListener
        {
            public EffectiveViewportChangedListener(Layoutable listener)
            {
                Listener = listener;
            }

            public Layoutable Listener { get; }
            public Rect? Viewport { get; set; }
        }

        private enum ArrangeResult
        {
            Arranged,
            NotVisible,
            AncestorMeasureInvalid,
        }
    }
}
