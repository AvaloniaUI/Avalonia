// -----------------------------------------------------------------------
// <copyright file="ScrollContentPresenter.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls.Presenters
{
    using System.Linq;
    using Perspex.Layout;

    public class ScrollContentPresenter :  ContentPresenter
    {
        public static PerspexProperty<Size> ExtentProperty =
            ScrollViewer.ExtentProperty.AddOwner<ScrollContentPresenter>();

        public static PerspexProperty<Vector> OffsetProperty =
            ScrollViewer.OffsetProperty.AddOwner<ScrollContentPresenter>();

        public static PerspexProperty<Size> ViewportProperty =
            ScrollViewer.ViewportProperty.AddOwner<ScrollContentPresenter>();

        public ScrollContentPresenter()
        {
            AffectsRender(OffsetProperty);
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

        protected override Size MeasureOverride(Size availableSize)
        {
            var content = this.Content as ILayoutable;

            if (content != null)
            {
                content.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
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
    }
}
