// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Metadata;

namespace Avalonia.Controls.Primitives
{
    public class Track : Control
    {
        public static readonly DirectProperty<Track, double> MinimumProperty =
            RangeBase.MinimumProperty.AddOwner<Track>(o => o.Minimum, (o, v) => o.Minimum = v);

        public static readonly DirectProperty<Track, double> MaximumProperty =
            RangeBase.MaximumProperty.AddOwner<Track>(o => o.Maximum, (o, v) => o.Maximum = v);

        public static readonly DirectProperty<Track, double> ValueProperty =
            RangeBase.ValueProperty.AddOwner<Track>(o => o.Value, (o, v) => o.Value = v);

        public static readonly StyledProperty<double> ViewportSizeProperty =
            ScrollBar.ViewportSizeProperty.AddOwner<Track>();

        public static readonly StyledProperty<Orientation> OrientationProperty =
            ScrollBar.OrientationProperty.AddOwner<Track>();

        public static readonly StyledProperty<Thumb> ThumbProperty =
            AvaloniaProperty.Register<Track, Thumb>(nameof(Thumb));

        public static readonly StyledProperty<Button> IncreaseButtonProperty =
            AvaloniaProperty.Register<Track, Button>(nameof(IncreaseButton));

        public static readonly StyledProperty<Button> DecreaseButtonProperty =
            AvaloniaProperty.Register<Track, Button>(nameof(DecreaseButton));

        private double _minimum;
        private double _maximum = 100.0;
        private double _value;

        static Track()
        {
            PseudoClass<Track, Orientation>(OrientationProperty, o => o == Orientation.Vertical, ":vertical");
            PseudoClass<Track, Orientation>(OrientationProperty, o => o == Orientation.Horizontal, ":horizontal");
            ThumbProperty.Changed.AddClassHandler<Track>(x => x.ThumbChanged);
            IncreaseButtonProperty.Changed.AddClassHandler<Track>(x => x.ButtonChanged);
            DecreaseButtonProperty.Changed.AddClassHandler<Track>(x => x.ButtonChanged);
            AffectsArrange<Track>(MinimumProperty, MaximumProperty, ValueProperty, OrientationProperty);
        }

        public double Minimum
        {
            get { return _minimum; }
            set { SetAndRaise(MinimumProperty, ref _minimum, value); }
        }

        public double Maximum
        {
            get { return _maximum; }
            set { SetAndRaise(MaximumProperty, ref _maximum, value); }
        }

        public double Value
        {
            get { return _value; }
            set { SetAndRaise(ValueProperty, ref _value, value); }
        }

        public double ViewportSize
        {
            get { return GetValue(ViewportSizeProperty); }
            set { SetValue(ViewportSizeProperty, value); }
        }

        public Orientation Orientation
        {
            get { return GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }

        [Content]
        public Thumb Thumb
        {
            get { return GetValue(ThumbProperty); }
            set { SetValue(ThumbProperty, value); }
        }

        public Button IncreaseButton
        {
            get { return GetValue(IncreaseButtonProperty); }
            set { SetValue(IncreaseButtonProperty, value); }
        }

        public Button DecreaseButton
        {
            get { return GetValue(DecreaseButtonProperty); }
            set { SetValue(DecreaseButtonProperty, value); }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var thumb = Thumb;

            if (thumb != null)
            {
                thumb.Measure(availableSize);

                if (Orientation == Orientation.Horizontal)
                {
                    return new Size(0, thumb.DesiredSize.Height);
                }
                else
                {
                    return new Size(thumb.DesiredSize.Width, 0);
                }
            }

            return base.MeasureOverride(availableSize);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var thumb = Thumb;
            var increaseButton = IncreaseButton;
            var decreaseButton = DecreaseButton;

            var range = Maximum - Minimum;
            var offset = Math.Min(Value - Minimum, range);
            var viewportSize = ViewportSize;
            var extent = range + viewportSize;

            if (Orientation == Orientation.Horizontal)
            {
                double thumbWidth = 0;

                if (double.IsNaN(viewportSize))
                {
                    thumbWidth = thumb?.DesiredSize.Width ?? 0;
                }
                else if (extent > 0)
                {
                    thumbWidth = finalSize.Width * viewportSize / extent;
                }

                var remaining = finalSize.Width - thumbWidth;
                var firstWidth = range <= 0 ? 0 : remaining * offset / range;

                if (decreaseButton != null)
                {
                    decreaseButton.Arrange(new Rect(0, 0, firstWidth, finalSize.Height));
                }

                if (thumb != null)
                {
                    thumb.Arrange(new Rect(firstWidth, 0, thumbWidth, finalSize.Height));
                }

                if (increaseButton != null)
                {
                    increaseButton.Arrange(new Rect(
                        firstWidth + thumbWidth,
                        0,
                        Math.Max(0, remaining - firstWidth),
                        finalSize.Height));
                }
            }
            else
            {
                double thumbHeight = 0;

                if (double.IsNaN(viewportSize))
                {
                    thumbHeight = thumb?.DesiredSize.Height ?? 0;
                }
                else if (extent > 0)
                {
                    thumbHeight = finalSize.Height * viewportSize / extent;
                }

                var remaining = finalSize.Height - thumbHeight;
                var firstHeight = range <= 0 ? 0 : remaining * offset / range;

                if (decreaseButton != null)
                {
                    decreaseButton.Arrange(new Rect(0, 0, finalSize.Width, firstHeight));
                }

                if (thumb != null)
                {
                    thumb.Arrange(new Rect(0, firstHeight, finalSize.Width, thumbHeight));
                }

                if (increaseButton != null)
                {
                    increaseButton.Arrange(new Rect(
                        0,
                        firstHeight + thumbHeight,
                        finalSize.Width,
                        Math.Max(remaining - firstHeight, 0)));
                }
            }

            return finalSize;
        }

        private void ThumbChanged(AvaloniaPropertyChangedEventArgs e)
        {
            var oldThumb = (Thumb)e.OldValue;
            var newThumb = (Thumb)e.NewValue;

            if (oldThumb != null)
            {
                oldThumb.DragDelta -= ThumbDragged;

                LogicalChildren.Remove(oldThumb);
                VisualChildren.Remove(oldThumb);
            }

            if (newThumb != null)
            {
                newThumb.DragDelta += ThumbDragged;
                LogicalChildren.Add(newThumb);
                VisualChildren.Add(newThumb);
            }
        }

        private void ButtonChanged(AvaloniaPropertyChangedEventArgs e)
        {
            var oldButton = (Button)e.OldValue;
            var newButton = (Button)e.NewValue;

            if (oldButton != null)
            {
                LogicalChildren.Remove(oldButton);
                VisualChildren.Remove(oldButton);
            }

            if (newButton != null)
            {
                LogicalChildren.Add(newButton);
                VisualChildren.Add(newButton);
            }
        }

        private void ThumbDragged(object sender, VectorEventArgs e)
        {
            double range = Maximum - Minimum;
            double value = Value;
            double offset;

            if (Orientation == Orientation.Horizontal)
            {
                offset = e.Vector.X / ((Bounds.Size.Width - Thumb.Bounds.Size.Width) / range);
            }
            else
            {
                offset = e.Vector.Y * (range / (Bounds.Size.Height - Thumb.Bounds.Size.Height));
            }

            if (!double.IsNaN(offset) && !double.IsInfinity(offset))
            {
                value += offset;
                value = Math.Max(value, Minimum);
                value = Math.Min(value, Maximum);
                Value = value;
            }
        }
    }
}
