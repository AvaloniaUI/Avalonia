// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.VisualTree;

namespace Avalonia.Controls.Presenters
{
    /// <summary>
    /// Presents a scrolling view of content inside a <see cref="ScrollViewer"/>.
    /// </summary>
    public class ScrollContentPresenter : ContentPresenter, IPresenter, IScrollable
    {
        /// <summary>
        /// Defines the <see cref="Extent"/> property.
        /// </summary>
        public static readonly DirectProperty<ScrollContentPresenter, Size> ExtentProperty =
            ScrollViewer.ExtentProperty.AddOwner<ScrollContentPresenter>(
                o => o.Extent,
                (o, v) => o.Extent = v);

        /// <summary>
        /// Defines the <see cref="Offset"/> property.
        /// </summary>
        public static readonly DirectProperty<ScrollContentPresenter, Vector> OffsetProperty =
            ScrollViewer.OffsetProperty.AddOwner<ScrollContentPresenter>(
                o => o.Offset,
                (o, v) => o.Offset = v);

        /// <summary>
        /// Defines the <see cref="Viewport"/> property.
        /// </summary>
        public static readonly DirectProperty<ScrollContentPresenter, Size> ViewportProperty =
            ScrollViewer.ViewportProperty.AddOwner<ScrollContentPresenter>(
                o => o.Viewport,
                (o, v) => o.Viewport = v);

        /// <summary>
        /// Defines the <see cref="CanScrollHorizontally"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> CanScrollHorizontallyProperty =
            ScrollViewer.CanScrollHorizontallyProperty.AddOwner<ScrollContentPresenter>();

        private Size _extent;
        private Size _measuredExtent;
        private Vector _offset;
        private IDisposable _logicalScrollSubscription;
        private Size _viewport;

        /// <summary>
        /// Initializes static members of the <see cref="ScrollContentPresenter"/> class.
        /// </summary>
        static ScrollContentPresenter()
        {
            ClipToBoundsProperty.OverrideDefaultValue(typeof(ScrollContentPresenter), true);
            ChildProperty.Changed.AddClassHandler<ScrollContentPresenter>(x => x.ChildChanged);
            AffectsArrange(OffsetProperty);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScrollContentPresenter"/> class.
        /// </summary>
        public ScrollContentPresenter()
        {
            AddHandler(RequestBringIntoViewEvent, BringIntoViewRequested);

            this.GetObservable(ChildProperty).Subscribe(UpdateScrollableSubscription);
        }

        /// <summary>
        /// Gets the extent of the scrollable content.
        /// </summary>
        public Size Extent
        {
            get { return _extent; }
            private set { SetAndRaise(ExtentProperty, ref _extent, value); }
        }

        /// <summary>
        /// Gets or sets the current scroll offset.
        /// </summary>
        public Vector Offset
        {
            get { return _offset; }
            set { SetAndRaise(OffsetProperty, ref _offset, value); }
        }

        /// <summary>
        /// Gets the size of the viewport on the scrollable content.
        /// </summary>
        public Size Viewport
        {
            get { return _viewport; }
            private set { SetAndRaise(ViewportProperty, ref _viewport, value); }
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
            if (Child == null)
            {
                return false;
            }

            var scrollable = Child as ILogicalScrollable;
            var control = target as IControl;

            if (scrollable?.IsLogicalScrollEnabled == true && control != null)
            {
                return scrollable.BringIntoView(control, targetRect);
            }

            var transform = target.TransformToVisual(Child);

            if (transform == null)
            {
                return false;
            }

            var rect = targetRect * transform.Value;
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

        /// <inheritdoc/>
        protected override Size MeasureOverride(Size availableSize)
        {
            var child = Child;

            if (child != null)
            {
                var measureSize = availableSize;

                if (_logicalScrollSubscription == null)
                {
                    measureSize = new Size(double.PositiveInfinity, double.PositiveInfinity);

                    if (!CanScrollHorizontally)
                    {
                        measureSize = measureSize.WithWidth(availableSize.Width);
                    }
                }

                child.Measure(measureSize);
                var size = child.DesiredSize;
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
            var logicalScroll = _logicalScrollSubscription != null;

            if (!logicalScroll)
            {
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
            }
            else if (child != null)
            {
                child.Arrange(new Rect(finalSize));
                return finalSize;
            }

            return new Size();
        }

        /// <inheritdoc/>
        protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
        {
            if (Extent.Height > Viewport.Height)
            {
                var scrollable = Child as ILogicalScrollable;

                if (scrollable?.IsLogicalScrollEnabled == true)
                {
                    var y = Offset.Y + (-e.Delta.Y * scrollable.ScrollSize.Height);
                    y = Math.Max(y, 0);
                    y = Math.Min(y, Extent.Height - Viewport.Height);
                    Offset = new Vector(Offset.X, y);
                    e.Handled = true;
                }
                else
                {
                    var y = Offset.Y + (-e.Delta.Y * 50);
                    y = Math.Max(y, 0);
                    y = Math.Min(y, Extent.Height - Viewport.Height);
                    Offset = new Vector(Offset.X, y);
                    e.Handled = true;
                }
            }
            else if (Extent.Width > Viewport.Width)
            {
                var scrollable = Child as ILogicalScrollable;

                if (scrollable?.IsLogicalScrollEnabled == true)
                {
                    var x = Offset.X + (-e.Delta.X * scrollable.ScrollSize.Width);
                    x = Math.Max(x, 0);
                    x = Math.Min(x, Extent.Width - Viewport.Width);
                    Offset = new Vector(x, Offset.Y);
                    e.Handled = true;
                }
                else
                {
                    var x = Offset.X + (-e.Delta.X * 50);
                    x = Math.Max(x, 0);
                    x = Math.Min(x, Extent.Width - Viewport.Width);
                    Offset = new Vector(x, Offset.Y);
                    e.Handled = true;
                }
            }
        }

        private void BringIntoViewRequested(object sender, RequestBringIntoViewEventArgs e)
        {
            e.Handled = BringDescendentIntoView(e.TargetObject, e.TargetRect);
        }

        private void ChildChanged(AvaloniaPropertyChangedEventArgs e)
        {
            UpdateScrollableSubscription((IControl)e.NewValue);

            if (e.OldValue != null)
            {
                Offset = default(Vector);
            }
        }

        private void UpdateScrollableSubscription(IControl child)
        {
            var scrollable = child as ILogicalScrollable;

            _logicalScrollSubscription?.Dispose();
            _logicalScrollSubscription = null;

            if (scrollable != null)
            {
                scrollable.InvalidateScroll = () => UpdateFromScrollable(scrollable);

                if (scrollable.IsLogicalScrollEnabled == true)
                {
                    _logicalScrollSubscription = new CompositeDisposable(
                        this.GetObservable(OffsetProperty).Skip(1).Subscribe(x => scrollable.Offset = x),
                        Disposable.Create(() => scrollable.InvalidateScroll = null));
                    UpdateFromScrollable(scrollable);
                }
            }
        }

        private void UpdateFromScrollable(ILogicalScrollable scrollable)
        {
            var logicalScroll = _logicalScrollSubscription != null;

            if (logicalScroll != scrollable.IsLogicalScrollEnabled)
            {
                UpdateScrollableSubscription(Child);
                Offset = default(Vector);
                InvalidateMeasure();
            }
            else if (scrollable.IsLogicalScrollEnabled)
            {
                Viewport = scrollable.Viewport;
                Extent = scrollable.Extent;
                Offset = scrollable.Offset;
            }
        }
    }
}
