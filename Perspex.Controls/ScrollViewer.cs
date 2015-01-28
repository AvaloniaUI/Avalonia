// -----------------------------------------------------------------------
// <copyright file="ScrollViewer.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System;
    using System.Reactive.Linq;
    using Perspex.Controls.Presenters;
    using Perspex.Controls.Primitives;

    public class ScrollViewer : ContentControl
    {
        public static readonly PerspexProperty<Size> ExtentProperty =
            PerspexProperty.Register<ScrollViewer, Size>("Extent");

        public static readonly PerspexProperty<Vector> OffsetProperty =
            PerspexProperty.Register<ScrollViewer, Vector>("Offset", coerce: CoerceOffset);

        public static readonly PerspexProperty<Size> ViewportProperty =
            PerspexProperty.Register<ScrollViewer, Size>("Viewport");

        public static readonly PerspexProperty<double> HorizontalScrollBarMaximumProperty =
            PerspexProperty.Register<ScrollViewer, double>("HorizontalScrollBarMaximum");

        public static readonly PerspexProperty<double> HorizontalScrollBarValueProperty =
            PerspexProperty.Register<ScrollViewer, double>("HorizontalScrollBarValue");

        public static readonly PerspexProperty<double> HorizontalScrollBarViewportSizeProperty =
            PerspexProperty.Register<ScrollViewer, double>("HorizontalScrollBarViewportSize");

        public static readonly PerspexProperty<double> VerticalScrollBarMaximumProperty =
            PerspexProperty.Register<ScrollViewer, double>("VerticalScrollBarMaximum");

        public static readonly PerspexProperty<double> VerticalScrollBarValueProperty =
            PerspexProperty.Register<ScrollViewer, double>("VerticalScrollBarValue");

        public static readonly PerspexProperty<double> VerticalScrollBarViewportSizeProperty =
            PerspexProperty.Register<ScrollViewer, double>("VerticalScrollBarViewportSize");

        public static readonly PerspexProperty<bool> CanScrollHorizontallyProperty =
            PerspexProperty.RegisterAttached<ScrollViewer, Control, bool>("CanScrollHorizontally", false);

        public static readonly PerspexProperty<ScrollBarVisibility> HorizontalScrollBarVisibilityProperty =
            PerspexProperty.RegisterAttached<ScrollBar, Control, ScrollBarVisibility>("HorizontalScrollBarVisibility", ScrollBarVisibility.Auto);

        public static readonly PerspexProperty<ScrollBarVisibility> VerticalScrollBarVisibilityProperty =
            PerspexProperty.RegisterAttached<ScrollViewer, Control, ScrollBarVisibility>("VerticalScrollBarVisibility", ScrollBarVisibility.Auto);

        static ScrollViewer()
        {
            AffectsCoercion(ExtentProperty, OffsetProperty);
            AffectsCoercion(ViewportProperty, OffsetProperty);
        }

        public ScrollViewer()
        {
            var extentAndViewport = Observable.CombineLatest(
                this.GetObservable(ExtentProperty),
                this.GetObservable(ViewportProperty))
                .Select(x => new { Extent = x[0], Viewport = x[1] });

            this.Bind(
                VerticalScrollBarViewportSizeProperty,
                extentAndViewport.Select(x => Max((x.Viewport.Height / x.Extent.Height) * (x.Extent.Height - x.Viewport.Height), 0)));

            this.Bind(
                HorizontalScrollBarViewportSizeProperty,
                extentAndViewport.Select(x => Max((x.Viewport.Width / x.Extent.Width) * (x.Extent.Width - x.Viewport.Width), 0)));

            this.Bind(
                HorizontalScrollBarMaximumProperty,
                extentAndViewport.Select(x => Max(x.Extent.Width - x.Viewport.Width, 0)));

            this.Bind(
                VerticalScrollBarMaximumProperty,
                extentAndViewport.Select(x => Max(x.Extent.Height - x.Viewport.Height, 0)));

            this.GetObservable(OffsetProperty).Subscribe(x =>
            {
                this.SetValue(HorizontalScrollBarValueProperty, x.X);
                this.SetValue(VerticalScrollBarValueProperty, x.Y);
            });

            var scrollBarOffset = Observable.CombineLatest(
                this.GetObservable(HorizontalScrollBarValueProperty),
                this.GetObservable(VerticalScrollBarValueProperty))
                .Select(x => new Vector(x[0], x[1]))
                .Subscribe(x => this.Offset = x);
        }

        public Size Extent
        {
            get { return this.GetValue(ExtentProperty); }
            private set { this.SetValue(ExtentProperty, value); }
        }

        public Vector Offset
        {
            get { return this.GetValue(OffsetProperty); }
            set { this.SetValue(OffsetProperty, value); }
        }

        public Size Viewport
        {
            get { return this.GetValue(ViewportProperty); }
            private set { this.SetValue(ViewportProperty, value); }
        }

        public bool CanScrollHorizontally
        {
            get { return this.GetValue(CanScrollHorizontallyProperty); }
            set { this.SetValue(CanScrollHorizontallyProperty, value); }
        }

        public ScrollBarVisibility HorizontalScrollBarVisibility
        {
            get { return this.GetValue(HorizontalScrollBarVisibilityProperty); }
            set { this.SetValue(HorizontalScrollBarVisibilityProperty, value); }
        }

        public ScrollBarVisibility VerticalScrollBarVisibility
        {
            get { return this.GetValue(VerticalScrollBarVisibilityProperty); }
            set { this.SetValue(VerticalScrollBarVisibilityProperty, value); }
        }

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

        private static Vector CoerceOffset(PerspexObject o, Vector value)
        {
            ScrollViewer scrollViewer = o as ScrollViewer;

            if (scrollViewer != null)
            {
                var extent = scrollViewer.Extent;
                var viewport = scrollViewer.Viewport;
                var maxX = Math.Max(extent.Width - viewport.Width, 0);
                var maxY = Math.Max(extent.Height - viewport.Height, 0);
                return new Vector(Clamp(value.X, 0, maxX), Clamp(value.Y, 0, maxY));
            }
            else
            {
                return value;
            }
        }
    }
}
