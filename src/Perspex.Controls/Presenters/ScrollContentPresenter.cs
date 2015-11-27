// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using Perspex.Input;
using Perspex.Layout;
using Perspex.VisualTree;

namespace Perspex.Controls.Presenters
{
    /// <summary>
    /// Presents a scrolling view of content inside a <see cref="ScrollViewer"/>.
    /// </summary>
    public class ScrollContentPresenter : ContentPresenter, IPresenter
    {
        /// <summary>
        /// Defines the <see cref="Extent"/> property.
        /// </summary>
        public static readonly PerspexProperty<Size> ExtentProperty =
            ScrollViewer.ExtentProperty.AddOwner<ScrollContentPresenter>();

        /// <summary>
        /// Defines the <see cref="Offset"/> property.
        /// </summary>
        public static readonly PerspexProperty<Vector> OffsetProperty =
            ScrollViewer.OffsetProperty.AddOwner<ScrollContentPresenter>();

        /// <summary>
        /// Defines the <see cref="Viewport"/> property.
        /// </summary>
        public static readonly PerspexProperty<Size> ViewportProperty =
            ScrollViewer.ViewportProperty.AddOwner<ScrollContentPresenter>();

        /// <summary>
        /// Defines the <see cref="CanScrollHorizontally"/> property.
        /// </summary>
        public static readonly PerspexProperty<bool> CanScrollHorizontallyProperty =
            PerspexProperty.Register<ScrollContentPresenter, bool>("CanScrollHorizontally", true);

        private Size _measuredExtent;

        /// <summary>
        /// Initializes static members of the <see cref="ScrollContentPresenter"/> class.
        /// </summary>
        static ScrollContentPresenter()
        {
            ClipToBoundsProperty.OverrideDefaultValue(typeof(ScrollContentPresenter), true);
            OffsetProperty.OverrideValidation<ScrollContentPresenter>(ValidateOffset);
            AffectsArrange(OffsetProperty);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScrollContentPresenter"/> class.
        /// </summary>
        public ScrollContentPresenter()
        {
            AddHandler(RequestBringIntoViewEvent, BringIntoViewRequested);
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
        public bool CanScrollHorizontally => GetValue(CanScrollHorizontallyProperty);

        /// <summary>
        /// Attempts to bring a portion of the target visual into view by scrolling the content.
        /// </summary>
        /// <param name="target">The target visual.</param>
        /// <param name="targetRect">The portion of the target visual to bring into view.</param>
        /// <returns>True if the scroll offset was changed; otherwise false.</returns>
        public bool BringDescendentIntoView(IVisual target, Rect targetRect)
        {
            if (Child != null)
            {
                var transform = target.TransformToVisual(Child);
                var rect = targetRect * transform;
                var offset = Offset;
                var result = false;

                if (rect.Bottom > offset.Y + Viewport.Height)
                {
                    offset = offset.WithY((rect.Bottom - Viewport.Height) + Child.Margin.Top);
                    result = true;
                }

                if (rect.Y < offset.Y)
                {
                    offset = offset.WithY(rect.Y);
                    result = true;
                }

                if (rect.Right > offset.X + Viewport.Width)
                {
                    offset = offset.WithX((rect.Right - Viewport.Width) + Child.Margin.Left);
                    result = true;
                }

                if (rect.X < offset.X)
                {
                    offset = offset.WithX(rect.X);
                    result = true;
                }

                if (result)
                {
                    Offset = offset;
                }

                return result;
            }
            else
            {
                return false;
            }
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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
            e.Handled = BringDescendentIntoView(e.TargetObject, e.TargetRect);
        }

        private static Vector ValidateOffset(ScrollContentPresenter o, Vector value)
        {
            return ScrollViewer.CoerceOffset(
                o.GetValue(ExtentProperty),
                o.GetValue(ViewportProperty),
                value);
        }
    }
}
