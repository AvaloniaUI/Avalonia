// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;

namespace Avalonia.Controls
{
    /// <summary>
    /// A control that lets the user select from a range of values by moving a Thumb control along a Track.
    /// </summary>
    public class Slider : RangeBase
    {
        /// <summary>
        /// Defines the <see cref="Orientation"/> property.
        /// </summary>
        public static readonly StyledProperty<Orientation> OrientationProperty =
            ScrollBar.OrientationProperty.AddOwner<Slider>();

        /// <summary>
        /// Defines the <see cref="IsSnapToTickEnabled"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsSnapToTickEnabledProperty =
            AvaloniaProperty.Register<Slider, bool>(nameof(IsSnapToTickEnabled), false);

        /// <summary>
        /// Defines the <see cref="TickFrequency"/> property.
        /// </summary>
        public static readonly StyledProperty<double> TickFrequencyProperty =
            AvaloniaProperty.Register<Slider, double>(nameof(TickFrequency), 0.0);

        // Slider required parts
        private Track _track;
        private Button _decreaseButton;
        private Button _increaseButton;

        /// <summary>
        /// Initializes static members of the <see cref="Slider"/> class. 
        /// </summary>
        static Slider()
        {
            OrientationProperty.OverrideDefaultValue(typeof(Slider), Orientation.Horizontal);
            PseudoClass<Slider, Orientation>(OrientationProperty, o => o == Orientation.Vertical, ":vertical");
            PseudoClass<Slider, Orientation>(OrientationProperty, o => o == Orientation.Horizontal, ":horizontal");
            Thumb.DragStartedEvent.AddClassHandler<Slider>(x => x.OnThumbDragStarted, RoutingStrategies.Bubble);
            Thumb.DragDeltaEvent.AddClassHandler<Slider>(x => x.OnThumbDragDelta, RoutingStrategies.Bubble);
            Thumb.DragCompletedEvent.AddClassHandler<Slider>(x => x.OnThumbDragCompleted, RoutingStrategies.Bubble);
        }

        /// <summary>
        /// Instantiates a new instance of the <see cref="Slider"/> class. 
        /// </summary>
        public Slider()
        {
        }

        /// <summary>
        /// Gets or sets the orientation of a <see cref="Slider"/>.
        /// </summary>
        public Orientation Orientation
        {
            get { return GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether the <see cref="Slider"/> automatically moves the <see cref="Thumb"/> to the closest tick mark.
        /// </summary>
        public bool IsSnapToTickEnabled
        {
            get { return GetValue(IsSnapToTickEnabledProperty); }
            set { SetValue(IsSnapToTickEnabledProperty, value); }
        }

        /// <summary>
        /// Gets or sets the interval between tick marks.
        /// </summary>
        public double TickFrequency
        {
            get { return GetValue(TickFrequencyProperty); }
            set { SetValue(TickFrequencyProperty, value); }
        }

        /// <inheritdoc/>
        protected override void OnTemplateApplied(TemplateAppliedEventArgs e)
        {
            if (_decreaseButton != null)
            {
                _decreaseButton.Click -= DecreaseClick;
            }

            if (_increaseButton != null)
            {
                _increaseButton.Click -= IncreaseClick;
            }

            _decreaseButton = e.NameScope.Find<Button>("PART_DecreaseButton");
            _track = e.NameScope.Find<Track>("PART_Track");
            _increaseButton = e.NameScope.Find<Button>("PART_IncreaseButton");

            if (_decreaseButton != null)
            {
                _decreaseButton.Click += DecreaseClick;
            }

            if (_increaseButton != null)
            {
                _increaseButton.Click += IncreaseClick;
            }
        }

        private void DecreaseClick(object sender, RoutedEventArgs e)
        {
            ChangeValueBy(-LargeChange);
        }

        private void IncreaseClick(object sender, RoutedEventArgs e)
        {
            ChangeValueBy(LargeChange);
        }

        private void ChangeValueBy(double by)
        {
            if (IsSnapToTickEnabled)
            {
                by = by < 0 ? Math.Min(-TickFrequency, by) : Math.Max(TickFrequency, by);
            }

            var value = Value;
            var next = SnapToTick(Math.Max(Math.Min(value + by, Maximum), Minimum));
            if (next != value)
            {
                Value = next;
            }
        }

        /// <summary>
        /// Called when user start dragging the <see cref="Thumb"/>.
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnThumbDragStarted(VectorEventArgs e)
        {
        }

        /// <summary>
        /// Called when user dragging the <see cref="Thumb"/>.
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnThumbDragDelta(VectorEventArgs e)
        {
            Thumb thumb = e.Source as Thumb;
            if (thumb != null && _track?.Thumb == thumb)
            {
                MoveToNextTick(_track.Value);
            }
        }

        /// <summary>
        /// Called when user stop dragging the <see cref="Thumb"/>.
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnThumbDragCompleted(VectorEventArgs e)
        {
        }

        /// <summary>
        /// Searches for the closest tick and sets Value to that tick.
        /// </summary>
        /// <param name="value">Value that want to snap to closest Tick.</param>
        private void MoveToNextTick(double value)
        {
            Value = SnapToTick(Math.Max(Minimum, Math.Min(Maximum, value)));
        }

        /// <summary>
        /// Snap the input 'value' to the closest tick.
        /// </summary>
        /// <param name="value">Value that want to snap to closest Tick.</param>
        private double SnapToTick(double value)
        {
            if (IsSnapToTickEnabled && TickFrequency > 0.0)
            {
                double previous = Minimum + (Math.Round(((value - Minimum) / TickFrequency)) * TickFrequency);
                double next = Math.Min(Maximum, previous + TickFrequency);
                value = value > (previous + next) * 0.5 ? next : previous;
            }

            return value;
        }
    }
}
