// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Perspex.Controls.Presenters;
using Perspex.Controls.Primitives;

namespace Perspex.Controls
{
    /// <summary>
    /// A control scrolls its content if the content is bigger than the space available.
    /// </summary>
    public class ScrollViewer : ContentControl
    {
        /// <summary>
        /// Defines the <see cref="CanScrollHorizontally"/> property.
        /// </summary>
        public static readonly PerspexProperty<bool> CanScrollHorizontallyProperty =
            PerspexProperty.RegisterAttached<ScrollViewer, Control, bool>(nameof(CanScrollHorizontally), true);

        /// <summary>
        /// Defines the <see cref="Extent"/> property.
        /// </summary>
        public static readonly PerspexProperty<Size> ExtentProperty =
            PerspexProperty.Register<ScrollViewer, Size>(nameof(Extent));

        /// <summary>
        /// Defines the <see cref="Offset"/> property.
        /// </summary>
        public static readonly PerspexProperty<Vector> OffsetProperty =
            PerspexProperty.Register<ScrollViewer, Vector>(nameof(Offset), validate: ValidateOffset);

        /// <summary>
        /// Defines the <see cref="Viewport"/> property.
        /// </summary>
        public static readonly PerspexProperty<Size> ViewportProperty =
            PerspexProperty.Register<ScrollViewer, Size>(nameof(Viewport));

        /// <summary>
        /// Defines the HorizontalScrollBarMaximum property.
        /// </summary>
        /// <remarks>
        /// There is no C# accessor for this property as it is intended to be bound to by a 
        /// <see cref="ScrollContentPresenter"/> in the control's template.
        /// </remarks>
        public static readonly PerspexProperty<double> HorizontalScrollBarMaximumProperty =
            PerspexProperty.Register<ScrollViewer, double>("HorizontalScrollBarMaximum");

        /// <summary>
        /// Defines the HorizontalScrollBarValue property.
        /// </summary>
        /// <remarks>
        /// There is no C# accessor for this property as it is intended to be bound to by a 
        /// <see cref="ScrollContentPresenter"/> in the control's template.
        /// </remarks>
        public static readonly PerspexProperty<double> HorizontalScrollBarValueProperty =
            PerspexProperty.Register<ScrollViewer, double>("HorizontalScrollBarValue");

        /// <summary>
        /// Defines the HorizontalScrollBarViewportSize property.
        /// </summary>
        /// <remarks>
        /// There is no C# accessor for this property as it is intended to be bound to by a 
        /// <see cref="ScrollContentPresenter"/> in the control's template.
        /// </remarks>
        public static readonly PerspexProperty<double> HorizontalScrollBarViewportSizeProperty =
            PerspexProperty.Register<ScrollViewer, double>("HorizontalScrollBarViewportSize");

        /// <summary>
        /// Defines the <see cref="HorizontalScrollBarVisibility"/> property.
        /// </summary>
        /// <remarks>
        /// There is no C# accessor for this property as it is intended to be bound to by a 
        /// <see cref="ScrollContentPresenter"/> in the control's template.
        /// </remarks>
        public static readonly PerspexProperty<ScrollBarVisibility> HorizontalScrollBarVisibilityProperty =
            PerspexProperty.RegisterAttached<ScrollBar, Control, ScrollBarVisibility>(
                nameof(HorizontalScrollBarVisibility),
                ScrollBarVisibility.Auto);

        /// <summary>
        /// Defines the VerticalScrollBarMaximum property.
        /// </summary>
        /// <remarks>
        /// There is no C# accessor for this property as it is intended to be bound to by a 
        /// <see cref="ScrollContentPresenter"/> in the control's template.
        /// </remarks>
        public static readonly PerspexProperty<double> VerticalScrollBarMaximumProperty =
            PerspexProperty.Register<ScrollViewer, double>("VerticalScrollBarMaximum");

        /// <summary>
        /// Defines the VerticalScrollBarValue property.
        /// </summary>
        /// <remarks>
        /// There is no C# accessor for this property as it is intended to be bound to by a 
        /// <see cref="ScrollContentPresenter"/> in the control's template.
        /// </remarks>
        public static readonly PerspexProperty<double> VerticalScrollBarValueProperty =
            PerspexProperty.Register<ScrollViewer, double>("VerticalScrollBarValue");

        /// <summary>
        /// Defines the VerticalScrollBarViewportSize property.
        /// </summary>
        /// <remarks>
        /// There is no C# accessor for this property as it is intended to be bound to by a 
        /// <see cref="ScrollContentPresenter"/> in the control's template.
        /// </remarks>
        public static readonly PerspexProperty<double> VerticalScrollBarViewportSizeProperty =
            PerspexProperty.Register<ScrollViewer, double>("VerticalScrollBarViewportSize");

