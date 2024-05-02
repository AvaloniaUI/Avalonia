// Portions of this source file are adapted from the Windows Presentation Foundation project.
// (https://github.com/dotnet/wpf/) 
// 
// Licensed to The Avalonia Project under MIT License, courtesy of The .NET Foundation.

using System;
using Avalonia.Controls.Metadata;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Metadata;
using Avalonia.Utilities;

namespace Avalonia.Controls.Primitives
{
    [PseudoClasses(":vertical", ":horizontal")]
    public class Track : Control
    {
        public static readonly StyledProperty<double> MinimumProperty =
            RangeBase.MinimumProperty.AddOwner<Track>();

        public static readonly StyledProperty<double> MaximumProperty =
            RangeBase.MaximumProperty.AddOwner<Track>();

        public static readonly StyledProperty<double> ValueProperty =
            RangeBase.ValueProperty.AddOwner<Track>();

        public static readonly StyledProperty<double> ViewportSizeProperty =
            ScrollBar.ViewportSizeProperty.AddOwner<Track>();

        public static readonly StyledProperty<Orientation> OrientationProperty =
            ScrollBar.OrientationProperty.AddOwner<Track>();

        public static readonly StyledProperty<Thumb?> ThumbProperty =
            AvaloniaProperty.Register<Track, Thumb?>(nameof(Thumb));

        public static readonly StyledProperty<Button?> IncreaseButtonProperty =
            AvaloniaProperty.Register<Track, Button?>(nameof(IncreaseButton));

        public static readonly StyledProperty<Button?> DecreaseButtonProperty =
            AvaloniaProperty.Register<Track, Button?>(nameof(DecreaseButton));

        public static readonly StyledProperty<bool> IsDirectionReversedProperty =
            AvaloniaProperty.Register<Track, bool>(nameof(IsDirectionReversed));

        public static readonly StyledProperty<bool> IgnoreThumbDragProperty =
            AvaloniaProperty.Register<Track, bool>(nameof(IgnoreThumbDrag));

        public static readonly StyledProperty<bool> DeferThumbDragProperty =
            AvaloniaProperty.Register<Track, bool>(nameof(DeferThumbDrag));

        private VectorEventArgs? _deferredThumbDrag;
        private Vector _lastDrag;

        static Track()
        {
            ThumbProperty.Changed.AddClassHandler<Track>((x, e) => x.ThumbChanged(e));
            IncreaseButtonProperty.Changed.AddClassHandler<Track>((x, e) => x.ButtonChanged(e));
            DecreaseButtonProperty.Changed.AddClassHandler<Track>((x, e) => x.ButtonChanged(e));
            AffectsArrange<Track>(IsDirectionReversedProperty, MinimumProperty, MaximumProperty, ValueProperty, OrientationProperty);
        }

        public Track()
        {
            UpdatePseudoClasses(Orientation);
        }

        public double Minimum
        {
            get => GetValue(MinimumProperty);
            set => SetValue(MinimumProperty, value);
        }

        public double Maximum
        {
            get => GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }

