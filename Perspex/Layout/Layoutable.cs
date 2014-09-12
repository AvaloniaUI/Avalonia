// -----------------------------------------------------------------------
// <copyright file="Layoutable.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Layout
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

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

    public class Layoutable : Visual, ILayoutable
    {
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
            get { return ((IVisual)this).Bounds.Size; }
        }

        public Size? DesiredSize
        {
            get;
            set;
        }

        public void Measure(Size availableSize)
        {
            availableSize = availableSize.Deflate(this.Margin);
            this.DesiredSize = this.MeasureCore(availableSize).Constrain(availableSize);
        }

        public void Arrange(Rect rect)
        {
            this.ArrangeCore(rect);
        }

        public void InvalidateMeasure()
        {
            ILayoutRoot root = this.GetVisualAncestorOrSelf<ILayoutRoot>();

            if (root != null && root.LayoutManager != null)
            {
                root.LayoutManager.InvalidateMeasure(this);
            }
        }

        public void InvalidateArrange()
        {
            ILayoutRoot root = this.GetVisualAncestorOrSelf<ILayoutRoot>();

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
            double originX = finalRect.X + this.Margin.Left;
            double originY = finalRect.Y + this.Margin.Top;
            double sizeX = Math.Max(0, finalRect.Width - this.Margin.Left - this.Margin.Right);
            double sizeY = Math.Max(0, finalRect.Height - this.Margin.Top - this.Margin.Bottom);

            if (this.HorizontalAlignment != HorizontalAlignment.Stretch)
            {
                sizeX = Math.Min(sizeX, this.DesiredSize.Value.Width);
            }

            if (this.VerticalAlignment != VerticalAlignment.Stretch)
            {
                sizeY = Math.Min(sizeY, this.DesiredSize.Value.Height);
            }

            Size taken = this.ArrangeOverride(new Size(sizeX, sizeY));

            sizeX = Math.Min(taken.Width, sizeX);
            sizeY = Math.Min(taken.Height, sizeY);

            switch (this.HorizontalAlignment)
            {
                case HorizontalAlignment.Center:
                    originX += (finalRect.Width - sizeX) / 2;
                    break;
                case HorizontalAlignment.Right:
                    originX += finalRect.Width - sizeX;
                    break;
            }

            switch (this.VerticalAlignment)
            {
                case VerticalAlignment.Center:
                    originY += (finalRect.Height - sizeY) / 2;
                    break;
                case VerticalAlignment.Bottom:
                    originY += finalRect.Height - sizeY;
                    break;
            }

            ((IVisual)this).Bounds = new Rect(originX, originY, sizeX, sizeY);
        }

        protected virtual Size ArrangeOverride(Size finalSize)
        {
            return finalSize;
        }

        protected virtual Size MeasureCore(Size availableSize)
        {
            if (this.IsVisible)
            {
                Size measuredSize = this.MeasureOverride(availableSize.Deflate(this.Margin)).Inflate(this.Margin);
                double width = (this.Width > 0) ? this.Width : measuredSize.Width;
                double height = (this.Height > 0) ? this.Height : measuredSize.Height;

                width = Math.Min(width, this.MaxWidth);
                width = Math.Max(width, this.MinWidth);
                height = Math.Min(height, this.MaxHeight);
                height = Math.Max(height, this.MinHeight);

                return new Size(width, height);
            }
            else
            {
                return new Size();
            }
        }

        protected virtual Size MeasureOverride(Size availableSize)
        {
            return new Size();
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

    }
}
