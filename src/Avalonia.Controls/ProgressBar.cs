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
            PseudoClass(OrientationProperty, o => o == Avalonia.Controls.Orientation.Vertical, ":vertical");
            PseudoClass(OrientationProperty, o => o == Avalonia.Controls.Orientation.Horizontal, ":horizontal");

            ValueProperty.Changed.AddClassHandler<ProgressBar>(x => x.ValueChanged);

            IsIndeterminateProperty.Changed.AddClassHandler<ProgressBar>(
                (p, e) => { if (p._indicator != null) p.UpdateIsIndeterminate((bool)e.NewValue); });
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

        // TODO: Implement Indeterminate Progress animation
        //       in xaml (most ideal) or if it's not possible
        //       then on this class.
        private class IndeterminateAnimation : IDisposable
        {
            private WeakReference<ProgressBar> _progressBar;

            private bool _disposed;

            public bool Disposed => _disposed;

            private IndeterminateAnimation(ProgressBar progressBar)
            {
                _progressBar = new WeakReference<ProgressBar>(progressBar);

            }

            public static IndeterminateAnimation StartAnimation(ProgressBar progressBar)
            {
                return new IndeterminateAnimation(progressBar);
            }

            private Rect GetAnimationRect(TimeSpan time)
            {
                return Rect.Empty;
            }

            public void Dispose()
            {
                _disposed = true;
            }
        }
    }
}