        public double Value
        {
            get => GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        /// <summary>
        /// Gets the value of the <see cref="Thumb"/>'s current position. This can differ from <see cref="Value"/> when <see cref="ScrollViewer.IsDeferredScrollingEnabled"/> is true.
        /// </summary>
        private double ThumbValue => Value + (_deferredThumbDrag == null ? 0 : ValueFromDistance(_deferredThumbDrag.Vector.X, _deferredThumbDrag.Vector.Y));

        public double ViewportSize
        {
            get => GetValue(ViewportSizeProperty);
            set => SetValue(ViewportSizeProperty, value);
        }

        public Orientation Orientation
        {
            get => GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }

        [Content]
        public Thumb? Thumb
        {
            get => GetValue(ThumbProperty);
            set => SetValue(ThumbProperty, value);
        }

        public Button? IncreaseButton
        {
            get => GetValue(IncreaseButtonProperty);
            set => SetValue(IncreaseButtonProperty, value);
        }

        public Button? DecreaseButton
        {
            get => GetValue(DecreaseButtonProperty);
            set => SetValue(DecreaseButtonProperty, value);
        }

        public bool IsDirectionReversed
        {
            get => GetValue(IsDirectionReversedProperty);
            set => SetValue(IsDirectionReversedProperty, value);
        }

        public bool IgnoreThumbDrag
        {
            get => GetValue(IgnoreThumbDragProperty);
            set => SetValue(IgnoreThumbDragProperty, value);
        }

        public bool DeferThumbDrag
        {
            get => GetValue(DeferThumbDragProperty);
            set => SetValue(DeferThumbDragProperty, value);
        }

        private double ThumbCenterOffset { get; set; }
        private double Density { get; set; }

        /// <summary>
        /// Calculates the distance along the <see cref="Thumb"/> of a specified point along the
        /// track.
        /// </summary>
        /// <param name="point">The specified point.</param>
        /// <returns>
        /// The distance between the Thumb and the specified pt value.
        /// </returns>
        public virtual double ValueFromPoint(Point point)
        {
            double val;

            // Find distance from center of thumb to given point.
            if (Orientation == Orientation.Horizontal)
            {
                val = ThumbValue + ValueFromDistance(point.X - ThumbCenterOffset, point.Y - (Bounds.Height * 0.5));
            }
            else
            {
                val = ThumbValue + ValueFromDistance(point.X - (Bounds.Width * 0.5), point.Y - ThumbCenterOffset);
            }

            return Math.Max(Minimum, Math.Min(Maximum, val));
        }

        /// <summary>
        /// Calculates the change in the <see cref="Value"/> of the <see cref="Track"/> when the
        /// <see cref="Thumb"/> moves.
        /// </summary>
        /// <param name="horizontal">The horizontal displacement of the thumb.</param>
        /// <param name="vertical">The vertical displacement of the thumb.</param>        
        public virtual double ValueFromDistance(double horizontal, double vertical)
        {
            double scale = IsDirectionReversed ? -1 : 1;

            if (Orientation == Orientation.Horizontal)
            {
                return scale * horizontal * Density;
            }
            else
            {
                // Increases in y cause decreases in Sliders value
                return -1 * scale * vertical * Density;
            }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            Size desiredSize = new Size(0.0, 0.0);

            // Only measure thumb.
            // Repeat buttons will be sized based on thumb
            if (Thumb != null)
            {
                Thumb.Measure(availableSize);
                desiredSize = Thumb.DesiredSize;
            }

            if (!double.IsNaN(ViewportSize))
            {
                // ScrollBar can shrink to 0 in the direction of scrolling
                if (Orientation == Orientation.Vertical)
                    desiredSize = desiredSize.WithHeight(0.0);
                else
                    desiredSize = desiredSize.WithWidth(0.0);
            }

            return desiredSize;
        }

        protected override Size ArrangeOverride(Size arrangeSize)
        {
            double decreaseButtonLength, thumbLength, increaseButtonLength;
            var isVertical = Orientation == Orientation.Vertical;
            var viewportSize = Math.Max(0.0, ViewportSize);

            // If viewport is NaN, compute thumb's size based on its desired size,
            // otherwise compute the thumb base on the viewport and extent properties
            if (double.IsNaN(ViewportSize))
            {
                ComputeSliderLengths(arrangeSize, isVertical, out decreaseButtonLength, out thumbLength, out increaseButtonLength);
            }
            else
            {
                // Don't arrange if there's not enough content or the track is too small
                if (!ComputeScrollBarLengths(arrangeSize, viewportSize, isVertical, out decreaseButtonLength, out thumbLength, out increaseButtonLength))
                {
                    return arrangeSize;
                }
            }

            // Layout the pieces of track
            var offset = new Point();
            var pieceSize = arrangeSize;
            var isDirectionReversed = IsDirectionReversed;

            if (isVertical)
            {
                CoerceLength(ref decreaseButtonLength, arrangeSize.Height);
                CoerceLength(ref increaseButtonLength, arrangeSize.Height);
                CoerceLength(ref thumbLength, arrangeSize.Height);

                offset = offset.WithY(isDirectionReversed ? decreaseButtonLength + thumbLength : 0.0);
                pieceSize = pieceSize.WithHeight(increaseButtonLength);

                IncreaseButton?.Arrange(new Rect(offset, pieceSize));

                offset = offset.WithY(isDirectionReversed ? 0.0 : increaseButtonLength + thumbLength);
                pieceSize = pieceSize.WithHeight(decreaseButtonLength);

                DecreaseButton?.Arrange(new Rect(offset, pieceSize));

                offset = offset.WithY(isDirectionReversed ? decreaseButtonLength : increaseButtonLength);
                pieceSize = pieceSize.WithHeight(thumbLength);

                if (Thumb != null)
                {
                    var bounds = new Rect(offset, pieceSize);
                    var adjust = CalculateThumbAdjustment(Thumb, bounds);
                    Thumb.Arrange(bounds);
                    Thumb.AdjustDrag(adjust);
                }

                ThumbCenterOffset = offset.Y + (thumbLength * 0.5);
            }
            else
            {
                CoerceLength(ref decreaseButtonLength, arrangeSize.Width);
                CoerceLength(ref increaseButtonLength, arrangeSize.Width);
                CoerceLength(ref thumbLength, arrangeSize.Width);

                offset = offset.WithX(isDirectionReversed ? increaseButtonLength + thumbLength : 0.0);
                pieceSize = pieceSize.WithWidth(decreaseButtonLength);

                DecreaseButton?.Arrange(new Rect(offset, pieceSize));

                offset = offset.WithX(isDirectionReversed ? 0.0 : decreaseButtonLength + thumbLength);
                pieceSize = pieceSize.WithWidth(increaseButtonLength);

                IncreaseButton?.Arrange(new Rect(offset, pieceSize));

                offset = offset.WithX(isDirectionReversed ? increaseButtonLength : decreaseButtonLength);
                pieceSize = pieceSize.WithWidth(thumbLength);

                if (Thumb != null)
                {
                    var bounds = new Rect(offset, pieceSize);
                    var adjust = CalculateThumbAdjustment(Thumb, bounds);
                    Thumb.Arrange(bounds);
                    Thumb.AdjustDrag(adjust);
                }

                ThumbCenterOffset = offset.X + (thumbLength * 0.5);
            }

            _lastDrag = default;
            return arrangeSize;
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == OrientationProperty)
            {
                UpdatePseudoClasses(change.GetNewValue<Orientation>());
            }
            else if (change.Property == DeferThumbDragProperty)
            {
                if (!change.GetNewValue<bool>())
                {
                    ApplyDeferredThumbDrag();
                }
            }
        }

