// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using Avalonia.Logging;
using Avalonia.VisualTree;

namespace Avalonia.Layout
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
        /// Defines the <see cref="DesiredSize"/> property.
        /// </summary>
        public static readonly DirectProperty<Layoutable, Size> DesiredSizeProperty =
            AvaloniaProperty.RegisterDirect<Layoutable, Size>(nameof(DesiredSize), o => o.DesiredSize);

        /// <summary>
        /// Defines the <see cref="Width"/> property.
        /// </summary>
        public static readonly StyledProperty<double> WidthProperty =
            AvaloniaProperty.Register<Layoutable, double>(nameof(Width), double.NaN);

        /// <summary>
        /// Defines the <see cref="Height"/> property.
        /// </summary>
        public static readonly StyledProperty<double> HeightProperty =
            AvaloniaProperty.Register<Layoutable, double>(nameof(Height), double.NaN);

        /// <summary>
        /// Defines the <see cref="MinWidth"/> property.
        /// </summary>
        public static readonly StyledProperty<double> MinWidthProperty =
            AvaloniaProperty.Register<Layoutable, double>(nameof(MinWidth));

        /// <summary>
        /// Defines the <see cref="MaxWidth"/> property.
        /// </summary>
        public static readonly StyledProperty<double> MaxWidthProperty =
            AvaloniaProperty.Register<Layoutable, double>(nameof(MaxWidth), double.PositiveInfinity);

        /// <summary>
        /// Defines the <see cref="MinHeight"/> property.
        /// </summary>
        public static readonly StyledProperty<double> MinHeightProperty =
            AvaloniaProperty.Register<Layoutable, double>(nameof(MinHeight));

        /// <summary>
        /// Defines the <see cref="MaxHeight"/> property.
        /// </summary>
        public static readonly StyledProperty<double> MaxHeightProperty =
            AvaloniaProperty.Register<Layoutable, double>(nameof(MaxHeight), double.PositiveInfinity);

        /// <summary>
        /// Defines the <see cref="Margin"/> property.
        /// </summary>
        public static readonly StyledProperty<Thickness> MarginProperty =
            AvaloniaProperty.Register<Layoutable, Thickness>(nameof(Margin));

        /// <summary>
        /// Defines the <see cref="HorizontalAlignment"/> property.
        /// </summary>
        public static readonly StyledProperty<HorizontalAlignment> HorizontalAlignmentProperty =
            AvaloniaProperty.Register<Layoutable, HorizontalAlignment>(nameof(HorizontalAlignment));

        /// <summary>
        /// Defines the <see cref="VerticalAlignment"/> property.
        /// </summary>
        public static readonly StyledProperty<VerticalAlignment> VerticalAlignmentProperty =
            AvaloniaProperty.Register<Layoutable, VerticalAlignment>(nameof(VerticalAlignment));

        /// <summary>
        /// Defines the <see cref="UseLayoutRoundingProperty"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> UseLayoutRoundingProperty =
            AvaloniaProperty.Register<Layoutable, bool>(nameof(UseLayoutRounding), defaultValue: true, inherits: true);

        private bool _measuring;
        private Size? _previousMeasure;
        private Rect? _previousArrange;

        /// <summary>
        /// Initializes static members of the <see cref="Layoutable"/> class.
        /// </summary>
        static Layoutable()
        {
            AffectsMeasure<Layoutable>(
                IsVisibleProperty,
                WidthProperty,
                HeightProperty,
                MinWidthProperty,
                MaxWidthProperty,
                MinHeightProperty,
                MaxHeightProperty,
                MarginProperty,
                HorizontalAlignmentProperty,
                VerticalAlignmentProperty);
        }

        /// <summary>
        /// Occurs when a layout pass completes for the control.
        /// </summary>
        public event EventHandler LayoutUpdated;

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
            private set;
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
        public void Measure(Size availableSize)
        {
            if (double.IsNaN(availableSize.Width) || double.IsNaN(availableSize.Height))
            {
                throw new InvalidOperationException("Cannot call Measure using a size with NaN values.");
            }

            if (!IsMeasureValid || _previousMeasure != availableSize)
            {
                var previousDesiredSize = DesiredSize;
                var desiredSize = default(Size);

                IsMeasureValid = true;

                try
                {
                    _measuring = true;
                    desiredSize = MeasureCore(availableSize);
                }
                finally
                {
                    _measuring = false;
                }

                if (IsInvalidSize(desiredSize))
                {
                    throw new InvalidOperationException("Invalid size returned for Measure.");
                }

                DesiredSize = desiredSize;
                _previousMeasure = availableSize;

                Logger.Verbose(LogArea.Layout, this, "Measure requested {DesiredSize}", DesiredSize);

                if (DesiredSize != previousDesiredSize)
                {
                    this.GetVisualParent<ILayoutable>()?.ChildDesiredSizeChanged(this);
                }
            }
        }

        /// <summary>
        /// Arranges the control and its children.
        /// </summary>
        /// <param name="rect">The control's new bounds.</param>
        public void Arrange(Rect rect)
        {
            if (IsInvalidRect(rect))
            {
                throw new InvalidOperationException("Invalid Arrange rectangle.");
            }

            if (!IsMeasureValid)
            {
                Measure(_previousMeasure ?? rect.Size);
            }

            if (!IsArrangeValid || _previousArrange != rect)
            {
                Logger.Verbose(LogArea.Layout, this, "Arrange to {Rect} ", rect);

                IsArrangeValid = true;
                ArrangeCore(rect);
                _previousArrange = rect;

                LayoutUpdated?.Invoke(this, EventArgs.Empty);
            }
        }


        /// <summary>
        /// Called by InvalidateMeasure
        /// </summary>
        protected virtual void OnMeasureInvalidated()
        {
        }

        /// <summary>
        /// Invalidates the measurement of the control and queues a new layout pass.
        /// </summary>
        public void InvalidateMeasure()
        {
            if (IsMeasureValid)
            {
                Logger.Verbose(LogArea.Layout, this, "Invalidated measure");

                IsMeasureValid = false;
                IsArrangeValid = false;

                if (((ILayoutable)this).IsAttachedToVisualTree)
                {
                    (VisualRoot as ILayoutRoot)?.LayoutManager.InvalidateMeasure(this);
                    InvalidateVisual();
                }
                OnMeasureInvalidated();
            }
        }

        /// <summary>
        /// Invalidates the arrangement of the control and queues a new layout pass.
        /// </summary>
        public void InvalidateArrange()
        {
            if (IsArrangeValid)
            {
                Logger.Verbose(LogArea.Layout, this, "Invalidated arrange");

                IsArrangeValid = false;
                (VisualRoot as ILayoutRoot)?.LayoutManager?.InvalidateArrange(this);
                InvalidateVisual();
            }
        }

        /// <inheritdoc/>
        void ILayoutable.ChildDesiredSizeChanged(ILayoutable control)
        {
            if (!_measuring)
            {
                InvalidateMeasure();
            }
        }

        /// <summary>
        /// Marks a property as affecting the control's measurement.
        /// </summary>
        /// <param name="properties">The properties.</param>
        /// <remarks>
        /// After a call to this method in a control's static constructor, any change to the
        /// property will cause <see cref="InvalidateMeasure"/> to be called on the element.
        /// </remarks>
        [Obsolete("Use AffectsMeasure<T> and specify the control type.")]
        protected static void AffectsMeasure(params AvaloniaProperty[] properties)
        {
            AffectsMeasure<Layoutable>(properties);
        }

        /// <summary>
        /// Marks a property as affecting the control's measurement.
        /// </summary>
        /// <typeparam name="T">The control which the property affects.</typeparam>
        /// <param name="properties">The properties.</param>
        /// <remarks>
        /// After a call to this method in a control's static constructor, any change to the
        /// property will cause <see cref="InvalidateMeasure"/> to be called on the element.
        /// </remarks>
        protected static void AffectsMeasure<T>(params AvaloniaProperty[] properties)
            where T : class, ILayoutable
        {
            void Invalidate(AvaloniaPropertyChangedEventArgs e)
            {
                (e.Sender as T)?.InvalidateMeasure();
            }

            foreach (var property in properties)
            {
                property.Changed.Subscribe(Invalidate);
            }
        }

        /// <summary>
        /// Marks a property as affecting the control's arrangement.
        /// </summary>
        /// <param name="properties">The properties.</param>
        /// <remarks>
        /// After a call to this method in a control's static constructor, any change to the
        /// property will cause <see cref="InvalidateArrange"/> to be called on the element.
        /// </remarks>
        [Obsolete("Use AffectsArrange<T> and specify the control type.")]
        protected static void AffectsArrange(params AvaloniaProperty[] properties)
        {
            AffectsArrange<Layoutable>(properties);
        }

        /// <summary>
        /// Marks a property as affecting the control's arrangement.
        /// </summary>
        /// <typeparam name="T">The control which the property affects.</typeparam>
        /// <param name="properties">The properties.</param>
        /// <remarks>
        /// After a call to this method in a control's static constructor, any change to the
        /// property will cause <see cref="InvalidateArrange"/> to be called on the element.
        /// </remarks>
        protected static void AffectsArrange<T>(params AvaloniaProperty[] properties)
            where T : class, ILayoutable
        {
            void Invalidate(AvaloniaPropertyChangedEventArgs e)
            {
                (e.Sender as T)?.InvalidateArrange();
            }

            foreach (var property in properties)
            {
                property.Changed.Subscribe(Invalidate);
            }
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
                var margin = Margin;

                ApplyTemplate();

                var constrained = LayoutHelper.ApplyLayoutConstraints(
                    this,
                    availableSize.Deflate(margin));
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

                width = Math.Min(width, availableSize.Width);
                height = Math.Min(height, availableSize.Height);

                if (UseLayoutRounding)
                {
                    var scale = GetLayoutScale();
                    width = Math.Ceiling(width * scale) / scale;
                    height = Math.Ceiling(height * scale) / scale;
                }

                return NonNegative(new Size(width, height).Inflate(margin));
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

            foreach (ILayoutable child in this.GetVisualChildren())
            {
                child.Measure(availableSize);
                width = Math.Max(width, child.DesiredSize.Width);
                height = Math.Max(height, child.DesiredSize.Height);
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
                var margin = Margin;
                var originX = finalRect.X + margin.Left;
                var originY = finalRect.Y + margin.Top;
                var availableSizeMinusMargins = new Size(
                    Math.Max(0, finalRect.Width - margin.Left - margin.Right),
                    Math.Max(0, finalRect.Height - margin.Top - margin.Bottom));
                var horizontalAlignment = HorizontalAlignment;
                var verticalAlignment = VerticalAlignment;
                var size = availableSizeMinusMargins;
                var scale = GetLayoutScale();

                if (horizontalAlignment != HorizontalAlignment.Stretch)
                {
                    size = size.WithWidth(Math.Min(size.Width, DesiredSize.Width - margin.Left - margin.Right));
                }

                if (verticalAlignment != VerticalAlignment.Stretch)
                {
                    size = size.WithHeight(Math.Min(size.Height, DesiredSize.Height - margin.Top - margin.Bottom));
                }

                size = LayoutHelper.ApplyLayoutConstraints(this, size);

                if (UseLayoutRounding)
                {
                    size = new Size(
                        Math.Ceiling(size.Width * scale) / scale, 
                        Math.Ceiling(size.Height * scale) / scale);
                    availableSizeMinusMargins = new Size(
                        Math.Ceiling(availableSizeMinusMargins.Width * scale) / scale, 
                        Math.Ceiling(availableSizeMinusMargins.Height * scale) / scale);
                }

                size = ArrangeOverride(size).Constrain(size);

                switch (horizontalAlignment)
                {
                    case HorizontalAlignment.Center:
                    case HorizontalAlignment.Stretch:
                        originX += (availableSizeMinusMargins.Width - size.Width) / 2;
                        break;
                    case HorizontalAlignment.Right:
                        originX += availableSizeMinusMargins.Width - size.Width;
                        break;
                }

                switch (verticalAlignment)
                {
                    case VerticalAlignment.Center:
                    case VerticalAlignment.Stretch:
                        originY += (availableSizeMinusMargins.Height - size.Height) / 2;
                        break;
                    case VerticalAlignment.Bottom:
                        originY += availableSizeMinusMargins.Height - size.Height;
                        break;
                }

                if (UseLayoutRounding)
                {
                    originX = Math.Floor(originX * scale) / scale;
                    originY = Math.Floor(originY * scale) / scale;
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

        /// <inheritdoc/>
        protected override sealed void OnVisualParentChanged(IVisual oldParent, IVisual newParent)
        {
            foreach (ILayoutable i in this.GetSelfAndVisualDescendants())
            {
                i.InvalidateMeasure();
            }

            base.OnVisualParentChanged(oldParent, newParent);
        }

        /// <summary>
        /// Tests whether any of a <see cref="Rect"/>'s properties include negative values,
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
        /// Tests whether any of a <see cref="Size"/>'s properties include negative values,
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
        /// Ensures neither component of a <see cref="Size"/> is negative.
        /// </summary>
        /// <param name="size">The size.</param>
        /// <returns>The non-negative size.</returns>
        private static Size NonNegative(Size size)
        {
            return new Size(Math.Max(size.Width, 0), Math.Max(size.Height, 0));
        }

        private double GetLayoutScale()
        {
            var result =  (VisualRoot as ILayoutRoot)?.LayoutScaling ?? 1.0;

            if (result == 0 || double.IsNaN(result) || double.IsInfinity(result))
            {
                throw new Exception($"Invalid LayoutScaling returned from {VisualRoot.GetType()}");
            }

            return result;
        }
    }
}