        /// <summary>
        /// Defines the <see cref="VerticalScrollBarVisibility"/> property.
        /// </summary>
        public static readonly PerspexProperty<ScrollBarVisibility> VerticalScrollBarVisibilityProperty =
            PerspexProperty.RegisterAttached<ScrollViewer, Control, ScrollBarVisibility>(
                nameof(VerticalScrollBarVisibility), 
                ScrollBarVisibility.Auto);

        private IDisposable _scrollableSubscription;

        /// <summary>
        /// Initializes static members of the <see cref="ScrollViewer"/> class.
        /// </summary>
        static ScrollViewer()
        {
            AffectsValidation(ExtentProperty, OffsetProperty);
            AffectsValidation(ViewportProperty, OffsetProperty);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScrollViewer"/> class.
        /// </summary>
        public ScrollViewer()
        {
            var extentAndViewport = Observable.CombineLatest(
                GetObservable(ExtentProperty),
                GetObservable(ViewportProperty))
                .Select(x => new { Extent = x[0], Viewport = x[1] });

            Bind(
                VerticalScrollBarViewportSizeProperty,
                extentAndViewport.Select(x => Max((x.Viewport.Height / x.Extent.Height) * (x.Extent.Height - x.Viewport.Height), 0)));

            Bind(
                HorizontalScrollBarViewportSizeProperty,
                extentAndViewport.Select(x => Max((x.Viewport.Width / x.Extent.Width) * (x.Extent.Width - x.Viewport.Width), 0)));

            Bind(
                HorizontalScrollBarMaximumProperty,
                extentAndViewport.Select(x => Max(x.Extent.Width - x.Viewport.Width, 0)));

            Bind(
                VerticalScrollBarMaximumProperty,
                extentAndViewport.Select(x => Max(x.Extent.Height - x.Viewport.Height, 0)));

            GetObservable(OffsetProperty).Subscribe(x =>
            {
                SetValue(HorizontalScrollBarValueProperty, x.X);
                SetValue(VerticalScrollBarValueProperty, x.Y);
            });

            var scrollBarOffset = Observable.CombineLatest(
                GetObservable(HorizontalScrollBarValueProperty),
                GetObservable(VerticalScrollBarValueProperty))
                .Select(x => new Vector(x[0], x[1]))
                .Subscribe(x => Offset = x);
        }

        /// <summary>
        /// Gets the extent of the scrollable content.
        /// </summary>
        public Size Extent
        {
            get { return GetValue(ExtentProperty); }
            private set { SetValue(ExtentProperty, value); }
        }

        /// <summary>
        /// Gets or sets the current scroll offset.
        /// </summary>
        public Vector Offset
        {
            get { return GetValue(OffsetProperty); }
            set { SetValue(OffsetProperty, value); }
        }

        /// <summary>
        /// Gets the size of the viewport on the scrollable content.
        /// </summary>
        public Size Viewport
        {
            get { return GetValue(ViewportProperty); }
            private set { SetValue(ViewportProperty, value); }
        }

        /// <summary>
        /// Gets a value indicating whether the content can be scrolled horizontally.
        /// </summary>
        public bool CanScrollHorizontally
        {
            get { return GetValue(CanScrollHorizontallyProperty); }
            set { SetValue(CanScrollHorizontallyProperty, value); }
        }

        /// <summary>
        /// Gets or sets the horizontal scrollbar visibility.
        /// </summary>
        public ScrollBarVisibility HorizontalScrollBarVisibility
        {
            get { return GetValue(HorizontalScrollBarVisibilityProperty); }
            set { SetValue(HorizontalScrollBarVisibilityProperty, value); }
        }

        /// <summary>
        /// Gets or sets the vertical scrollbar visibility.
        /// </summary>
        public ScrollBarVisibility VerticalScrollBarVisibility
        {
            get { return GetValue(VerticalScrollBarVisibilityProperty); }
            set { SetValue(VerticalScrollBarVisibilityProperty, value); }
        }

        internal static Vector CoerceOffset(Size extent, Size viewport, Vector offset)
        {
            var maxX = Math.Max(extent.Width - viewport.Width, 0);
            var maxY = Math.Max(extent.Height - viewport.Height, 0);
            return new Vector(Clamp(offset.X, 0, maxX), Clamp(offset.Y, 0, maxY));
        }

        /// <inheritdoc/>
        protected override Size MeasureOverride(Size availableSize)
        {
            return base.MeasureOverride(availableSize);
        }

        private static double Clamp(double value, double min, double max)
        {
            return (value < min) ? min : (value > max) ? max : value;
        }

        private static double Max(double x, double y)
        {
            var result = Math.Max(x, y);
            return double.IsNaN(result) ? 0 : Math.Round(result);
        }

        private static Vector ValidateOffset(PerspexObject o, Vector value)
        {
            ScrollViewer scrollViewer = o as ScrollViewer;

            if (scrollViewer != null)
            {
                var extent = scrollViewer.Extent;
                var viewport = scrollViewer.Viewport;
                return CoerceOffset(extent, viewport, value);
            }
            else
            {
                return value;
            }
        }
    }
}