        private Vector CalculateThumbAdjustment(Thumb thumb, Rect newThumbBounds)
        {
            var thumbDelta = newThumbBounds.Position - thumb.Bounds.Position;
            return _lastDrag - thumbDelta;
        }

        private static void CoerceLength(ref double componentLength, double trackLength)
        {
            if (componentLength < 0)
            {
                componentLength = 0.0;
            }
            else if (componentLength > trackLength || double.IsNaN(componentLength))
            {
                componentLength = trackLength;
            }
        }

        private void ComputeSliderLengths(Size arrangeSize, bool isVertical, out double decreaseButtonLength, out double thumbLength, out double increaseButtonLength)
        {
            double min = Minimum;
            double range = Math.Max(0.0, Maximum - min);
            double offset = Math.Min(range, ThumbValue - min);

            double trackLength;

            // Compute thumb size
            if (isVertical)
            {
                trackLength = arrangeSize.Height;
                thumbLength = Thumb == null ? 0 : Thumb.DesiredSize.Height;
            }
            else
            {
                trackLength = arrangeSize.Width;
                thumbLength = Thumb == null ? 0 : Thumb.DesiredSize.Width;
            }

            CoerceLength(ref thumbLength, trackLength);

            double remainingTrackLength = trackLength - thumbLength;

            decreaseButtonLength = remainingTrackLength * offset / range;
            CoerceLength(ref decreaseButtonLength, remainingTrackLength);

            increaseButtonLength = remainingTrackLength - decreaseButtonLength;
            CoerceLength(ref increaseButtonLength, remainingTrackLength);

            Density = range / remainingTrackLength;
        }

