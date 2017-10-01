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
        private IDisposable _indeterminateBindSubscription;

        static ProgressBar()
        {
            ValueProperty.Changed.AddClassHandler<ProgressBar>(x => x.ValueChanged);

            HorizontalAlignmentProperty.OverrideDefaultValue<ProgressBar>(HorizontalAlignment.Left);
            VerticalAlignmentProperty.OverrideDefaultValue<ProgressBar>(VerticalAlignment.Top);
        }

        public bool IsIndeterminate
        {
            get => GetValue(IsIndeterminateProperty);
            set
            {
                SetValue(IsIndeterminateProperty, value);
                UpdateIsIndeterminate(value);
            }
        }

        public Orientation Orientation
        {
            get => GetValue(OrientationProperty);
            set
            {
                SetValue(OrientationProperty, value);
                UpdateOrientation(value);
            }
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
                var start = Animate.Stopwatch.Elapsed;

                if (Orientation == Orientation.Horizontal)
                {
                    _indeterminateBindSubscription = Animate.Timer.TakeWhile(x => (x - start).TotalSeconds <= 4.0)
                                                                  .Select(x => new Rect(-_indicator.Width - 5 + (x - start).TotalSeconds / 4.0 * (Bounds.Width + _indicator.Width + 10), 0, _indicator.Bounds.Width, _indicator.Bounds.Height))
                                                                  .Finally(() => start = Animate.Stopwatch.Elapsed)
                                                                  .Repeat()
                                                                  .Subscribe(x => _indicator.Arrange(x));
                }
                else
                {
                    _indeterminateBindSubscription = Animate.Timer.TakeWhile(x => (x - start).TotalSeconds <= 4.0)
                                                                  .Select(x => new Rect(0, Bounds.Height + 5 - (x - start).TotalSeconds / 4.0 * (Bounds.Height + _indicator.Height + 10), _indicator.Bounds.Width, _indicator.Bounds.Height))
                                                                  .Finally(() => start = Animate.Stopwatch.Elapsed)
                                                                  .Repeat()
                                                                  .Subscribe(x => _indicator.Arrange(x));
                }
            }
            else
                _indeterminateBindSubscription?.Dispose();
        }

        private void ValueChanged(AvaloniaPropertyChangedEventArgs e)
        {
            UpdateIndicator(Bounds.Size);
        }
    }
}
