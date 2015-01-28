// -----------------------------------------------------------------------
// <copyright file="ScrollBar.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls.Primitives
{
    using System;
    using System.Reactive;
    using System.Reactive.Linq;

    public class ScrollBar : TemplatedControl
    {
        public static readonly PerspexProperty<double> MinimumProperty =
            PerspexProperty.Register<ScrollBar, double>("Minimum");

        public static readonly PerspexProperty<double> MaximumProperty =
            PerspexProperty.Register<ScrollBar, double>("Maximum", defaultValue: 100.0);

        public static readonly PerspexProperty<double> ValueProperty =
            PerspexProperty.Register<ScrollBar, double>("Value");

        public static readonly PerspexProperty<double> ViewportSizeProperty =
            PerspexProperty.Register<ScrollBar, double>("ViewportSize", defaultValue: double.NaN);

        public static readonly PerspexProperty<ScrollBarVisibility> VisibilityProperty =
            PerspexProperty.Register<ScrollBar, ScrollBarVisibility>("Visibility");

        public static readonly PerspexProperty<Orientation> OrientationProperty =
            PerspexProperty.Register<ScrollBar, Orientation>("Orientation");

        static ScrollBar()
        {
            Control.PseudoClass(OrientationProperty, x => x == Orientation.Horizontal, ":horizontal");
            Control.PseudoClass(OrientationProperty, x => x == Orientation.Vertical, ":vertical");
        }

        public ScrollBar()
        {
            var isVisible = Observable.Merge(
                this.GetObservable(MinimumProperty).Select(_ => Unit.Default),
                this.GetObservable(MaximumProperty).Select(_ => Unit.Default),
                this.GetObservable(ViewportSizeProperty).Select(_ => Unit.Default),
                this.GetObservable(VisibilityProperty).Select(_ => Unit.Default))
                .Select(_ => this.CalculateIsVisible());
            this.Bind(ScrollBar.IsVisibleProperty, isVisible, BindingPriority.Style);
        }

        public double Minimum
        {
            get { return this.GetValue(MinimumProperty); }
            set { this.SetValue(MinimumProperty, value); }
        }

        public double Maximum
        {
            get { return this.GetValue(MaximumProperty); }
            set { this.SetValue(MaximumProperty, value); }
        }

        public double Value
        {
            get { return this.GetValue(ValueProperty); }
            set { this.SetValue(ValueProperty, value); }
        }

        public double ViewportSize
        {
            get { return this.GetValue(ViewportSizeProperty); }
            set { this.SetValue(ViewportSizeProperty, value); }
        }

        public ScrollBarVisibility Visibility
        {
            get { return this.GetValue(VisibilityProperty); }
            set { this.SetValue(VisibilityProperty, value); }
        }

        public Orientation Orientation
        {
            get { return this.GetValue(OrientationProperty); }
            set { this.SetValue(OrientationProperty, value); }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            return base.MeasureOverride(availableSize);
        }

        private bool CalculateIsVisible()
        {
            switch (this.Visibility)
            {
                case ScrollBarVisibility.Visible:
                    return true;

                case ScrollBarVisibility.Hidden:
                    return false;

                case ScrollBarVisibility.Auto:
                    var viewportSize = this.ViewportSize;
                    return !double.IsNaN(viewportSize) && viewportSize < this.Maximum - this.Minimum;

                default:
                    throw new InvalidOperationException("Invalid value for ScrollBar.Visibility.");
            }
        }
    }
}
