// -----------------------------------------------------------------------
// <copyright file="LayoutManager.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Layout
{
    using System;
    using System.Reactive;
    using System.Reactive.Disposables;
    using System.Reactive.Subjects;
    using NGenerics.DataStructures.General;
    using Perspex.VisualTree;
    using Serilog;
    using Serilog.Core.Enrichers;

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
        private Subject<Unit> layoutNeeded;

        /// <summary>
        /// Called when a layout is completed.
        /// </summary>
        private Subject<Unit> layoutCompleted;

        /// <summary>
        /// Whether a measure is needed on the next layout pass.
        /// </summary>
        private bool measureNeeded = true;

        /// <summary>
        /// The controls that need to be measured, sorted by distance to layout root.
        /// </summary>
        private Heap<Item> toMeasure = new Heap<Item>(HeapType.Minimum);

        /// <summary>
        /// The controls that need to be arranged, sorted by distance to layout root.
        /// </summary>
        private Heap<Item> toArrange = new Heap<Item>(HeapType.Minimum);

        /// <summary>
        /// Prevents re-entrancy.
        /// </summary>
        private bool running;

        /// <summary>
        /// The logger to use.
        /// </summary>
        private ILogger log;

        /// <summary>
        /// Initializes a new instance of the <see cref="LayoutManager"/> class.
        /// </summary>
        public LayoutManager()
        {
            this.log = Log.ForContext(new[]
            {
                new PropertyEnricher("Area", "Layout"),
                new PropertyEnricher("SourceContext", this.GetType()),
                new PropertyEnricher("Id", this.GetHashCode()),
            });

            this.layoutNeeded = new Subject<Unit>();
            this.layoutCompleted = new Subject<Unit>();
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
        public IObservable<Unit> LayoutNeeded => this.layoutNeeded;

        /// <summary>
        /// Gets an observable that is fired when a layout pass is completed.
        /// </summary>
        public IObservable<Unit> LayoutCompleted => this.layoutCompleted;

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
            if (this.running)
            {
                return;
            }

            using (Disposable.Create(() => this.running = false))
            {
                this.running = true;
                this.LayoutQueued = false;

                this.log.Information(
                    "Started layout pass. To measure: {Measure} To arrange: {Arrange}",
                    this.toMeasure.Count,
                    this.toArrange.Count);

                var stopwatch = new System.Diagnostics.Stopwatch();
                stopwatch.Start();

                for (int i = 0; i < MaxTries; ++i)
                {
                    if (this.measureNeeded)
                    {
                        this.ExecuteMeasure();
                        this.measureNeeded = false;
                    }

                    this.ExecuteArrange();

                    if (this.toMeasure.Count == 0)
                    {
                        break;
                    }
                }

                stopwatch.Stop();
                this.log.Information("Layout pass finised in {Time}", stopwatch.Elapsed);

                this.layoutCompleted.OnNext(Unit.Default);
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
            this.toMeasure.Add(item);
            this.toArrange.Add(item);

            this.measureNeeded = true;

            if (!this.LayoutQueued)
            {
                IVisual visual = control as IVisual;
                this.layoutNeeded.OnNext(Unit.Default);
                this.LayoutQueued = true;
            }
        }

        /// <summary>
        /// Notifies the layout manager that a control requires an arrange.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="distance">The control's distance from the layout root.</param>
        public void InvalidateArrange(ILayoutable control, int distance)
        {
            this.toArrange.Add(new Item(control, distance));

            if (!this.LayoutQueued)
            {
                IVisual visual = control as IVisual;
                this.layoutNeeded.OnNext(Unit.Default);
                this.LayoutQueued = true;
            }
        }

        /// <summary>
        /// Executes the measure part of the layout pass.
        /// </summary>
        private void ExecuteMeasure()
        {
            for (int i = 0; i < MaxTries; ++i)
            {
                var measure = this.toMeasure;

                this.toMeasure = new Heap<Item>(HeapType.Minimum);

                if (!this.Root.IsMeasureValid)
                {
                    var size = new Size(
                        double.IsNaN(this.Root.Width) ? double.PositiveInfinity : this.Root.Width,
                        double.IsNaN(this.Root.Height) ? double.PositiveInfinity : this.Root.Height);
                    this.Root.Measure(size);
                }

                foreach (var item in measure)
                {
                    if (!item.Control.IsMeasureValid)
                    {
                        if (item.Control != this.Root)
                        {
                            var parent = item.Control.GetVisualParent<ILayoutable>();

                            while (parent.PreviousMeasure == null)
                            {
                                parent = parent.GetVisualParent<ILayoutable>();
                            }

                            if (parent.GetVisualRoot() == this.Root)
                            {
                                parent.Measure(parent.PreviousMeasure.Value, true);
                            }
                        }
                    }
                }

                if (this.toMeasure.Count == 0)
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
                var arrange = this.toArrange;

                this.toArrange = new Heap<Item>(HeapType.Minimum);

                if (!this.Root.IsArrangeValid && this.Root.IsMeasureValid)
                {
                    this.Root.Arrange(new Rect(this.Root.DesiredSize));
                }

                if (this.toMeasure.Count > 0)
                {
                    return;
                }

                foreach (var item in arrange)
                {
                    if (!item.Control.IsArrangeValid)
                    {
                        if (item.Control != this.Root)
                        {
                            var control = item.Control;

                            while (control.PreviousArrange == null)
                            {
                                control = control.GetVisualParent<ILayoutable>();
                            }

                            if (control.GetVisualRoot() == this.Root)
                            {
                                control.Arrange(control.PreviousArrange.Value, true);
                            }

                            if (this.toMeasure.Count > 0)
                            {
                                return;
                            }
                        }
                    }
                }

                if (this.toArrange.Count == 0)
                {
                    break;
                }
            }
        }

        private class Item : IComparable<Item>
        {
            public Item(ILayoutable control, int distance)
            {
                this.Control = control;
                this.Distance = distance;
            }

            public ILayoutable Control { get; private set; }

            public int Distance { get; private set; }

            public int CompareTo(Item other)
            {
                return this.Distance - other.Distance;
            }
        }
    }
}
