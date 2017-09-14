// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive;
using System.Reactive.Linq;
using Avalonia.Data;

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    /// A scrollbar control.
    /// </summary>
    public class ScrollBar : RangeBase
    {
        /// <summary>
        /// Defines the <see cref="ViewportSize"/> property.
        /// </summary>
        public static readonly StyledProperty<double> ViewportSizeProperty =
            AvaloniaProperty.Register<ScrollBar, double>(nameof(ViewportSize), defaultValue: double.NaN);

        /// <summary>
        /// Defines the <see cref="Visibility"/> property.
        /// </summary>
        public static readonly StyledProperty<ScrollBarVisibility> VisibilityProperty =
            AvaloniaProperty.Register<ScrollBar, ScrollBarVisibility>(nameof(Visibility));

        /// <summary>
        /// Defines the <see cref="Orientation"/> property.
        /// </summary>
        public static readonly StyledProperty<Orientation> OrientationProperty =
            AvaloniaProperty.Register<ScrollBar, Orientation>(nameof(Orientation));

        /// <summary>
        /// Initializes static members of the <see cref="ScrollBar"/> class. 
        /// </summary>
        static ScrollBar()
        {
            PseudoClass(OrientationProperty, o => o == Avalonia.Controls.Orientation.Vertical, ":vertical");
            PseudoClass(OrientationProperty, o => o == Avalonia.Controls.Orientation.Horizontal, ":horizontal");
        }

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
                .Select(_ => CalculateIsVisible());
            Bind(IsVisibleProperty, isVisible, BindingPriority.Style);
        }

        /// <summary>
        /// Gets or sets the amount of the scrollable content that is currently visible.
        /// </summary>
        public double ViewportSize
        {
            get { return GetValue(ViewportSizeProperty); }
            set { SetValue(ViewportSizeProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether the scrollbar should hide itself when it
        /// is not needed.
        /// </summary>
        public ScrollBarVisibility Visibility
        {
            get { return GetValue(VisibilityProperty); }
            set { SetValue(VisibilityProperty, value); }
        }

        /// <summary>
        /// Gets or sets the orientation of the scrollbar.
        /// </summary>
        public Orientation Orientation
        {
            get { return GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }

        /// <summary>
        /// Calculates whether the scrollbar should be visible.
        /// </summary>
        /// <returns>The scrollbar's visibility.</returns>
        private bool CalculateIsVisible()
        {
            switch (Visibility)
            {
                case ScrollBarVisibility.Visible:
                    return true;

                case ScrollBarVisibility.Hidden:
                    return false;

                case ScrollBarVisibility.Auto:
                    var viewportSize = ViewportSize;
                    return double.IsNaN(viewportSize) || viewportSize < Maximum - Minimum;

                default:
                    throw new InvalidOperationException("Invalid value for ScrollBar.Visibility.");
            }
        }
    }
}
