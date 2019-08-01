// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.


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

        private static readonly DirectProperty<ProgressBar, double> IndeterminateStartingOffsetProperty =
            AvaloniaProperty.RegisterDirect<ProgressBar, double>(
                nameof(IndeterminateStartingOffset),
                p => p.IndeterminateStartingOffset,
                (p, o) => p.IndeterminateStartingOffset = o);

        private static readonly DirectProperty<ProgressBar, double> IndeterminateEndingOffsetProperty =
            AvaloniaProperty.RegisterDirect<ProgressBar, double>(
                nameof(IndeterminateEndingOffset),
                p => p.IndeterminateEndingOffset,
                (p, o) => p.IndeterminateEndingOffset = o);

        private Border _indicator;

        static ProgressBar()
        {
            PseudoClass<ProgressBar, Orientation>(OrientationProperty, o => o == Orientation.Vertical, ":vertical");
            PseudoClass<ProgressBar, Orientation>(OrientationProperty, o => o == Orientation.Horizontal, ":horizontal");
            PseudoClass<ProgressBar>(IsIndeterminateProperty, ":indeterminate");

            ValueProperty.Changed.AddClassHandler<ProgressBar>(x => x.UpdateIndicatorWhenPropChanged);
            IsIndeterminateProperty.Changed.AddClassHandler<ProgressBar>(x => x.UpdateIndicatorWhenPropChanged);
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
        private double _indeterminateStartingOffset;
        private double IndeterminateStartingOffset
        {
            get => _indeterminateStartingOffset;
            set => SetAndRaise(IndeterminateStartingOffsetProperty, ref _indeterminateStartingOffset, value);
        }

        private double _indeterminateEndingOffset;
        private double IndeterminateEndingOffset
        {
            get => _indeterminateEndingOffset;
            set => SetAndRaise(IndeterminateEndingOffsetProperty, ref _indeterminateEndingOffset, value);
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

        private void UpdateIndicatorWhenPropChanged(AvaloniaPropertyChangedEventArgs e)
        {
            UpdateIndicator(Bounds.Size);
        }
    }
}
