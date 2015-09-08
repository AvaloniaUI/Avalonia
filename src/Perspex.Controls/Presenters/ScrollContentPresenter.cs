// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using Perspex.Input;
using Perspex.Layout;
using Perspex.VisualTree;

namespace Perspex.Controls.Presenters
{
    public class ScrollContentPresenter : ContentPresenter, IPresenter
    {
        public static readonly PerspexProperty<Size> ExtentProperty =
            ScrollViewer.ExtentProperty.AddOwner<ScrollContentPresenter>();

        public static readonly PerspexProperty<Vector> OffsetProperty =
            ScrollViewer.OffsetProperty.AddOwner<ScrollContentPresenter>();

        public static readonly PerspexProperty<Size> ViewportProperty =
            ScrollViewer.ViewportProperty.AddOwner<ScrollContentPresenter>();

        public static readonly PerspexProperty<bool> CanScrollHorizontallyProperty =
            PerspexProperty.Register<ScrollContentPresenter, bool>("CanScrollHorizontally", true);

        private Size _measuredExtent;

        static ScrollContentPresenter()
        {
            ClipToBoundsProperty.OverrideDefaultValue(typeof(ScrollContentPresenter), true);
            AffectsArrange(OffsetProperty);
        }

        public ScrollContentPresenter()
        {
            AddHandler(RequestBringIntoViewEvent, BringIntoViewRequested);
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
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var content = Content as ILayoutable;

            if (content != null)
            {
                var measureSize = new Size(double.PositiveInfinity, double.PositiveInfinity);

                if (!CanScrollHorizontally)
                {
                    measureSize = measureSize.WithWidth(availableSize.Width);
                }

                content.Measure(measureSize);
                var size = content.DesiredSize;
                _measuredExtent = size;
                return size.Constrain(availableSize);
            }
            else
            {
                return Extent = new Size();
            }
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var child = this.GetVisualChildren().SingleOrDefault() as ILayoutable;

            Viewport = finalSize;
            Extent = _measuredExtent;

            if (child != null)
            {
                var size = new Size(
                    Math.Max(finalSize.Width, child.DesiredSize.Width),
                    Math.Max(finalSize.Height, child.DesiredSize.Height));
                child.Arrange(new Rect((Point)(-Offset), size));
                return finalSize;
            }

            return new Size();
        }

        protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
        {
            if (Extent.Height > Viewport.Height)
            {
                var y = Offset.Y + (-e.Delta.Y * 50);
                y = Math.Max(y, 0);
                y = Math.Min(y, Extent.Height - Viewport.Height);
                Offset = new Vector(Offset.X, y);
                e.Handled = true;
            }
        }

        private void BringIntoViewRequested(object sender, RequestBringIntoViewEventArgs e)
        {
            var transform = e.TargetObject.TransformToVisual(this.GetVisualChildren().Single());
            var rect = e.TargetRect * transform;
            var offset = Offset;

            if (rect.Bottom > offset.Y + Viewport.Height)
            {
                offset = offset.WithY(rect.Bottom - Viewport.Height);
                e.Handled = true;
            }

            if (rect.Y < offset.Y)
            {
                offset = offset.WithY(rect.Y);
                e.Handled = true;
            }

            if (rect.Right > offset.X + Viewport.Width)
            {
                offset = offset.WithX(rect.Right - Viewport.Width);
                e.Handled = true;
            }

            if (rect.X < offset.X)
            {
                offset = offset.WithX(rect.X);
                e.Handled = true;
            }

            Offset = offset;
        }
    }
}
