// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using Perspex.VisualTree;
using Serilog;
using Serilog.Core.Enrichers;

namespace Perspex.Layout
{
    /// <summary>
    /// Defines how a control aligns itself horizontally in its parent control.
    /// </summary>
    public enum HorizontalAlignment
    {
        /// <summary>
        /// The control stretches to fill the width of the parent control.
        /// </summary>
        Stretch,

        /// <summary>
        /// The control aligns itself to the left of the parent control.
        /// </summary>
        Left,

        /// <summary>
        /// The control centers itself in the parent control.
        /// </summary>
        Center,

        /// <summary>
        /// The control aligns itself to the right of the parent control.
        /// </summary>
        Right,
    }

    /// <summary>
    /// Defines how a control aligns itself vertically in its parent control.
    /// </summary>
    public enum VerticalAlignment
    {
        /// <summary>
        /// The control stretches to fill the height of the parent control.
        /// </summary>
        Stretch,

        /// <summary>
        /// The control aligns itself to the top of the parent control.
        /// </summary>
        Top,

        /// <summary>
        /// The control centers itself within the parent control.
        /// </summary>
        Center,

        /// <summary>
        /// The control aligns itself to the bottom of the parent control.
        /// </summary>
        Bottom,
    }

    /// <summary>
    /// Implements layout-related functionality for a control.
    /// </summary>
    public class Layoutable : Visual, ILayoutable
    {
        /// <summary>
        /// Defines the <see cref="Width"/> property.
        /// </summary>
        public static readonly PerspexProperty<double> WidthProperty =
            PerspexProperty.Register<Layoutable, double>(nameof(Width), double.NaN);

        /// <summary>
        /// Defines the <see cref="Height"/> property.
        /// </summary>
        public static readonly PerspexProperty<double> HeightProperty =
            PerspexProperty.Register<Layoutable, double>(nameof(Height), double.NaN);

        /// <summary>
        /// Defines the <see cref="MinWidth"/> property.
        /// </summary>
        public static readonly PerspexProperty<double> MinWidthProperty =
            PerspexProperty.Register<Layoutable, double>(nameof(MinWidth));

        /// <summary>
        /// Defines the <see cref="MaxWidth"/> property.
        /// </summary>
        public static readonly PerspexProperty<double> MaxWidthProperty =
            PerspexProperty.Register<Layoutable, double>(nameof(MaxWidth), double.PositiveInfinity);

        /// <summary>
        /// Defines the <see cref="MinHeight"/> property.
        /// </summary>
        public static readonly PerspexProperty<double> MinHeightProperty =
            PerspexProperty.Register<Layoutable, double>(nameof(MinHeight));

        /// <summary>
        /// Defines the <see cref="MaxHeight"/> property.
        /// </summary>
        public static readonly PerspexProperty<double> MaxHeightProperty =
            PerspexProperty.Register<Layoutable, double>(nameof(MaxHeight), double.PositiveInfinity);

        /// <summary>
        /// Defines the <see cref="Margin"/> property.
        /// </summary>
        public static readonly PerspexProperty<Thickness> MarginProperty =
            PerspexProperty.Register<Layoutable, Thickness>(nameof(Margin));

        /// <summary>
        /// Defines the <see cref="HorizontalAlignment"/> property.
        /// </summary>
        public static readonly PerspexProperty<HorizontalAlignment> HorizontalAlignmentProperty =
            PerspexProperty.Register<Layoutable, HorizontalAlignment>(nameof(HorizontalAlignment));

        /// <summary>
        /// Defines the <see cref="VerticalAlignment"/> property.
        /// </summary>
        public static readonly PerspexProperty<VerticalAlignment> VerticalAlignmentProperty =
            PerspexProperty.Register<Layoutable, VerticalAlignment>(nameof(VerticalAlignment));

        /// <summary>
        /// Defines the <see cref="UseLayoutRoundingProperty"/> property.
        /// </summary>
        public static readonly PerspexProperty<bool> UseLayoutRoundingProperty =
            PerspexProperty.Register<Layoutable, bool>(nameof(UseLayoutRounding), defaultValue: true, inherits: true);

