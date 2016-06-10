// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Logging;
using Avalonia.Threading;

namespace Avalonia.Layout
{
    /// <summary>
    /// Manages measuring and arranging of controls.
    /// </summary>
    public class LayoutManager : ILayoutManager
    {
        private readonly HashSet<ILayoutable> _toMeasure = new HashSet<ILayoutable>();
        private readonly HashSet<ILayoutable> _toArrange = new HashSet<ILayoutable>();
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

            _toMeasure.Add(control);
            _toArrange.Add(control);
            QueueLayoutPass();
        }

        /// <inheritdoc/>
        public void InvalidateArrange(ILayoutable control)
        {
            Contract.Requires<ArgumentNullException>(control != null);
            Dispatcher.UIThread.VerifyAccess();

            _toArrange.Add(control);
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
                var next = _toMeasure.First();
                Measure(next);
            }
        }

        private void ExecuteArrangePass()
        {
            while (_toArrange.Count > 0 && _toMeasure.Count == 0)
            {
                var next = _toArrange.First();
                Arrange(next);
            }
        }

        private void Measure(ILayoutable control)
        {
            var root = control as ILayoutRoot;
            var parent = control.VisualParent as ILayoutable;

            if (root != null)
            {
                root.Measure(root.MaxClientSize);
            }
            else if (parent != null)
            {
                Measure(parent);
            }

            if (!control.IsMeasureValid)
            {
                control.Measure(control.PreviousMeasure.Value);
            }

            _toMeasure.Remove(control);
        }

        private void Arrange(ILayoutable control)
        {
            var root = control as ILayoutRoot;
            var parent = control.VisualParent as ILayoutable;

            if (root != null)
            {
                root.Arrange(new Rect(root.DesiredSize));
            }
            else if (parent != null)
            {
                Arrange(parent);
            }

            if (control.PreviousArrange.HasValue)
            {
                control.Arrange(control.PreviousArrange.Value);
            }

            _toArrange.Remove(control);
        }

        private void QueueLayoutPass()
        {
            if (!_queued)
            {
                Dispatcher.UIThread.InvokeAsync(ExecuteLayoutPass, DispatcherPriority.Render);
                _queued = true;
            }
        }
    }
}
