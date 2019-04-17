// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Logging;
using Avalonia.Threading;

namespace Avalonia.Layout
{
    /// <summary>
    /// Manages measuring and arranging of controls.
    /// </summary>
    public class LayoutManager : ILayoutManager
    {
        private readonly LayoutQueue<ILayoutable> _toMeasure = new LayoutQueue<ILayoutable>(v => !v.IsMeasureValid);
        private readonly LayoutQueue<ILayoutable> _toArrange = new LayoutQueue<ILayoutable>(v => !v.IsArrangeValid);
        private bool _queued;
        private bool _running;

        /// <inheritdoc/>
        public void InvalidateMeasure(ILayoutable control)
        {
            Contract.Requires<ArgumentNullException>(control != null);
            Dispatcher.UIThread.VerifyAccess();

            if (!control.IsAttachedToVisualTree)
            {
#if DEBUG
                throw new AvaloniaInternalException(
                    "LayoutManager.InvalidateMeasure called on a control that is detached from the visual tree.");
#else
                return;
#endif
            }

            _toMeasure.Enqueue(control);
            _toArrange.Enqueue(control);
            QueueLayoutPass();
        }

        /// <inheritdoc/>
        public void InvalidateArrange(ILayoutable control)
        {
            Contract.Requires<ArgumentNullException>(control != null);
            Dispatcher.UIThread.VerifyAccess();

            if (!control.IsAttachedToVisualTree)
            {
#if DEBUG
                throw new AvaloniaInternalException(
                    "LayoutManager.InvalidateArrange called on a control that is detached from the visual tree.");
#else
                return;
#endif
            }

            _toArrange.Enqueue(control);
            QueueLayoutPass();
        }

        /// <inheritdoc/>
        public void ExecuteLayoutPass()
        {
            const int MaxPasses = 3;

            Dispatcher.UIThread.VerifyAccess();

            if (!_running)
            {
                _running = true;

                Logger.Information(
                    LogArea.Layout,
                    this,
                    "Started layout pass. To measure: {Measure} To arrange: {Arrange}",
                    _toMeasure.Count,
                    _toArrange.Count);

                var stopwatch = new System.Diagnostics.Stopwatch();
                stopwatch.Start();

                _toMeasure.BeginLoop(MaxPasses);
                _toArrange.BeginLoop(MaxPasses);

                try
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
                finally
                {
                    _running = false;
                }

                _toMeasure.EndLoop();
                _toArrange.EndLoop();

                stopwatch.Stop();
                Logger.Information(LogArea.Layout, this, "Layout pass finished in {Time}", stopwatch.Elapsed);
            }

            _queued = false;
        }

        /// <inheritdoc/>
        public void ExecuteInitialLayoutPass(ILayoutRoot root)
        {
            Measure(root);
            Arrange(root);

            // Running the initial layout pass may have caused some control to be invalidated
            // so run a full layout pass now (this usually due to scrollbars; its not known
            // whether they will need to be shown until the layout pass has run and if the
            // first guess was incorrect the layout will need to be updated).
            ExecuteLayoutPass();
        }

        private void ExecuteMeasurePass()
        {
            while (_toMeasure.Count > 0)
            {
                var control = _toMeasure.Dequeue();

                if (!control.IsMeasureValid && control.IsAttachedToVisualTree)
                {
                    Measure(control);
                }
            }
        }

        private void ExecuteArrangePass()
        {
            while (_toArrange.Count > 0)
            {
                var control = _toArrange.Dequeue();

                if (!control.IsArrangeValid && control.IsAttachedToVisualTree)
                {
                    Arrange(control);
                }
            }
        }

        private void Measure(ILayoutable control)
        {
            // Controls closest to the visual root need to be arranged first. We don't try to store
            // ordered invalidation lists, instead we traverse the tree upwards, measuring the
            // controls closest to the root first. This has been shown by benchmarks to be the
            // fastest and most memory-efficient algorithm.
            if (control.VisualParent is ILayoutable parent)
            {
                Measure(parent);
            }

            // If the control being measured has IsMeasureValid == true here then its measure was
            // handed by an ancestor and can be ignored. The measure may have also caused the
            // control to be removed.
            if (!control.IsMeasureValid && control.IsAttachedToVisualTree)
            {
                if (control is ILayoutRoot root)
                {
                    root.Measure(Size.Infinity);
                }
                else if (control.PreviousMeasure.HasValue)
                {
                    control.Measure(control.PreviousMeasure.Value);
                }
            }
        }

        private void Arrange(ILayoutable control)
        {
            if (control.VisualParent is ILayoutable parent)
            {
                Arrange(parent);
            }

            if (!control.IsArrangeValid && control.IsAttachedToVisualTree)
            {
                if (control is IEmbeddedLayoutRoot embeddedRoot)
                    control.Arrange(new Rect(embeddedRoot.AllocatedSize));
                else if (control is ILayoutRoot root)
                    control.Arrange(new Rect(root.DesiredSize));
                else if (control.PreviousArrange != null)
                {
                    // Has been observed that PreviousArrange sometimes is null, probably a bug somewhere else.
                    // Condition observed: control.VisualParent is Scrollbar, control is Border.
                    control.Arrange(control.PreviousArrange.Value);
                }
            }
        }

        private void QueueLayoutPass()
        {
            if (!_queued && !_running)
            {
                Dispatcher.UIThread.Post(ExecuteLayoutPass, DispatcherPriority.Layout);
                _queued = true;
            }
        }
    }
}
