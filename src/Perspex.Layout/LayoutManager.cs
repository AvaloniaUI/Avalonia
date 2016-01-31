// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Subjects;
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
        private readonly Subject<Unit> _layoutNeeded = new Subject<Unit>();
        private readonly Subject<Unit> _layoutCompleted = new Subject<Unit>();
        private readonly ILogger _log;
        private bool _first = true;
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
        /// Gets or sets the root element that the manager is attached to.
        /// </summary>
        /// <remarks>
        /// This must be set before the layout manager can be used.
        /// </remarks>
        public ILayoutRoot Root
        {
            get;
            set;
        }

        /// <summary>
        /// Gets an observable that is fired when a layout pass is needed.
        /// </summary>
        public IObservable<Unit> LayoutNeeded => _layoutNeeded;

        /// <summary>
        /// Gets an observable that is fired when a layout pass is completed.
        /// </summary>
        public IObservable<Unit> LayoutCompleted => _layoutCompleted;

        /// <summary>
        /// Gets a value indicating whether a layout is queued.
        /// </summary>
        /// <remarks>
        /// Returns true when <see cref="LayoutNeeded"/> has been fired, but
        /// <see cref="ExecuteLayoutPass"/> has not yet been called.
        /// </remarks>
        public bool LayoutQueued
        {
            get;
            private set;
        }

        /// <summary>
        /// Notifies the layout manager that a control requires a measure.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="distance">The control's distance from the layout root.</param>
        public void InvalidateMeasure(ILayoutable control, int distance)
        {
            Contract.Requires<ArgumentNullException>(control != null);
            Dispatcher.UIThread.VerifyAccess();

            _toMeasure.Enqueue(control);
            _toArrange.Enqueue(control);
            FireLayoutNeeded();
        }

        /// <summary>
        /// Notifies the layout manager that a control requires an arrange.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="distance">The control's distance from the layout root.</param>
        public void InvalidateArrange(ILayoutable control, int distance)
        {
            Contract.Requires<ArgumentNullException>(control != null);
            Dispatcher.UIThread.VerifyAccess();

            _toArrange.Enqueue(control);
            FireLayoutNeeded();
        }

        /// <summary>
        /// Executes a layout pass.
        /// </summary>
        public void ExecuteLayoutPass()
        {
            const int MaxPasses = 3;

            Dispatcher.UIThread.VerifyAccess();

            if (Root == null)
            {
                throw new InvalidOperationException("Root must be set before executing layout pass.");
            }

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
                    if (_first)
                    {
                        Measure(Root);
                        Arrange(Root);
                        _first = false;
                    }

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
                    LayoutQueued = false;
                }

                stopwatch.Stop();
                _log.Information("Layout pass finised in {Time}", stopwatch.Elapsed);

                _layoutCompleted.OnNext(Unit.Default);
            }
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
                root.Measure(Size.Infinity);
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

        private void FireLayoutNeeded()
        {
            if (!LayoutQueued)
            {
                _layoutNeeded.OnNext(Unit.Default);
                LayoutQueued = true;
            }
        }
    }
}
