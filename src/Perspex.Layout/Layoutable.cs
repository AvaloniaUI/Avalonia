﻿// -----------------------------------------------------------------------
// <copyright file="Layoutable.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Layout
{
    using System;
    using System.Linq;
    using Perspex.VisualTree;
    using Serilog;
    using Serilog.Core.Enrichers;

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

        private Size? previousMeasure;

        private Rect? previousArrange;

        private ILogger layoutLog;

        /// <summary>
        /// Initializes static members of the <see cref="Layoutable"/> class.
        /// </summary>
        static Layoutable()
        {
            Layoutable.AffectsMeasure(Visual.IsVisibleProperty);
            Layoutable.AffectsMeasure(Layoutable.WidthProperty);
            Layoutable.AffectsMeasure(Layoutable.HeightProperty);
            Layoutable.AffectsMeasure(Layoutable.MinWidthProperty);
            Layoutable.AffectsMeasure(Layoutable.MaxWidthProperty);
            Layoutable.AffectsMeasure(Layoutable.MinHeightProperty);
            Layoutable.AffectsMeasure(Layoutable.MaxHeightProperty);
            Layoutable.AffectsMeasure(Layoutable.MarginProperty);
            Layoutable.AffectsMeasure(Layoutable.HorizontalAlignmentProperty);
            Layoutable.AffectsMeasure(Layoutable.VerticalAlignmentProperty);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Layoutable"/> class.
        /// </summary>
        public Layoutable()
        {
            this.layoutLog = Log.ForContext(new[]
            {
                new PropertyEnricher("Area", "Layout"),
                new PropertyEnricher("SourceContext", this.GetType()),
                new PropertyEnricher("Id", this.GetHashCode()),
            });
        }

        /// <summary>
        /// Gets or sets the width of the element.
        /// </summary>
        public double Width
        {
            get { return this.GetValue(WidthProperty); }
            set { this.SetValue(WidthProperty, value); }
        }

        /// <summary>
        /// Gets or sets the height of the element.
        /// </summary>
        public double Height
        {
            get { return this.GetValue(HeightProperty); }
            set { this.SetValue(HeightProperty, value); }
        }

        /// <summary>
        /// Gets or sets the minimum width of the element.
        /// </summary>
        public double MinWidth
        {
            get { return this.GetValue(MinWidthProperty); }
            set { this.SetValue(MinWidthProperty, value); }
        }

        /// <summary>
        /// Gets or sets the maximum width of the element.
        /// </summary>
        public double MaxWidth
        {
            get { return this.GetValue(MaxWidthProperty); }
            set { this.SetValue(MaxWidthProperty, value); }
        }

        /// <summary>
        /// Gets or sets the minimum height of the element.
        /// </summary>
        public double MinHeight
        {
            get { return this.GetValue(MinHeightProperty); }
            set { this.SetValue(MinHeightProperty, value); }
        }

        /// <summary>
        /// Gets or sets the maximum height of the element.
        /// </summary>
        public double MaxHeight
        {
            get { return this.GetValue(MaxHeightProperty); }
            set { this.SetValue(MaxHeightProperty, value); }
        }

        /// <summary>
        /// Gets or sets the margin around the element.
        /// </summary>
        public Thickness Margin
        {
            get { return this.GetValue(MarginProperty); }
            set { this.SetValue(MarginProperty, value); }
        }

        /// <summary>
        /// Gets or sets the element's preferred horizontal alignment in its parent.
        /// </summary>
        public HorizontalAlignment HorizontalAlignment
        {
            get { return this.GetValue(HorizontalAlignmentProperty); }
            set { this.SetValue(HorizontalAlignmentProperty, value); }
        }

        /// <summary>
        /// Gets or sets the element's preferred vertical alignment in its parent.
        /// </summary>
        public VerticalAlignment VerticalAlignment
        {
            get { return this.GetValue(VerticalAlignmentProperty); }
            set { this.SetValue(VerticalAlignmentProperty, value); }
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
            get { return this.GetValue(UseLayoutRoundingProperty); }
            set { this.SetValue(UseLayoutRoundingProperty, value); }
        }

        /// <summary>
        /// Gets the available size passed in the previous layout pass, if any.
        /// </summary>
        Size? ILayoutable.PreviousMeasure
        {
            get { return this.previousMeasure; }
        }

        /// <summary>
        /// Gets the layout rect passed in the previous layout pass, if any.
        /// </summary>
        Rect? ILayoutable.PreviousArrange
        {
            get { return this.previousArrange; }
        }

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

            if (force || !this.IsMeasureValid || this.previousMeasure != availableSize)
            {
                this.IsMeasureValid = true;

                var desiredSize = this.MeasureCore(availableSize).Constrain(availableSize);

                if (IsInvalidSize(desiredSize))
                {
                    throw new InvalidOperationException("Invalid size returned for Measure.");
                }

                this.DesiredSize = desiredSize;
                this.previousMeasure = availableSize;

                this.layoutLog.Verbose("Measure requested {DesiredSize}", this.DesiredSize);
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
            if (!this.IsMeasureValid)
            {
                return;
            }

            if (force || !this.IsArrangeValid || this.previousArrange != rect)
            {
                this.layoutLog.Verbose("Arrange to {Rect} ", rect);

                this.IsArrangeValid = true;
                this.ArrangeCore(rect);
                this.previousArrange = rect;
            }
        }

        /// <summary>
        /// Invalidates the measurement of the control and queues a new layout pass.
        /// </summary>
        public void InvalidateMeasure()
        {
            var parent = this.GetVisualParent<ILayoutable>();

            if (this.IsMeasureValid)
            {
                this.layoutLog.Verbose("Invalidated measure");
            }

            this.IsMeasureValid = false;
            this.IsArrangeValid = false;
            this.previousMeasure = null;
            this.previousArrange = null;

            if (parent != null && IsResizable(parent))
            {
                parent.InvalidateMeasure();
            }
            else
            {
                var root = this.GetLayoutRoot();

                if (root != null && root.Item1.LayoutManager != null)
                {
                    root.Item1.LayoutManager.InvalidateMeasure(this, root.Item2);
                }
            }
        }

        /// <summary>
        /// Invalidates the arrangement of the control and queues a new layout pass.
        /// </summary>
        public void InvalidateArrange()
        {
            var root = this.GetLayoutRoot();

            if (this.IsArrangeValid)
            {
                this.layoutLog.Verbose("Arrange measure");
            }

            this.IsArrangeValid = false;
            this.previousArrange = null;

            if (root != null && root.Item1.LayoutManager != null)
            {
                root.Item1.LayoutManager.InvalidateArrange(this, root.Item2);
            }
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
            if (this.IsVisible)
            {
                this.ApplyTemplate();

                var constrained = LayoutHelper
                    .ApplyLayoutConstraints(this, availableSize)
                    .Deflate(this.Margin);

                var measured = this.MeasureOverride(constrained);
                var width = measured.Width;
                var height = measured.Height;

                if (!double.IsNaN(this.Width))
                {
                    width = this.Width;
                }

                width = Math.Min(width, this.MaxWidth);
                width = Math.Max(width, this.MinWidth);

                if (!double.IsNaN(this.Height))
                {
                    height = this.Height;
                }

                height = Math.Min(height, this.MaxHeight);
                height = Math.Max(height, this.MinHeight);

                return new Size(width, height).Inflate(this.Margin);
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
            if (this.IsVisible)
            {
                double originX = finalRect.X + this.Margin.Left;
                double originY = finalRect.Y + this.Margin.Top;
                var sizeMinusMargins = new Size(
                    Math.Max(0, finalRect.Width - this.Margin.Left - this.Margin.Right),
                    Math.Max(0, finalRect.Height - this.Margin.Top - this.Margin.Bottom));
                var size = sizeMinusMargins;

                if (this.HorizontalAlignment != HorizontalAlignment.Stretch)
                {
                    size = size.WithWidth(Math.Min(size.Width, this.DesiredSize.Width));
                }

                if (this.VerticalAlignment != VerticalAlignment.Stretch)
                {
                    size = size.WithHeight(Math.Min(size.Height, this.DesiredSize.Height));
                }

                size = LayoutHelper.ApplyLayoutConstraints(this, size);

                if (this.UseLayoutRounding)
                {
                    size = new Size(Math.Ceiling(size.Width), Math.Ceiling(size.Height));
                }

                size = this.ArrangeOverride(size).Constrain(size);

                switch (this.HorizontalAlignment)
                {
                    case HorizontalAlignment.Center:
                        originX += (sizeMinusMargins.Width - size.Width) / 2;
                        break;
                    case HorizontalAlignment.Right:
                        originX += sizeMinusMargins.Width - size.Width;
                        break;
                }

                switch (this.VerticalAlignment)
                {
                    case VerticalAlignment.Center:
                        originY += (sizeMinusMargins.Height - size.Height) / 2;
                        break;
                    case VerticalAlignment.Bottom:
                        originY += sizeMinusMargins.Height - size.Height;
                        break;
                }

                if (this.UseLayoutRounding)
                {
                    originX = Math.Floor(originX);
                    originY = Math.Floor(originY);
                }

                this.Bounds = new Rect(originX, originY, size.Width, size.Height);
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

            if (control != null)
            {
                control.InvalidateMeasure();
            }
        }

        /// <summary>
        /// Calls <see cref="InvalidateArrange"/> on the control on which a property changed.
        /// </summary>
        /// <param name="e">The event args.</param>
        private static void AffectsArrangeInvalidate(PerspexPropertyChangedEventArgs e)
        {
            ILayoutable control = e.Sender as ILayoutable;

            if (control != null)
            {
                control.InvalidateArrange();
            }
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
