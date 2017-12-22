// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Linq;

using Avalonia.Animation;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;

namespace Avalonia.Controls
{
    /// <summary>
    /// A control used to indicate the progress of an operation.
    /// </summary>
    public class ProgressBar : RangeBase
    {
        public static readonly StyledProperty<bool> IsIndeterminateProperty =
            AvaloniaProperty.Register<ProgressBar, bool>(nameof(IsIndeterminate));

        public static readonly StyledProperty<Orientation> OrientationProperty =
            AvaloniaProperty.Register<ProgressBar, Orientation>(nameof(Orientation), Orientation.Horizontal);

        private Border _indicator;
        private IndeterminateAnimation _indeterminateAnimation;

        static ProgressBar()
        {
            ValueProperty.Changed.AddClassHandler<ProgressBar>(x => x.ValueChanged);

            IsIndeterminateProperty.Changed.AddClassHandler<ProgressBar>(
                (p, e) => { if (p._indicator != null) p.UpdateIsIndeterminate((bool)e.NewValue); });
            OrientationProperty.Changed.AddClassHandler<ProgressBar>(
                (p, e) => { if (p._indicator != null) p.UpdateOrientation((Orientation)e.NewValue); });
        }

        public bool IsIndeterminate
        {
            get => GetValue(IsIndeterminateProperty);
            set => SetValue(IsIndeterminateProperty, value);
        }

        public Orientation Orientation
        {
            get => GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }

        /// <inheritdoc/>
        protected override Size ArrangeOverride(Size finalSize)
        {
            UpdateIndicator(finalSize);
            return base.ArrangeOverride(finalSize);
        }

        /// <inheritdoc/>
        protected override void OnTemplateApplied(TemplateAppliedEventArgs e)
        {
            _indicator = e.NameScope.Get<Border>("PART_Indicator");

            UpdateIndicator(Bounds.Size);
            UpdateOrientation(Orientation);
            UpdateIsIndeterminate(IsIndeterminate);
        }

        private void UpdateIndicator(Size bounds)
        {
            if (_indicator != null)
            {
                if (IsIndeterminate)
                {
                    if (Orientation == Orientation.Horizontal)
                        _indicator.Width = bounds.Width / 5.0;
                    else
                        _indicator.Height = bounds.Height / 5.0;
                }
                else
                {
                    double percent = Maximum == Minimum ? 1.0 : (Value - Minimum) / (Maximum - Minimum);

                    if (Orientation == Orientation.Horizontal)
                        _indicator.Width = bounds.Width * percent;
                    else
                        _indicator.Height = bounds.Height * percent;
                }
            }
        }

        private void UpdateOrientation(Orientation orientation)
        {
            if (orientation == Orientation.Horizontal)
            {
                MinHeight = 14;
                MinWidth = 200;

                _indicator.HorizontalAlignment = HorizontalAlignment.Left;
                _indicator.VerticalAlignment = VerticalAlignment.Stretch;
            }
            else
            {
                MinHeight = 200;
                MinWidth = 14;

                _indicator.HorizontalAlignment = HorizontalAlignment.Stretch;
                _indicator.VerticalAlignment = VerticalAlignment.Bottom;
            }
        }

        private void UpdateIsIndeterminate(bool isIndeterminate)
        {
            if (isIndeterminate)
            {
                if (_indeterminateAnimation == null || _indeterminateAnimation.Disposed)
                    _indeterminateAnimation = IndeterminateAnimation.StartAnimation(this);
            }
            else
                _indeterminateAnimation?.Dispose();
        }

        private void ValueChanged(AvaloniaPropertyChangedEventArgs e)
        {
            UpdateIndicator(Bounds.Size);
        }

        private class IndeterminateAnimation : IDisposable
        {
            private WeakReference<ProgressBar> _progressBar;
            private IDisposable _indeterminateBindSubscription;
            private TimeSpan _startTime;
            private bool _disposed;

            public bool Disposed => _disposed;

            private IndeterminateAnimation(ProgressBar progressBar)
            {
                _progressBar = new WeakReference<ProgressBar>(progressBar);
                _startTime = Animate.Stopwatch.Elapsed;
                _indeterminateBindSubscription = Animate.Timer.TakeWhile(x => (x - _startTime).TotalSeconds <= 4.0)
                                                              .Select(GetAnimationRect)
                                                              .Finally(() => _startTime = Animate.Stopwatch.Elapsed)
                                                              .Repeat()
                                                              .Subscribe(AnimationTick);
            }

            public static IndeterminateAnimation StartAnimation(ProgressBar progressBar)
            {
                return new IndeterminateAnimation(progressBar);
            }

            private Rect GetAnimationRect(TimeSpan time)
            {
                if (_progressBar.TryGetTarget(out var progressBar))
                {
                    if (progressBar.Orientation == Orientation.Horizontal)
                        return new Rect(-progressBar._indicator.Width - 5 + (time - _startTime).TotalSeconds / 4.0 * (progressBar.Bounds.Width + progressBar._indicator.Width + 10), 0, progressBar._indicator.Bounds.Width, progressBar._indicator.Bounds.Height);
                    else
                        return new Rect(0, progressBar.Bounds.Height + 5 - (time - _startTime).TotalSeconds / 4.0 * (progressBar.Bounds.Height + progressBar._indicator.Height + 10), progressBar._indicator.Bounds.Width, progressBar._indicator.Bounds.Height);
                }
                else
                {
                    _indeterminateBindSubscription.Dispose();
                    return Rect.Empty;
                }
            }

            private void AnimationTick(Rect rect)
            {
                if (_progressBar.TryGetTarget(out var progressBar))
                    progressBar._indicator.Arrange(rect);
                else
                    _indeterminateBindSubscription.Dispose();
            }

            public void Dispose()
            {
                _indeterminateBindSubscription?.Dispose();
                _disposed = true;
            }
        }
    }
}