        private bool ComputeScrollBarLengths(Size arrangeSize, double viewportSize, bool isVertical, out double decreaseButtonLength, out double thumbLength, out double increaseButtonLength)
        {
            var min = Minimum;
            var range = Math.Max(0.0, Maximum - min);
            var offset = Math.Min(range, ThumbValue - min);
            var extent = Math.Max(0.0, range) + viewportSize;
            var trackLength = isVertical ? arrangeSize.Height : arrangeSize.Width;
            double thumbMinLength = 10;

            StyledProperty<double> minLengthProperty = isVertical ? MinHeightProperty : MinWidthProperty;

            var thumb = Thumb;

            if (thumb != null && thumb.IsSet(minLengthProperty))
            {
                thumbMinLength = thumb.GetValue(minLengthProperty);
            }

            thumbLength = trackLength * viewportSize / extent;
            CoerceLength(ref thumbLength, trackLength);
            thumbLength = Math.Max(thumbMinLength, thumbLength);

            // If we don't have enough content to scroll, disable the track.
            var notEnoughContentToScroll = MathUtilities.LessThanOrClose(range, 0.0);
            var thumbLongerThanTrack = thumbLength > trackLength;

            // if there's not enough content or the thumb is longer than the track, 
            // hide the track and don't arrange the pieces
            if (notEnoughContentToScroll || thumbLongerThanTrack)
            {
                ShowChildren(false);
                ThumbCenterOffset = Double.NaN;
                Density = Double.NaN;
                decreaseButtonLength = 0.0;
                increaseButtonLength = 0.0;
                return false; // don't arrange
            }
            else
            {
                ShowChildren(true);
            }

            // Compute lengths of increase and decrease button
            double remainingTrackLength = trackLength - thumbLength;
            decreaseButtonLength = remainingTrackLength * offset / range;
            CoerceLength(ref decreaseButtonLength, remainingTrackLength);

            increaseButtonLength = remainingTrackLength - decreaseButtonLength;
            CoerceLength(ref increaseButtonLength, remainingTrackLength);

            Density = range / remainingTrackLength;

            return true;
        }

        private void ThumbChanged(AvaloniaPropertyChangedEventArgs e)
        {
            var oldThumb = (Thumb?)e.OldValue;
            var newThumb = (Thumb?)e.NewValue;

            if (oldThumb != null)
            {
                oldThumb.DragDelta -= ThumbDragged;
                oldThumb.DragCompleted -= ThumbDragCompleted;
                LogicalChildren.Remove(oldThumb);
                VisualChildren.Remove(oldThumb);
            }

            if (newThumb != null)
            {
                newThumb.DragDelta += ThumbDragged;
                newThumb.DragCompleted += ThumbDragCompleted;
                LogicalChildren.Add(newThumb);
                VisualChildren.Add(newThumb);
            }
        }

        private void ButtonChanged(AvaloniaPropertyChangedEventArgs e)
        {
            var oldButton = (Button?)e.OldValue;
            var newButton = (Button?)e.NewValue;

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

        private void ThumbDragged(object? sender, VectorEventArgs e)
        {
            if (IgnoreThumbDrag)
                return;

            if (DeferThumbDrag)
            {
                _deferredThumbDrag = e;
                InvalidateArrange();
            }
            else
            {
                ApplyThumbDrag(e);
            }

        }

        private void ApplyThumbDrag(VectorEventArgs e)
        {
            var delta = ValueFromDistance(e.Vector.X, e.Vector.Y);
            var factor = e.Vector / delta;
            var oldValue = Value;

            SetCurrentValue(ValueProperty, MathUtilities.Clamp(
                Value + delta,
                Minimum,
                Maximum));

            // Record the part of the drag that actually had effect as the last drag delta.
            // Due to clamping, we need to compare the two values instead of using the drag delta.
            _lastDrag = (Value - oldValue) * factor;
        }

        private void ThumbDragCompleted(object? sender, EventArgs e) => ApplyDeferredThumbDrag();

        private void ApplyDeferredThumbDrag()
        {
            if (_deferredThumbDrag != null)
            {
                ApplyThumbDrag(_deferredThumbDrag);
                _deferredThumbDrag = null;
            }
        }

        private void ShowChildren(bool visible)
        {
            // WPF sets Visible = Hidden here but we don't have that, and setting IsVisible = false
            // will cause us to stop being laid out. Instead show/hide the child controls.
            if (Thumb != null)
            {
                Thumb.IsVisible = visible;
            }

            if (IncreaseButton != null)
            {
                IncreaseButton.IsVisible = visible;
            }

            if (DecreaseButton != null)
            {
                DecreaseButton.IsVisible = visible;
            }
        }

        private void UpdatePseudoClasses(Orientation o)
        {
            PseudoClasses.Set(":vertical", o == Orientation.Vertical);
            PseudoClasses.Set(":horizontal", o == Orientation.Horizontal);
        }
    }
}
