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
    using Perspex.Controls.Templates;

    public class ScrollBar : RangeBase
    {
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

        protected override void OnTemplateApplied()
        {
            base.OnTemplateApplied();

            // Binding between this.Value and track.Value must be done explicitly like this rather 
            // than using standard bindings as it shouldn't be able to to be overridden by binding
            // e.g. ScrollBar.Value.
            var track = this.GetTemplateChild<Track>("track");
            track.GetObservable(ValueProperty).Subscribe(x => this.Value = x);
            this.GetObservable(ValueProperty).Subscribe(x => track.Value = x);
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
