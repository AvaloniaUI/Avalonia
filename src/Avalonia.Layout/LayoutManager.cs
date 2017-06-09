// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using Avalonia.Logging;
using Avalonia.Rendering;
using Avalonia.Threading;

namespace Avalonia.Layout
{
    /// <summary>
    /// Manages measuring and arranging of controls.
    /// </summary>
    public class LayoutManager : ILayoutManager
    {
        private readonly DirtyVisuals _toMeasure = new DirtyVisuals();
        private readonly DirtyVisuals _toArrange = new DirtyVisuals();
        private bool _queued;
        private bool _running;

        /// <summary>
        /// Gets the layout manager.
        /// </summary>
        public static ILayoutManager Instance => AvaloniaLocator.Current.GetService<ILayoutManager>();

        /// <inheritdoc/>
        public void InvalidateMeasure(ILayoutable control)
        {
            Contract.Requires<ArgumentNullException>(control != null);
            Dispatcher.UIThread.VerifyAccess();

            if (control.IsAttachedToVisualTree)
            {
                _toMeasure.Add(control);
                _toArrange.Add(control);
                QueueLayoutPass();
            }
        }

        /// <inheritdoc/>
        public void InvalidateArrange(ILayoutable control)
        {
            Contract.Requires<ArgumentNullException>(control != null);
            Dispatcher.UIThread.VerifyAccess();

            if (control.IsAttachedToVisualTree)
            {
                _toArrange.Add(control);
                QueueLayoutPass();
            }
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

                stopwatch.Stop();
                Logger.Information(LogArea.Layout, this, "Layout pass finised in {Time}", stopwatch.Elapsed);
            }

            _queued = false;
        }

        /// <inheritdoc/>
        public void ExecuteInitialLayoutPass(ILayoutRoot root)
        {
            root.Measure((root as IEmbeddedLayoutRoot)?.EmbeddedConstraint ?? Size.Infinity);
            root.Arrange(new Rect(root.DesiredSize));

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
                var next = _toMeasure.First();
                Measure((ILayoutable)next);
            }
        }

        private void ExecuteArrangePass()
        {
            while (_toArrange.Count > 0 && _toMeasure.Count == 0)
            {
                var next = _toArrange.First();
                Arrange((ILayoutable)next);
            }
        }

        private void Measure(ILayoutable control)
        {
            if (!control.IsMeasureValid && control.IsAttachedToVisualTree)
            {
                if (control is ILayoutRoot root)
                {
                    control.Measure((control as IEmbeddedLayoutRoot)?.EmbeddedConstraint ?? Size.Infinity);
                }
                else if (control.PreviousMeasure.HasValue)
                {
                    control.Measure(control.PreviousMeasure.Value);
                }
                else
                {
                    Logger.Error(
                        LogArea.Layout,
                        this,
                        "A control of type {Type} was queued for measure but its parent of type {ParentType} did not queue itself.",
                        control.GetType(),
                        control.VisualParent?.GetType());
                }
            }

            _toMeasure.Remove(control);
        }

        private void Arrange(ILayoutable control)
        {
            if (!control.IsArrangeValid && control.IsAttachedToVisualTree)
            {
                if (control is ILayoutRoot root)
                {
                    control.Arrange(new Rect(root.DesiredSize));
                }
                else if (control.PreviousArrange.HasValue)
                {
                    control.Arrange(control.PreviousArrange.Value);
                }
                else
                {
                    Logger.Error(
                        LogArea.Layout,
                        this,
                        "A control of type {Type} was queued for arrange but its parent of type {ParentType} did not queue itself.",
                        control.GetType(),
                        control.VisualParent?.GetType());
                }
            }

            _toArrange.Remove(control);
        }

        private void QueueLayoutPass()
        {
            if (!_queued && !_running)
            {
                Dispatcher.UIThread.InvokeAsync(ExecuteLayoutPass, DispatcherPriority.Render);
                _queued = true;
            }
        }
    }
}
