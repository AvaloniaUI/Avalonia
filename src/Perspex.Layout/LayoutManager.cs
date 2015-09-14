// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using NGenerics.DataStructures.General;
using Perspex.VisualTree;
using Serilog;
using Serilog.Core.Enrichers;

namespace Perspex.Layout
{
    /// <summary>
    /// Manages measuring and arranging of controls.
    /// </summary>
    /// <remarks>
    /// Each layout root element such as a window has its own LayoutManager that is responsible
    /// for laying out its child controls. When a layout is required the <see cref="LayoutNeeded"/>
    /// observable will fire and the root element should respond by calling
    /// <see cref="ExecuteLayoutPass"/> at the earliest opportunity to carry out the layout.
    /// </remarks>
    public class LayoutManager : ILayoutManager
    {
        /// <summary>
        /// The maximum number of times a measure/arrange loop can be retried.
        /// </summary>
        private const int MaxTries = 3;

        /// <summary>
        /// Called when a layout is needed.
        /// </summary>
        private readonly Subject<Unit> _layoutNeeded;

        /// <summary>
        /// Called when a layout is completed.
        /// </summary>
        private readonly Subject<Unit> _layoutCompleted;

        /// <summary>
        /// Whether a measure is needed on the next layout pass.
        /// </summary>
        private bool _measureNeeded = true;

        /// <summary>
        /// The controls that need to be measured, sorted by distance to layout root.
        /// </summary>
        private Heap<Item> _toMeasure = new Heap<Item>(HeapType.Minimum);

        /// <summary>
        /// The controls that need to be arranged, sorted by distance to layout root.
        /// </summary>
        private Heap<Item> _toArrange = new Heap<Item>(HeapType.Minimum);

        /// <summary>
        /// Prevents re-entrancy.
        /// </summary>
        private bool _running;

        /// <summary>
        /// The logger to use.
        /// </summary>
        private readonly ILogger _log;

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

            _layoutNeeded = new Subject<Unit>();
            _layoutCompleted = new Subject<Unit>();
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
        /// Executes a layout pass.
        /// </summary>
        public void ExecuteLayoutPass()
        {
            if (_running)
            {
                return;
            }

            using (Disposable.Create(() => _running = false))
            {
                _running = true;
                LayoutQueued = false;

                _log.Information(
                    "Started layout pass. To measure: {Measure} To arrange: {Arrange}",
                    _toMeasure.Count,
                    _toArrange.Count);

                var stopwatch = new System.Diagnostics.Stopwatch();
                stopwatch.Start();

                for (int i = 0; i < MaxTries; ++i)
                {
                    if (_measureNeeded)
                    {
                        ExecuteMeasure();
                        _measureNeeded = false;
                    }

                    ExecuteArrange();

                    if (_toMeasure.Count == 0)
                    {
                        break;
                    }
                }

                stopwatch.Stop();
                _log.Information("Layout pass finised in {Time}", stopwatch.Elapsed);

                _layoutCompleted.OnNext(Unit.Default);
            }
        }

        /// <summary>
        /// Notifies the layout manager that a control requires a measure.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="distance">The control's distance from the layout root.</param>
        public void InvalidateMeasure(ILayoutable control, int distance)
        {
            var item = new Item(control, distance);
            _toMeasure.Add(item);
            _toArrange.Add(item);

            _measureNeeded = true;

            if (!LayoutQueued)
            {
                IVisual visual = control as IVisual;
                _layoutNeeded.OnNext(Unit.Default);
                LayoutQueued = true;
            }
        }

        /// <summary>
        /// Notifies the layout manager that a control requires an arrange.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="distance">The control's distance from the layout root.</param>
        public void InvalidateArrange(ILayoutable control, int distance)
        {
            _toArrange.Add(new Item(control, distance));

            if (!LayoutQueued)
            {
                IVisual visual = control as IVisual;
                _layoutNeeded.OnNext(Unit.Default);
                LayoutQueued = true;
            }
        }

        /// <summary>
        /// Executes the measure part of the layout pass.
        /// </summary>
        private void ExecuteMeasure()
        {
            for (int i = 0; i < MaxTries; ++i)
            {
                var measure = _toMeasure;

                _toMeasure = new Heap<Item>(HeapType.Minimum);

                if (!Root.IsMeasureValid)
                {
                    var size = new Size(
                        double.IsNaN(Root.Width) ? double.PositiveInfinity : Root.Width,
                        double.IsNaN(Root.Height) ? double.PositiveInfinity : Root.Height);
                    Root.Measure(size);
                }

                foreach (var item in measure)
                {
                    if (!item.Control.IsMeasureValid)
                    {
                        if (item.Control != Root)
                        {
                            var parent = item.Control.GetVisualParent<ILayoutable>();

                            while (parent.PreviousMeasure == null)
                            {
                                parent = parent.GetVisualParent<ILayoutable>();
                            }

                            if (parent.GetVisualRoot() == Root)
                            {
                                parent.Measure(parent.PreviousMeasure.Value, true);
                            }
                        }
                    }
                }

                if (_toMeasure.Count == 0)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Executes the arrange part of the layout pass.
        /// </summary>
        private void ExecuteArrange()
        {
            for (int i = 0; i < MaxTries; ++i)
            {
                var arrange = _toArrange;

                _toArrange = new Heap<Item>(HeapType.Minimum);

                if (!Root.IsArrangeValid && Root.IsMeasureValid)
                {
                    Root.Arrange(new Rect(Root.DesiredSize));
                }

                if (_toMeasure.Count > 0)
                {
                    return;
                }

                foreach (var item in arrange)
                {
                    if (!item.Control.IsArrangeValid)
                    {
                        if (item.Control != Root)
                        {
                            var control = item.Control;

                            while (control.PreviousArrange == null)
                            {
                                control = control.GetVisualParent<ILayoutable>();
                            }

                            if (control.GetVisualRoot() == Root)
                            {
                                control.Arrange(control.PreviousArrange.Value, true);
                            }

                            if (_toMeasure.Count > 0)
                            {
                                return;
                            }
                        }
                    }
                }

                if (_toArrange.Count == 0)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// An item to be layed-out.
        /// </summary>
        private class Item : IComparable<Item>
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="Item"/> class.
            /// </summary>
            /// <param name="control">The control.</param>
            /// <param name="distance">The control's distance from the layout root.</param>
            public Item(ILayoutable control, int distance)
            {
                Control = control;
                Distance = distance;
            }

            /// <summary>
            /// Gets the control.
            /// </summary>
            public ILayoutable Control { get; }

            /// <summary>
            /// Gets the control's distance from the layout root.
            /// </summary>
            public int Distance { get; }

            /// <summary>
            /// Compares the distance of two items.
            /// </summary>
            /// <param name="other">The other item/</param>
            /// <returns>The comparison.</returns>
            public int CompareTo(Item other)
            {
                return Distance - other.Distance;
            }
        }
    }
}
