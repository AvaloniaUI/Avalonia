using System;

namespace Avalonia.Controls
{
    /// <inheritdoc />
    public class DoubleUpDown : CommonNumericUpDown<double>
    {
        /// <summary>
        /// Defines the <see cref="AllowInputSpecialValues"/> property.
        /// </summary>
        public static readonly DirectProperty<DoubleUpDown, AllowedSpecialValues> AllowInputSpecialValuesProperty =
            AvaloniaProperty.RegisterDirect<DoubleUpDown, AllowedSpecialValues>(nameof(AllowInputSpecialValues),
                updown => updown.AllowInputSpecialValues, (updown, v) => updown.AllowInputSpecialValues = v);

        private AllowedSpecialValues _allowInputSpecialValues;

        /// <summary>
        /// Initializes static members of the <see cref="DoubleUpDown"/> class.
        /// </summary>
        static DoubleUpDown() => UpdateMetadata(typeof(DoubleUpDown), 1d, double.NegativeInfinity, double.PositiveInfinity);

        /// <summary>
        /// Initializes new instance of the <see cref="DoubleUpDown"/> class.
        /// </summary>
        public DoubleUpDown() : base(double.TryParse, decimal.ToDouble, (v1, v2) => v1 < v2, (v1, v2) => v1 > v2)
        {
        }

        /// <summary>
        /// Gets or sets a value representing the special values the user is allowed to input, such as "Infinity", "-Infinity" and "NaN" values.
        /// </summary>
        public AllowedSpecialValues AllowInputSpecialValues
        {
            get { return _allowInputSpecialValues; }
            set { SetAndRaise(AllowInputSpecialValuesProperty, ref _allowInputSpecialValues, value); }
        }

        /// <inheritdoc />
        protected override double IncrementValue(double value, double increment) => value + increment;

        /// <inheritdoc />
        protected override double DecrementValue(double value, double increment) => value - increment;

        /// <inheritdoc />
        protected override double? OnCoerceIncrement(double? baseValue)
        {
            if (baseValue.HasValue && double.IsNaN(baseValue.Value))
            {
                throw new ArgumentException("NaN is invalid for Increment.");
            }
            return base.OnCoerceIncrement(baseValue);
        }

        /// <inheritdoc />
        protected override double? OnCoerceMaximum(double? baseValue)
        {
            if (baseValue.HasValue && double.IsNaN(baseValue.Value))
            {
                throw new ArgumentException("NaN is invalid for Maximum.");
            }
            return base.OnCoerceMaximum(baseValue);
        }

        /// <inheritdoc />
        protected override double? OnCoerceMinimum(double? baseValue)
        {
            if (baseValue.HasValue && double.IsNaN(baseValue.Value))
            {
                throw new ArgumentException("NaN is invalid for Minimum.");
            }
            return base.OnCoerceMinimum(baseValue);
        }

        /// <inheritdoc />
        protected override void SetValidSpinDirection()
        {
            if (Value.HasValue && double.IsInfinity(Value.Value) && (Spinner != null))
            {
                Spinner.ValidSpinDirection = ValidSpinDirections.None;
            }
            else
            {
                base.SetValidSpinDirection();
            }
        }

        /// <inheritdoc />
        protected override double? ConvertTextToValue(string text)
        {
            var result = base.ConvertTextToValue(text);
            if (result != null)
            {
                if (double.IsNaN(result.Value))
                {
                    TestInputSpecialValue(AllowInputSpecialValues, AllowedSpecialValues.NaN);
                }
                else if (double.IsPositiveInfinity(result.Value))
                {
                    TestInputSpecialValue(AllowInputSpecialValues, AllowedSpecialValues.PositiveInfinity);
                }
                else if (double.IsNegativeInfinity(result.Value))
                {
                    TestInputSpecialValue(AllowInputSpecialValues, AllowedSpecialValues.NegativeInfinity);
                }
            }
            return result;
        }
    }
}