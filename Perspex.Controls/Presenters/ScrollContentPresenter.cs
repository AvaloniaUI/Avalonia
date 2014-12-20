// -----------------------------------------------------------------------
// <copyright file="ScrollContentPresenter.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls.Presenters
{
    using System;
    using System.Linq;
    using Perspex.Input;
    using Perspex.Layout;
    using Perspex.VisualTree;

    public class ScrollContentPresenter : ContentPresenter
    {
        public static readonly PerspexProperty<Size> ExtentProperty =
            ScrollViewer.ExtentProperty.AddOwner<ScrollContentPresenter>();

        public static readonly PerspexProperty<Vector> OffsetProperty =
            ScrollViewer.OffsetProperty.AddOwner<ScrollContentPresenter>();

        public static readonly PerspexProperty<Size> ViewportProperty =
            ScrollViewer.ViewportProperty.AddOwner<ScrollContentPresenter>();

        public static readonly PerspexProperty<bool> CanScrollHorizontallyProperty =
            PerspexProperty.Register<ScrollContentPresenter, bool>("CanScrollHorizontally", true);

        private IDisposable contentBindings;

        static ScrollContentPresenter()
        {
            ClipToBoundsProperty.OverrideDefaultValue(typeof(ScrollContentPresenter), true);
            Control.AffectsArrange(OffsetProperty);
        }

        public ScrollContentPresenter()
        {
            this.AddHandler(
                Control.RequestBringIntoViewEvent,
                new EventHandler<RequestBringIntoViewEventArgs>(this.BringIntoViewRequested));
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
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var content = this.Content as ILayoutable;

            if (content != null)
            {
                var measureSize = new Size(double.PositiveInfinity, double.PositiveInfinity);

                if (!this.CanScrollHorizontally)
                {
                    measureSize = measureSize.WithWidth(availableSize.Width);
                }

                content.Measure(measureSize);
                var size = content.DesiredSize.Value;
                this.Extent = size;
                return size.Constrain(availableSize);
            }
            else
            {
                return this.Extent = new Size();
            }
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var child = this.GetVisualChildren().SingleOrDefault() as ILayoutable;

            this.Viewport = finalSize;

            if (child != null)
            {
                child.Arrange(new Rect((Point)(-this.Offset), child.DesiredSize.Value));
                return finalSize;
            }

            return new Size();
        }

        protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
        {
            if (this.Extent.Height > this.Viewport.Height)
            {
                var y = this.Offset.Y + (-e.Delta.Y * 50);
                y = Math.Max(y, 0);
                y = Math.Min(y, this.Extent.Height - this.Viewport.Height);
                this.Offset = new Vector(this.Offset.X, y);
                e.Handled = true;
            }
        }

        private void BringIntoViewRequested(object sender, RequestBringIntoViewEventArgs e)
        {
            var transform = e.TargetObject.TransformToVisual(this.GetVisualChildren().Single());
            var rect = e.TargetRect * transform;
            var offset = this.Offset;

            if (rect.Bottom > offset.Y + this.Viewport.Height)
            {
                offset = offset.WithY(rect.Bottom - this.Viewport.Height);
                e.Handled = true;
            }

            if (rect.Y < offset.Y)
            {
                offset = offset.WithY(rect.Y);
                e.Handled = true;
            }

            if (rect.Right > offset.X + this.Viewport.Width)
            {
                offset = offset.WithX(rect.Right - this.Viewport.Width);
                e.Handled = true;
            }

            if (rect.X < offset.X)
            {
                offset = offset.WithX(rect.X);
                e.Handled = true;
            }

            this.Offset = offset;
        }
    }
}
