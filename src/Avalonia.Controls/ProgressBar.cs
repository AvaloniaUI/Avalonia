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

        private static readonly StyledProperty<double> IndeterminateStartingOffsetProperty =
            AvaloniaProperty.Register<ProgressBar, double>(nameof(IndeterminateStartingOffset));

        private static readonly StyledProperty<double> IndeterminateEndingOffsetProperty =
            AvaloniaProperty.Register<ProgressBar, double>(nameof(IndeterminateEndingOffset));

        private Border _indicator;

        static ProgressBar()
        {
            PseudoClass(OrientationProperty, o => o == Avalonia.Controls.Orientation.Vertical, ":vertical");
            PseudoClass(OrientationProperty, o => o == Avalonia.Controls.Orientation.Horizontal, ":horizontal");
            PseudoClass(IsIndeterminateProperty, ":indeterminate");

            ValueProperty.Changed.AddClassHandler<ProgressBar>(x => x.ValueChanged);
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

        private double IndeterminateStartingOffset
        {
            get => GetValue(IndeterminateStartingOffsetProperty);
            set => SetValue(IndeterminateStartingOffsetProperty, value);
        }

        private double IndeterminateEndingOffset
        {
            get => GetValue(IndeterminateEndingOffsetProperty);
            set => SetValue(IndeterminateEndingOffsetProperty, value);
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
        }

        private void UpdateIndicator(Size bounds)
        {
            if (_indicator != null)
            {
                if (IsIndeterminate)
                {
                    if (Orientation == Orientation.Horizontal)
                    {
                        var width = bounds.Width / 5.0;
                        IndeterminateStartingOffset = -width;
                        _indicator.Width = width;
                        IndeterminateEndingOffset = bounds.Width;

                    }
                    else
                    {
                        var height = bounds.Height / 5.0;
                        IndeterminateStartingOffset = -bounds.Height;
                        _indicator.Height = height;
                        IndeterminateEndingOffset = height;
                    }
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

        private void ValueChanged(AvaloniaPropertyChangedEventArgs e)
        {
            UpdateIndicator(Bounds.Size);
        }
    }
}
