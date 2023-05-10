using System;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace Avalonia.Controls
{
    /// <summary>
    /// Represents a control that raises its <see cref="Button.Click"/> event repeatedly when it is pressed and held.
    /// </summary>
    public class RepeatButton : Button
    {
        /// <summary>
        /// Defines the <see cref="Interval"/> property.
        /// </summary>
        public static readonly StyledProperty<int> IntervalProperty =
            AvaloniaProperty.Register<RepeatButton, int>(nameof(Interval), 100);

        /// <summary>
        /// Defines the <see cref="Delay"/> property.
        /// </summary>
        public static readonly StyledProperty<int> DelayProperty =
            AvaloniaProperty.Register<RepeatButton, int>(nameof(Delay), 300);

        private DispatcherTimer? _repeatTimer;

        /// <summary>
        /// Gets or sets the amount of time, in milliseconds, of repeating clicks.
        /// </summary>
        public int Interval
        {
            get { return GetValue(IntervalProperty); }
            set { SetValue(IntervalProperty, value); }
        }

        /// <summary>
        /// Gets or sets the amount of time, in milliseconds, to wait before repeating begins.
        /// </summary>
        public int Delay
        {
            get { return GetValue(DelayProperty); }
            set { SetValue(DelayProperty, value); }
        }

        private void StartTimer(RoutedEventArgs e)
        {
            if (_repeatTimer == null)
            {
                _repeatTimer = new DispatcherTimer();
                _repeatTimer.Tick += (s, inner) => {
                    RepeatTimerOnTick(s, e);
                };
            }

            if (_repeatTimer.IsEnabled) return;

            _repeatTimer.Interval = TimeSpan.FromMilliseconds(Delay);
            _repeatTimer.Start();
        }

        private void RepeatTimerOnTick(object? sender, RoutedEventArgs e)
        {
            var interval = TimeSpan.FromMilliseconds(Interval);
            if (_repeatTimer!.Interval != interval)
            {
                _repeatTimer.Interval = interval;
            }
            OnClick(e);
        }

        private void StopTimer()
        {
            _repeatTimer?.Stop();
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == IsPressedProperty && change.GetNewValue<bool>() == false)
            {
                StopTimer();
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key == Key.Space)
            {
                StartTimer(e);
            }
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);

            StopTimer();
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);

            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                StartTimer(e);
            }
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);

            if (e.InitialPressMouseButton == MouseButton.Left)
            {
                StopTimer();
            }
        }
    }
}
