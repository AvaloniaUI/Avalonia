// -----------------------------------------------------------------------
// <copyright file="Layoutable.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Layout
{
    using System;
    using System.Linq;
    using Splat;

    public enum HorizontalAlignment
    {
        Stretch,
        Left,
        Center,
        Right,
    }

    public enum VerticalAlignment
    {
        Stretch,
        Top,
        Center,
        Bottom,
    }

    public class Layoutable : Visual, ILayoutable, IEnableLogger
    {
        public static readonly PerspexProperty<Size> ActualSizeProperty =
            PerspexProperty.Register<Layoutable, Size>("ActualSize");

        public static readonly PerspexProperty<double> WidthProperty =
            PerspexProperty.Register<Layoutable, double>("Width", double.NaN);

        public static readonly PerspexProperty<double> HeightProperty =
            PerspexProperty.Register<Layoutable, double>("Height", double.NaN);

        public static readonly PerspexProperty<double> MinWidthProperty =
            PerspexProperty.Register<Layoutable, double>("MinWidth");

        public static readonly PerspexProperty<double> MaxWidthProperty =
            PerspexProperty.Register<Layoutable, double>("MaxWidth", double.PositiveInfinity);

        public static readonly PerspexProperty<double> MinHeightProperty =
            PerspexProperty.Register<Layoutable, double>("MinHeight");

        public static readonly PerspexProperty<double> MaxHeightProperty =
            PerspexProperty.Register<Layoutable, double>("MaxHeight", double.PositiveInfinity);

        public static readonly PerspexProperty<Thickness> MarginProperty =
            PerspexProperty.Register<Layoutable, Thickness>("Margin");

        public static readonly PerspexProperty<HorizontalAlignment> HorizontalAlignmentProperty =
            PerspexProperty.Register<Layoutable, HorizontalAlignment>("HorizontalAlignment");

        public static readonly PerspexProperty<VerticalAlignment> VerticalAlignmentProperty =
            PerspexProperty.Register<Layoutable, VerticalAlignment>("VerticalAlignment");

        public double Width
        {
            get { return this.GetValue(WidthProperty); }
            set { this.SetValue(WidthProperty, value); }
        }

        public double Height
        {
            get { return this.GetValue(HeightProperty); }
            set { this.SetValue(HeightProperty, value); }
        }

        public double MinWidth
        {
            get { return this.GetValue(MinWidthProperty); }
            set { this.SetValue(MinWidthProperty, value); }
        }

        public double MaxWidth
        {
            get { return this.GetValue(MaxWidthProperty); }
            set { this.SetValue(MaxWidthProperty, value); }
        }

        public double MinHeight
        {
            get { return this.GetValue(MinHeightProperty); }
            set { this.SetValue(MinHeightProperty, value); }
        }

        public double MaxHeight
        {
            get { return this.GetValue(MaxHeightProperty); }
            set { this.SetValue(MaxHeightProperty, value); }
        }

        public Thickness Margin
        {
            get { return this.GetValue(MarginProperty); }
            set { this.SetValue(MarginProperty, value); }
        }

        public HorizontalAlignment HorizontalAlignment
        {
            get { return this.GetValue(HorizontalAlignmentProperty); }
            set { this.SetValue(HorizontalAlignmentProperty, value); }
        }

        public VerticalAlignment VerticalAlignment
        {
            get { return this.GetValue(VerticalAlignmentProperty); }
            set { this.SetValue(VerticalAlignmentProperty, value); }
        }

        public Size ActualSize
        {
            get { return this.GetValue(ActualSizeProperty); }
        }

        public Size? DesiredSize
        {
            get;
            set;
        }

        public void Measure(Size availableSize)
        {
            if (double.IsNaN(availableSize.Width) || double.IsNaN(availableSize.Height))
            {
                throw new InvalidOperationException("Cannot call Measure using a size with NaN values.");
            }

            availableSize = availableSize.Deflate(this.Margin);
            this.DesiredSize = this.MeasureCore(availableSize).Constrain(availableSize);

            this.Log().Debug(
                "Measure of {0} (#{1:x8}) requested {2} ",
                this.GetType().Name,
                this.GetHashCode(),
                this.DesiredSize);
        }

        public void Arrange(Rect rect)
        {
            if (rect.Width < 0 || rect.Height < 0 ||
                double.IsInfinity(rect.Width) || double.IsInfinity(rect.Height) ||
                double.IsNaN(rect.Width) || double.IsNaN(rect.Height))
            {
                throw new InvalidOperationException("Invalid Arrange rectangle.");
            }

            if (!this.DesiredSize.HasValue)
            {
                throw new InvalidOperationException("Arrange called before Measure.");
            }

            this.Log().Debug(
                "Arrange of {0} (#{1:x8}) gave {2} ",
                this.GetType().Name,
                this.GetHashCode(),
                rect);

            if (this.DesiredSize.HasValue)
            {
                this.ArrangeCore(rect);
            }
        }

        public void InvalidateMeasure()
        {
            ILayoutRoot root = this.GetLayoutRoot();

            if (root != null && root.LayoutManager != null)
            {
                root.LayoutManager.InvalidateMeasure(this);
            }
        }

        public void InvalidateArrange()
        {
            ILayoutRoot root = this.GetLayoutRoot();

            if (root != null)
            {
                root.LayoutManager.InvalidateArrange(this);
            }
        }

        protected static void AffectsArrange(PerspexProperty property)
        {
            property.Changed.Subscribe(AffectsArrangeInvalidate);
        }

        protected static void AffectsMeasure(PerspexProperty property)
        {
            property.Changed.Subscribe(AffectsMeasureInvalidate);
        }

        protected virtual void ArrangeCore(Rect finalRect)
        {
            if (this.IsVisible)
            {
                double originX = finalRect.X + this.Margin.Left;
                double originY = finalRect.Y + this.Margin.Top;
                var size = new Size(
                    Math.Max(0, finalRect.Width - this.Margin.Left - this.Margin.Right),
                    Math.Max(0, finalRect.Height - this.Margin.Top - this.Margin.Bottom));

                if (this.HorizontalAlignment != HorizontalAlignment.Stretch)
                {
                    size = size.WithWidth(Math.Min(size.Width, this.DesiredSize.Value.Width));
                }

                if (this.VerticalAlignment != VerticalAlignment.Stretch)
                {
                    size = size.WithHeight(Math.Min(size.Height, this.DesiredSize.Value.Height));
                }

                size = LayoutHelper.ApplyLayoutConstraints(this, size);
                size = this.ArrangeOverride(size).Constrain(size);

                switch (this.HorizontalAlignment)
                {
                    case HorizontalAlignment.Center:
                        originX += (finalRect.Width - size.Width) / 2;
                        break;
                    case HorizontalAlignment.Right:
                        originX += finalRect.Width - size.Width;
                        break;
                }

                switch (this.VerticalAlignment)
                {
                    case VerticalAlignment.Center:
                        originY += (finalRect.Height - size.Height) / 2;
                        break;
                    case VerticalAlignment.Bottom:
                        originY += finalRect.Height - size.Height;
                        break;
                }

                var bounds = new Rect(originX, originY, size.Width, size.Height);
                this.SetVisualBounds(bounds);
                this.SetValue(ActualSizeProperty, bounds.Size);
            }
        }

        protected virtual Size ArrangeOverride(Size finalSize)
        {
            foreach (ILayoutable child in this.GetVisualChildren().OfType<ILayoutable>())
            {
                child.Arrange(new Rect(finalSize));
            }

            return finalSize;
        }

        protected virtual Size MeasureCore(Size availableSize)
        {
            if (this.IsVisible)
            {
                var constrained = LayoutHelper.ApplyLayoutConstraints(this, availableSize)
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

        protected virtual Size MeasureOverride(Size availableSize)
        {
            double width = 0;
            double height = 0;

            foreach (ILayoutable child in this.GetVisualChildren().OfType<ILayoutable>())
            {
                child.Measure(availableSize);
                width = Math.Max(width, child.DesiredSize.Value.Width);
                height= Math.Max(height, child.DesiredSize.Value.Height);
            }

            return new Size(width, height);
        }

        private static void AffectsArrangeInvalidate(PerspexPropertyChangedEventArgs e)
        {
            ILayoutable control = e.Sender as ILayoutable;

            if (control != null)
            {
                control.InvalidateArrange();
            }
        }

        private static void AffectsMeasureInvalidate(PerspexPropertyChangedEventArgs e)
        {
            ILayoutable control = e.Sender as ILayoutable;

            if (control != null)
            {
                control.InvalidateMeasure();
            }
        }

        private ILayoutRoot GetLayoutRoot()
        {
            return this.GetSelfAndVisualAncestors().OfType<ILayoutRoot>().FirstOrDefault();
        }
    }
}
