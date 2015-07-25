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

    /// <summary>
    /// A scrollbar control.
    /// </summary>
    public class ScrollBar : RangeBase
    {
        /// <summary>
        /// Defines the <see cref="ViewportSize"/> property.
        /// </summary>
        public static readonly PerspexProperty<double> ViewportSizeProperty =
            PerspexProperty.Register<ScrollBar, double>(nameof(ViewportSize), defaultValue: double.NaN);

        /// <summary>
        /// Defines the <see cref="Visibility"/> property.
        /// </summary>
        public static readonly PerspexProperty<ScrollBarVisibility> VisibilityProperty =
            PerspexProperty.Register<ScrollBar, ScrollBarVisibility>(nameof(Visibility));

        /// <summary>
        /// Defines the <see cref="Orientation"/> property.
        /// </summary>
        public static readonly PerspexProperty<Orientation> OrientationProperty =
            PerspexProperty.Register<ScrollBar, Orientation>(nameof(Orientation));

        /// <summary>
        /// Initializes a new instance of the <see cref="ScrollBar"/> class.
        /// </summary>
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

        /// <summary>
        /// Gets or sets the amount of the scrollable content that is currently visible.
        /// </summary>
        public double ViewportSize
        {
            get { return this.GetValue(ViewportSizeProperty); }
            set { this.SetValue(ViewportSizeProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether the scrollbar should hide itself when it
        /// is not needed.
        /// </summary>
        public ScrollBarVisibility Visibility
        {
            get { return this.GetValue(VisibilityProperty); }
            set { this.SetValue(VisibilityProperty, value); }
        }

        /// <summary>
        /// Gets or sets the orientation of the scrollbar.
        /// </summary>
        public Orientation Orientation
        {
            get { return this.GetValue(OrientationProperty); }
            set { this.SetValue(OrientationProperty, value); }
        }

        /// <inheritdoc/>
        protected override Size MeasureOverride(Size availableSize)
        {
            return base.MeasureOverride(availableSize);
        }

        /// <summary>
        /// Calculates whether the scrollbar should be visible.
        /// </summary>
        /// <returns>The scrollbar's visibility.</returns>
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
