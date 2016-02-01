// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using Perspex.Threading;
using Serilog;
using Serilog.Core.Enrichers;

namespace Perspex.Layout
{
    /// <summary>
    /// Manages measuring and arranging of controls.
    /// </summary>
    public class LayoutManager : ILayoutManager
    {
        private readonly Queue<ILayoutable> _toMeasure = new Queue<ILayoutable>();
        private readonly Queue<ILayoutable> _toArrange = new Queue<ILayoutable>();
        private readonly ILogger _log;
        private bool _queued;
        private bool _running;

        /// <summary>
        /// Initializes a new instance of the <see cref="LayoutManager"/> class.
        /// </summary>
        public LayoutManager()
        {
            _log = Log.ForContext(new[]
            {
                new PropertyEnricher("Area", "Layout"),
                new PropertyEnricher("SourceContext", GetType()),
                new PropertyEnricher("Id", GetHashCode()),
            });
        }

        /// <summary>
        /// Gets the layout manager.
        /// </summary>
        public static ILayoutManager Instance => PerspexLocator.Current.GetService<ILayoutManager>();

        /// <inheritdoc/>
        public void InvalidateMeasure(ILayoutable control)
        {
            Contract.Requires<ArgumentNullException>(control != null);
            Dispatcher.UIThread.VerifyAccess();

            _toMeasure.Enqueue(control);
            _toArrange.Enqueue(control);
            QueueLayoutPass();
        }

        /// <inheritdoc/>
        public void InvalidateArrange(ILayoutable control)
        {
            Contract.Requires<ArgumentNullException>(control != null);
            Dispatcher.UIThread.VerifyAccess();

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

                _log.Information(
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
                _log.Information("Layout pass finised in {Time}", stopwatch.Elapsed);
            }
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
                var next = _toMeasure.Dequeue();
                Measure(next);
            }
        }

        private void ExecuteArrangePass()
        {
            while (_toArrange.Count > 0 && _toMeasure.Count == 0)
            {
                var next = _toArrange.Dequeue();
                Arrange(next);
            }
        }

        private void Measure(ILayoutable control)
        {
            var root = control as ILayoutRoot;

            if (root != null)
            {
                root.Measure(root.MaxClientSize);
            }
            else if (control.PreviousMeasure.HasValue)
            {
                control.Measure(control.PreviousMeasure.Value);
            }
        }

        private void Arrange(ILayoutable control)
        {
            var root = control as ILayoutRoot;

            if (root != null)
            {
                root.Arrange(new Rect(root.DesiredSize));
            }
            else if (control.PreviousArrange.HasValue)
            {
                control.Arrange(control.PreviousArrange.Value);
            }
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
