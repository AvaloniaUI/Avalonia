// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Data;
using Avalonia.Utilities;

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    /// Base class for controls that display a value within a range.
    /// </summary>
    public abstract class RangeBase : TemplatedControl
    {
        /// <summary>
        /// Defines the <see cref="Minimum"/> property.
        /// </summary>
        public static readonly DirectProperty<RangeBase, double> MinimumProperty =
            AvaloniaProperty.RegisterDirect<RangeBase, double>(
                nameof(Minimum),
                o => o.Minimum,
                (o, v) => o.Minimum = v);

        /// <summary>
        /// Defines the <see cref="Maximum"/> property.
        /// </summary>
        public static readonly DirectProperty<RangeBase, double> MaximumProperty =
            AvaloniaProperty.RegisterDirect<RangeBase, double>(
                nameof(Maximum),
                o => o.Maximum,
                (o, v) => o.Maximum = v);

        /// <summary>
        /// Defines the <see cref="Value"/> property.
        /// </summary>
        public static readonly DirectProperty<RangeBase, double> ValueProperty =
            AvaloniaProperty.RegisterDirect<RangeBase, double>(
                nameof(Value),
                o => o.Value,
                (o, v) => o.Value = v,
                defaultBindingMode: BindingMode.TwoWay);

        /// <summary>
        /// Defines the <see cref="SmallChange"/> property.
        /// </summary>
        public static readonly StyledProperty<double> SmallChangeProperty =
            AvaloniaProperty.Register<RangeBase, double>(nameof(SmallChange), 0.1);

        /// <summary>
        /// Defines the <see cref="LargeChange"/> property.
        /// </summary>
        public static readonly StyledProperty<double> LargeChangeProperty =
            AvaloniaProperty.Register<RangeBase, double>(nameof(LargeChange), 1);

        private double _minimum;
        private double _maximum = 100.0;
        private double _value;

        /// <summary>
        /// Initializes a new instance of the <see cref="RangeBase"/> class.
        /// </summary>
        public RangeBase()
        {
        }

        /// <summary>
        /// Gets or sets the minimum value.
        /// </summary>
        public double Minimum
        {
            get
            {
                return _minimum;
            }

            set
            {
                value = ValidateMinimum(value);
                SetAndRaise(MinimumProperty, ref _minimum, value);
                Maximum = ValidateMaximum(Maximum);
                Value = ValidateValue(Value);
            }
        }

        /// <summary>
        /// Gets or sets the maximum value.
        /// </summary>
        public double Maximum
        {
            get
            {
                return _maximum;
            }

            set
            {
                value = ValidateMaximum(value);
                SetAndRaise(MaximumProperty, ref _maximum, value);
                Value = ValidateValue(Value);
            }
        }

        /// <summary>
        /// Gets or sets the current value.
        /// </summary>
        public double Value
        {
            get
            {
                return _value;
            }

            set
            {
                value = ValidateValue(value);
                SetAndRaise(ValueProperty, ref _value, value);
            }
        }

        public double SmallChange
        {
            get => GetValue(SmallChangeProperty);
            set => SetValue(SmallChangeProperty, value);
        }

        public double LargeChange
        {
            get => GetValue(LargeChangeProperty);
            set => SetValue(LargeChangeProperty, value);
        }

        /// <summary>
        /// Throws an exception if the double valus is NaN or Inf.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="property">The name of the property being set.</param>
        private static void ValidateDouble(double value, string property)
        {
            if (double.IsInfinity(value) || double.IsNaN(value))
            {
                throw new ArgumentException($"{value} is not a valid value for {property}.");
            }
        }

        /// <summary>
        /// Validates the <see cref="Minimum"/> property.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The coerced value.</returns>
        private double ValidateMinimum(double value)
        {
            ValidateDouble(value, "Minimum");
            return value;
        }

        /// <summary>
        /// Validates/coerces the <see cref="Maximum"/> property.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The coerced value.</returns>
        private double ValidateMaximum(double value)
        {
            ValidateDouble(value, "Maximum");
            return Math.Max(value, Minimum);
        }

        /// <summary>
        /// Validates/coerces the <see cref="Value"/> property.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The coerced value.</returns>
        private double ValidateValue(double value)
        {
            ValidateDouble(value, "Value");
            return MathUtilities.Clamp(value, Minimum, Maximum);
        }
    }
}
