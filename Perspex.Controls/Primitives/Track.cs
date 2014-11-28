// -----------------------------------------------------------------------
// <copyright file="Thumb.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls.Primitives
{
    using System;
    using Perspex.Input;
    using Perspex.Interactivity;

    public class Track : Control
    {
        public static readonly PerspexProperty<double> MinimumProperty =
            ScrollBar.MinimumProperty.AddOwner<Track>();

        public static readonly PerspexProperty<double> MaximumProperty =
            ScrollBar.MaximumProperty.AddOwner<Track>();

        public static readonly PerspexProperty<double> ValueProperty =
            ScrollBar.ValueProperty.AddOwner<Track>();

        public static readonly PerspexProperty<double> ViewportSizeProperty =
            ScrollBar.ViewportSizeProperty.AddOwner<Track>();

        public static readonly PerspexProperty<Orientation> OrientationProperty =
            ScrollBar.OrientationProperty.AddOwner<Track>();

        public static readonly PerspexProperty<Thumb> ThumbProperty =
            PerspexProperty.Register<Track, Thumb>("Thumb");

        public Track()
        {
            this.GetObservableWithHistory(ThumbProperty).Subscribe(val =>
            {
                if (val.Item1 != null)
                {
                    val.Item1.DragDelta -= ThumbDragged;
                }

                this.ClearVisualChildren();

                if (val.Item2 != null)
                {
                    val.Item2.DragDelta += ThumbDragged;
                    this.AddVisualChild(val.Item2);
                }
            });

            AffectsArrange(MinimumProperty);
            AffectsArrange(MaximumProperty);
            AffectsArrange(ValueProperty);
            AffectsMeasure(OrientationProperty);
        }

        public double Minimum
        {
            get { return this.GetValue(MinimumProperty); }
            set { this.SetValue(MinimumProperty, value); }
        }

        public double Maximum
        {
            get { return this.GetValue(MaximumProperty); }
            set { this.SetValue(MaximumProperty, value); }
        }

        public double Value
        {
            get { return this.GetValue(ValueProperty); }
            set { this.SetValue(ValueProperty, value); }
        }

        public double ViewportSize
        {
            get { return this.GetValue(ViewportSizeProperty); }
            set { this.SetValue(ViewportSizeProperty, value); }
        }

        public Orientation Orientation
        {
            get { return this.GetValue(OrientationProperty); }
            set { this.SetValue(OrientationProperty, value); }
        }

        public Thumb Thumb
        {
            get { return this.GetValue(ThumbProperty); }
            set { this.SetValue(ThumbProperty, value); }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var thumb = this.Thumb;

            if (thumb != null)
            {
                thumb.Measure(availableSize);

                if (this.Orientation == Orientation.Horizontal)
                {
                    return new Size(0, thumb.DesiredSize.Value.Height);
                }
                else
                {
                    return new Size(thumb.DesiredSize.Value.Width, 0);
                }
            }

            return base.MeasureOverride(availableSize);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var thumb = this.Thumb;

            if (thumb != null)
            {
                var range = this.Maximum - this.Minimum;
                var thumbsize = this.ViewportSize / range;

                if (this.Orientation == Orientation.Horizontal)
                {
                    var width = finalSize.Width * thumbsize;
                    var x = finalSize.Width * (this.Value / range);
                    thumb.Arrange(new Rect(x, 0, width, finalSize.Height));
                }
                else
                {
                    var height = finalSize.Height * thumbsize;
                    var y = finalSize.Height * (this.Value / range);
                    thumb.Arrange(new Rect(0, y, finalSize.Width, height));
                }
            }

            return finalSize;
        }

        private void ThumbDragged(object sender, VectorEventArgs e)
        {
            var range = this.Maximum - this.Minimum;
            var value = this.Value;

            if (this.Orientation == Orientation.Horizontal)
            {
                value += e.Vector.X * (range / this.ActualSize.Width);

            }
            else
            {
                value += e.Vector.Y * (range / this.ActualSize.Height);
            }

            value = Math.Max(value, this.Minimum);
            value = Math.Min(value, this.Maximum - this.ViewportSize);

            this.Value = value;
        }
    }
}
