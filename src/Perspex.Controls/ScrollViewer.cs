// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Linq;
using Perspex.Controls.Presenters;
using Perspex.Controls.Primitives;

namespace Perspex.Controls
{
    public class ScrollViewer : ContentControl
    {
        public static readonly PerspexProperty<Size> ExtentProperty =
            PerspexProperty.Register<ScrollViewer, Size>("Extent");

        public static readonly PerspexProperty<Vector> OffsetProperty =
            PerspexProperty.Register<ScrollViewer, Vector>("Offset", validate: ValidateOffset);

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
            PerspexProperty.RegisterAttached<ScrollViewer, Control, bool>("CanScrollHorizontally", true);

        public static readonly PerspexProperty<ScrollBarVisibility> HorizontalScrollBarVisibilityProperty =
            PerspexProperty.RegisterAttached<ScrollBar, Control, ScrollBarVisibility>("HorizontalScrollBarVisibility", ScrollBarVisibility.Auto);

        public static readonly PerspexProperty<ScrollBarVisibility> VerticalScrollBarVisibilityProperty =
            PerspexProperty.RegisterAttached<ScrollViewer, Control, ScrollBarVisibility>("VerticalScrollBarVisibility", ScrollBarVisibility.Auto);

        static ScrollViewer()
        {
            AffectsValidation(ExtentProperty, OffsetProperty);
            AffectsValidation(ViewportProperty, OffsetProperty);
        }

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

        public Size Extent
        {
            get { return GetValue(ExtentProperty); }
            private set { SetValue(ExtentProperty, value); }
        }

        public Vector Offset
        {
            get { return GetValue(OffsetProperty); }
            set { SetValue(OffsetProperty, value); }
        }

        public Size Viewport
        {
            get { return GetValue(ViewportProperty); }
            private set { SetValue(ViewportProperty, value); }
        }

        public bool CanScrollHorizontally
        {
            get { return GetValue(CanScrollHorizontallyProperty); }
            set { SetValue(CanScrollHorizontallyProperty, value); }
        }

        // HACK: Currently exposed because XAML bindings don't work with attached perspex properties.
        public double HorizontalScrollBarMaximum
        {
            get { return GetValue(HorizontalScrollBarMaximumProperty); }
            private set { SetValue(HorizontalScrollBarMaximumProperty, value); }
        }

        // HACK: Currently exposed because XAML bindings don't work with attached perspex properties.
        public double HorizontalScrollBarValue
        {
            get { return GetValue(HorizontalScrollBarValueProperty); }
            private set { SetValue(HorizontalScrollBarValueProperty, value); }
        }

        // HACK: Currently exposed because XAML bindings don't work with attached perspex properties.
        public double HorizontalScrollBarViewportSize
        {
            get { return GetValue(HorizontalScrollBarViewportSizeProperty); }
            set { SetValue(HorizontalScrollBarViewportSizeProperty, value); }
        }

        public ScrollBarVisibility HorizontalScrollBarVisibility
        {
            get { return GetValue(HorizontalScrollBarVisibilityProperty); }
            set { SetValue(HorizontalScrollBarVisibilityProperty, value); }
        }

        // HACK: Currently exposed because XAML bindings don't work with attached perspex properties.
        public double VerticalScrollBarMaximum
        {
            get { return GetValue(VerticalScrollBarMaximumProperty); }
            private set { SetValue(VerticalScrollBarMaximumProperty, value); }
        }

        // HACK: Currently exposed because XAML bindings don't work with attached perspex properties.
        public double VerticalScrollBarValue
        {
            get { return GetValue(VerticalScrollBarValueProperty); }
            private set { SetValue(VerticalScrollBarValueProperty, value); }
        }

        // HACK: Currently exposed because XAML bindings don't work with attached perspex properties.
        public double VerticalScrollBarViewportSize
        {
            get { return GetValue(VerticalScrollBarViewportSizeProperty); }
            private set { SetValue(VerticalScrollBarViewportSizeProperty, value); }
        }

        public ScrollBarVisibility VerticalScrollBarVisibility
        {
            get { return GetValue(VerticalScrollBarVisibilityProperty); }
            set { SetValue(VerticalScrollBarVisibilityProperty, value); }
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

        private static Vector ValidateOffset(PerspexObject o, Vector value)
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