        private Size? _previousMeasure;

        private Rect? _previousArrange;

        private readonly ILogger _layoutLog;

        /// <summary>
        /// Initializes static members of the <see cref="Layoutable"/> class.
        /// </summary>
        static Layoutable()
        {
            AffectsMeasure(IsVisibleProperty);
            AffectsMeasure(WidthProperty);
            AffectsMeasure(HeightProperty);
            AffectsMeasure(MinWidthProperty);
            AffectsMeasure(MaxWidthProperty);
            AffectsMeasure(MinHeightProperty);
            AffectsMeasure(MaxHeightProperty);
            AffectsMeasure(MarginProperty);
            AffectsMeasure(HorizontalAlignmentProperty);
            AffectsMeasure(VerticalAlignmentProperty);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Layoutable"/> class.
        /// </summary>
        public Layoutable()
        {
            _layoutLog = Log.ForContext(new[]
            {
                new PropertyEnricher("Area", "Layout"),
                new PropertyEnricher("SourceContext", GetType()),
                new PropertyEnricher("Id", GetHashCode()),
            });
        }

        /// <summary>
        /// Gets or sets the width of the element.
        /// </summary>
        public double Width
        {
            get { return GetValue(WidthProperty); }
            set { SetValue(WidthProperty, value); }
        }

        /// <summary>
        /// Gets or sets the height of the element.
        /// </summary>
        public double Height
        {
            get { return GetValue(HeightProperty); }
            set { SetValue(HeightProperty, value); }
        }

        /// <summary>
        /// Gets or sets the minimum width of the element.
        /// </summary>
        public double MinWidth
        {
            get { return GetValue(MinWidthProperty); }
            set { SetValue(MinWidthProperty, value); }
        }

        /// <summary>
        /// Gets or sets the maximum width of the element.
        /// </summary>
        public double MaxWidth
        {
            get { return GetValue(MaxWidthProperty); }
            set { SetValue(MaxWidthProperty, value); }
        }

        /// <summary>
        /// Gets or sets the minimum height of the element.
        /// </summary>
        public double MinHeight
        {
            get { return GetValue(MinHeightProperty); }
            set { SetValue(MinHeightProperty, value); }
        }

        /// <summary>
        /// Gets or sets the maximum height of the element.
        /// </summary>
        public double MaxHeight
        {
            get { return GetValue(MaxHeightProperty); }
            set { SetValue(MaxHeightProperty, value); }
        }

        /// <summary>
        /// Gets or sets the margin around the element.
        /// </summary>
        public Thickness Margin
        {
            get { return GetValue(MarginProperty); }
            set { SetValue(MarginProperty, value); }
        }

        /// <summary>
        /// Gets or sets the element's preferred horizontal alignment in its parent.
        /// </summary>
        public HorizontalAlignment HorizontalAlignment
        {
            get { return GetValue(HorizontalAlignmentProperty); }
            set { SetValue(HorizontalAlignmentProperty, value); }
        }

        /// <summary>
        /// Gets or sets the element's preferred vertical alignment in its parent.
        /// </summary>
        public VerticalAlignment VerticalAlignment
        {
            get { return GetValue(VerticalAlignmentProperty); }
            set { SetValue(VerticalAlignmentProperty, value); }
        }

        /// <summary>
        /// Gets the size that this element computed during the measure pass of the layout process.
        /// </summary>
        public Size DesiredSize
        {
            get;
            set;
        }

        /// <summary>
        /// Gets a value indicating whether the control's layout measure is valid.
        /// </summary>
        public bool IsMeasureValid
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets a value indicating whether the control's layouts arrange is valid.
        /// </summary>
        public bool IsArrangeValid
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets a value that determines whether the element should be snapped to pixel
        /// boundaries at layout time.
        /// </summary>
        public bool UseLayoutRounding
        {
            get { return GetValue(UseLayoutRoundingProperty); }
            set { SetValue(UseLayoutRoundingProperty, value); }
        }

        /// <summary>
        /// Gets the available size passed in the previous layout pass, if any.
        /// </summary>
        Size? ILayoutable.PreviousMeasure => _previousMeasure;

        /// <summary>
        /// Gets the layout rect passed in the previous layout pass, if any.
        /// </summary>
        Rect? ILayoutable.PreviousArrange => _previousArrange;

        /// <summary>
        /// Creates the visual children of the control, if necessary
        /// </summary>
        public virtual void ApplyTemplate()
        {
        }

        /// <summary>
        /// Carries out a measure of the control.
        /// </summary>
        /// <param name="availableSize">The available size for the control.</param>
        /// <param name="force">
        /// If true, the control will be measured even if <paramref name="availableSize"/> has not
        /// changed from the last measure.
        /// </param>
        public void Measure(Size availableSize, bool force = false)
        {
            if (double.IsNaN(availableSize.Width) || double.IsNaN(availableSize.Height))
            {
                throw new InvalidOperationException("Cannot call Measure using a size with NaN values.");
            }

            if (force || !IsMeasureValid || _previousMeasure != availableSize)
            {
                IsMeasureValid = true;

                var desiredSize = MeasureCore(availableSize).Constrain(availableSize);

                if (IsInvalidSize(desiredSize))
                {
                    throw new InvalidOperationException("Invalid size returned for Measure.");
                }

                DesiredSize = desiredSize;
                _previousMeasure = availableSize;

                _layoutLog.Verbose("Measure requested {DesiredSize}", DesiredSize);
            }
        }

        /// <summary>
        /// Arranges the control and its children.
        /// </summary>
        /// <param name="rect">The control's new bounds.</param>
        /// <param name="force">
        /// If true, the control will be arranged even if <paramref name="rect"/> has not changed
        /// from the last arrange.
        /// </param>
        public void Arrange(Rect rect, bool force = false)
        {
            if (IsInvalidRect(rect))
            {
                throw new InvalidOperationException("Invalid Arrange rectangle.");
            }

            // If the measure was invalidated during an arrange pass, wait for the measure pass to
            // be re-run.
            if (!IsMeasureValid)
            {
                return;
            }

            if (force || !IsArrangeValid || _previousArrange != rect)
            {
                _layoutLog.Verbose("Arrange to {Rect} ", rect);

                IsArrangeValid = true;
                ArrangeCore(rect);
                _previousArrange = rect;
            }
        }

        /// <summary>
        /// Invalidates the measurement of the control and queues a new layout pass.
        /// </summary>
        public void InvalidateMeasure()
        {
            var parent = this.GetVisualParent<ILayoutable>();

            if (IsMeasureValid)
            {
                _layoutLog.Verbose("Invalidated measure");
            }

            IsMeasureValid = false;
            IsArrangeValid = false;
            _previousMeasure = null;
            _previousArrange = null;

            if (parent != null && IsResizable(parent))
            {
                parent.InvalidateMeasure();
            }
            else
            {
                var root = GetLayoutRoot();
                root?.Item1.LayoutManager?.InvalidateMeasure(this, root.Item2);
            }
        }

        /// <summary>
        /// Invalidates the arrangement of the control and queues a new layout pass.
        /// </summary>
        public void InvalidateArrange()
        {
            var root = GetLayoutRoot();

            if (IsArrangeValid)
            {
                _layoutLog.Verbose("Arrange measure");
            }

            IsArrangeValid = false;
            _previousArrange = null;
            root?.Item1.LayoutManager?.InvalidateArrange(this, root.Item2);
        }

        /// <summary>
        /// Marks a property as affecting the control's measurement.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <remarks>
        /// After a call to this method in a control's static constructor, any change to the
        /// property will cause <see cref="InvalidateMeasure"/> to be called on the element.
        /// </remarks>
        protected static void AffectsMeasure(PerspexProperty property)
        {
            property.Changed.Subscribe(AffectsMeasureInvalidate);
        }

        /// <summary>
        /// Marks a property as affecting the control's arrangement.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <remarks>
        /// After a call to this method in a control's static constructor, any change to the
        /// property will cause <see cref="InvalidateArrange"/> to be called on the element.
        /// </remarks>
        protected static void AffectsArrange(PerspexProperty property)
        {
            property.Changed.Subscribe(AffectsArrangeInvalidate);
        }

        /// <summary>
        /// The default implementation of the control's measure pass.
        /// </summary>
        /// <param name="availableSize">The size available to the control.</param>
        /// <returns>The desired size for the control.</returns>
        /// <remarks>
        /// This method calls <see cref="MeasureOverride(Size)"/> which is probably the method you
        /// want to override in order to modify a control's arrangement.
        /// </remarks>
        protected virtual Size MeasureCore(Size availableSize)
        {
            if (IsVisible)
            {
                ApplyTemplate();

                var constrained = LayoutHelper
                    .ApplyLayoutConstraints(this, availableSize)
                    .Deflate(Margin);

                var measured = MeasureOverride(constrained);
                var width = measured.Width;
                var height = measured.Height;

                if (!double.IsNaN(Width))
                {
                    width = Width;
                }

                width = Math.Min(width, MaxWidth);
                width = Math.Max(width, MinWidth);

                if (!double.IsNaN(Height))
                {
                    height = Height;
                }

                height = Math.Min(height, MaxHeight);
                height = Math.Max(height, MinHeight);

                return new Size(width, height).Inflate(Margin);
            }
            else
            {
                return new Size();
            }
        }

        /// <summary>
        /// Measures the control and its child elements as part of a layout pass.
        /// </summary>
        /// <param name="availableSize">The size available to the control.</param>
        /// <returns>The desired size for the control.</returns>
        protected virtual Size MeasureOverride(Size availableSize)
        {
            double width = 0;
            double height = 0;

            foreach (ILayoutable child in this.GetVisualChildren().OfType<ILayoutable>())
            {
                child.Measure(availableSize);
                width = Math.Max(width, child.DesiredSize.Width);
                height = Math.Max(height, child.DesiredSize.Height);
            }

            if (UseLayoutRounding)
            {
                width = Math.Ceiling(width);
                height = Math.Ceiling(height);
            }

            return new Size(width, height);
        }

        /// <summary>
        /// The default implementation of the control's arrange pass.
        /// </summary>
        /// <param name="finalRect">The control's new bounds.</param>
        /// <remarks>
        /// This method calls <see cref="ArrangeOverride(Size)"/> which is probably the method you
        /// want to override in order to modify a control's arrangement.
        /// </remarks>
        protected virtual void ArrangeCore(Rect finalRect)
        {
            if (IsVisible)
            {
                double originX = finalRect.X + Margin.Left;
                double originY = finalRect.Y + Margin.Top;
                var sizeMinusMargins = new Size(
                    Math.Max(0, finalRect.Width - Margin.Left - Margin.Right),
                    Math.Max(0, finalRect.Height - Margin.Top - Margin.Bottom));
                var size = sizeMinusMargins;

                if (HorizontalAlignment != HorizontalAlignment.Stretch)
                {
                    size = size.WithWidth(Math.Min(size.Width, DesiredSize.Width));
                }

                if (VerticalAlignment != VerticalAlignment.Stretch)
                {
                    size = size.WithHeight(Math.Min(size.Height, DesiredSize.Height));
                }

                size = LayoutHelper.ApplyLayoutConstraints(this, size);

                if (UseLayoutRounding)
                {
                    size = new Size(Math.Ceiling(size.Width), Math.Ceiling(size.Height));
                }

                size = ArrangeOverride(size).Constrain(size);

                switch (HorizontalAlignment)
                {
                    case HorizontalAlignment.Center:
                    case HorizontalAlignment.Stretch:
                        originX += (sizeMinusMargins.Width - size.Width) / 2;
                        break;
                    case HorizontalAlignment.Right:
                        originX += sizeMinusMargins.Width - size.Width;
                        break;
                }

                switch (VerticalAlignment)
                {
                    case VerticalAlignment.Center:
                    case VerticalAlignment.Stretch:
                        originY += (sizeMinusMargins.Height - size.Height) / 2;
                        break;
                    case VerticalAlignment.Bottom:
                        originY += sizeMinusMargins.Height - size.Height;
                        break;
                }

                if (UseLayoutRounding)
                {
                    originX = Math.Floor(originX);
                    originY = Math.Floor(originY);
                }

                Bounds = new Rect(originX, originY, size.Width, size.Height);
            }
        }

        /// <summary>
        /// Positions child elements as part of a layout pass.
        /// </summary>
        /// <param name="finalSize">The size available to the control.</param>
        /// <returns>The actual size used.</returns>
        protected virtual Size ArrangeOverride(Size finalSize)
        {
            foreach (ILayoutable child in this.GetVisualChildren().OfType<ILayoutable>())
            {
                child.Arrange(new Rect(finalSize));
            }

            return finalSize;
        }

        /// <summary>
        /// Calls <see cref="InvalidateMeasure"/> on the control on which a property changed.
        /// </summary>
        /// <param name="e">The event args.</param>
        private static void AffectsMeasureInvalidate(PerspexPropertyChangedEventArgs e)
        {
            ILayoutable control = e.Sender as ILayoutable;
            control?.InvalidateMeasure();
        }

        /// <summary>
        /// Calls <see cref="InvalidateArrange"/> on the control on which a property changed.
        /// </summary>
        /// <param name="e">The event args.</param>
        private static void AffectsArrangeInvalidate(PerspexPropertyChangedEventArgs e)
        {
            ILayoutable control = e.Sender as ILayoutable;
            control?.InvalidateArrange();
        }

        /// <summary>
        /// Tests whether a control's size can be changed by a layout pass.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <returns>True if the control's size can change; otherwise false.</returns>
        private static bool IsResizable(ILayoutable control)
        {
            return double.IsNaN(control.Width) || double.IsNaN(control.Height);
        }

        /// <summary>
        /// Tests whether any of a <see cref="Rect"/>'s properties incude nagative values,
        /// a NaN or Infinity.
        /// </summary>
        /// <param name="rect">The rect.</param>
        /// <returns>True if the rect is invalid; otherwise false.</returns>
        private static bool IsInvalidRect(Rect rect)
        {
            return rect.Width < 0 || rect.Height < 0 ||
                double.IsInfinity(rect.X) || double.IsInfinity(rect.Y) ||
                double.IsInfinity(rect.Width) || double.IsInfinity(rect.Height) ||
                double.IsNaN(rect.X) || double.IsNaN(rect.Y) ||
                double.IsNaN(rect.Width) || double.IsNaN(rect.Height);
        }

        /// <summary>
        /// Tests whether any of a <see cref="Size"/>'s properties incude nagative values,
        /// a NaN or Infinity.
        /// </summary>
        /// <param name="size">The size.</param>
        /// <returns>True if the size is invalid; otherwise false.</returns>
        private static bool IsInvalidSize(Size size)
        {
            return size.Width < 0 || size.Height < 0 ||
                double.IsInfinity(size.Width) || double.IsInfinity(size.Height) ||
                double.IsNaN(size.Width) || double.IsNaN(size.Height);
        }

        /// <summary>
        /// Gets the layout root, together with its distance.
        /// </summary>
        /// <returns>
        /// A tuple containing the layout root and the root's distance from this control.
        /// </returns>
        private Tuple<ILayoutRoot, int> GetLayoutRoot()
        {
            var control = (IVisual)this;
            var distance = 0;

            while (control != null && !(control is ILayoutRoot))
            {
                control = control.GetVisualParent();
                ++distance;
            }

            return control != null ? Tuple.Create((ILayoutRoot)control, distance) : null;
        }
    }
}
