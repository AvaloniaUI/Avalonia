// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex.Utilities;

namespace Perspex.Controls.Primitives
{
    /// <summary>
    /// Base class for controls that display a value within a range.
    /// </summary>
    public abstract class RangeBase : TemplatedControl
    {
        /// <summary>
        /// Defines the <see cref="Minimum"/> property.
        /// </summary>
        public static readonly PerspexProperty<double> MinimumProperty =
            PerspexProperty.Register<RangeBase, double>(
                nameof(Minimum),
                validate: ValidateMinimum);

        /// <summary>
        /// Defines the <see cref="Maximum"/> property.
        /// </summary>
        public static readonly PerspexProperty<double> MaximumProperty =
            PerspexProperty.Register<RangeBase, double>(
                nameof(Maximum),
                defaultValue: 100.0,
                validate: ValidateMaximum);

        /// <summary>
        /// Defines the <see cref="Value"/> property.
        /// </summary>
        public static readonly PerspexProperty<double> ValueProperty =
            PerspexProperty.Register<RangeBase, double>(
                nameof(Value),
                validate: ValidateValue);

        /// <summary>
        /// Initializes a new instance of the <see cref="RangeBase"/> class.
        /// </summary>
        public RangeBase()
        {
            AffectsValidation(MinimumProperty, MaximumProperty, ValueProperty);
            AffectsValidation(MaximumProperty, ValueProperty);
        }

        /// <summary>
        /// Gets or sets the minimum value.
        /// </summary>
        public double Minimum
        {
            get { return this.GetValue(MinimumProperty); }
            set { this.SetValue(MinimumProperty, value); }
        }

        /// <summary>
        /// Gets or sets the maximum value.
        /// </summary>
        public double Maximum
        {
            get { return this.GetValue(MaximumProperty); }
            set { this.SetValue(MaximumProperty, value); }
        }

        /// <summary>
        /// Gets or sets the current value.
        /// </summary>
        public double Value
        {
            get { return this.GetValue(ValueProperty); }
            set { this.SetValue(ValueProperty, value); }
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
        /// <param name="sender">The RangeBase control.</param>
        /// <param name="value">The value.</param>
        /// <returns>The coerced value.</returns>
        private static double ValidateMinimum(RangeBase sender, double value)
        {
            ValidateDouble(value, "Minimum");
            return value;
        }

        /// <summary>
        /// Validates/coerces the <see cref="Maximum"/> property.
        /// </summary>
        /// <param name="sender">The RangeBase control.</param>
        /// <param name="value">The value.</param>
        /// <returns>The coerced value.</returns>
        private static double ValidateMaximum(RangeBase sender, double value)
        {
            ValidateDouble(value, "Maximum");
            return Math.Max(value, sender.Minimum);
        }

        /// <summary>
        /// Validates/coerces the <see cref="Value"/> property.
        /// </summary>
        /// <param name="sender">The RangeBase control.</param>
        /// <param name="value">The value.</param>
        /// <returns>The coerced value.</returns>
        private static double ValidateValue(RangeBase sender, double value)
        {
            ValidateDouble(value, "Value");
            return MathUtilities.Clamp(value, sender.Minimum, sender.Maximum);
        }
    }
}
