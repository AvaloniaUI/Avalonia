using System;

namespace Avalonia.Controls
{
    /// <inheritdoc />
    public class SingleUpDown : CommonNumericUpDown<float>
    {
        /// <summary>
        /// Defines the <see cref="AllowInputSpecialValues"/> property.
        /// </summary>
        public static readonly DirectProperty<SingleUpDown, AllowedSpecialValues> AllowInputSpecialValuesProperty =
            AvaloniaProperty.RegisterDirect<SingleUpDown, AllowedSpecialValues>(nameof(AllowInputSpecialValues),
                updown => updown.AllowInputSpecialValues, (updown, v) => updown.AllowInputSpecialValues = v);

        private AllowedSpecialValues _allowInputSpecialValues;

        /// <summary>
        /// Initializes static members of the <see cref="SingleUpDown"/> class.
        /// </summary>
        static SingleUpDown() => UpdateMetadata(typeof(SingleUpDown), 1f, float.NegativeInfinity, float.PositiveInfinity);

        /// <summary>
        /// Initializes new instance of the <see cref="SingleUpDown"/> class.
        /// </summary>
        public SingleUpDown() : base(float.TryParse, decimal.ToSingle, (v1, v2) => v1 < v2, (v1, v2) => v1 > v2)
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
        protected override float? OnCoerceIncrement(float? baseValue)
        {
            if (baseValue.HasValue && float.IsNaN(baseValue.Value))
                throw new ArgumentException("NaN is invalid for Increment.");

            return base.OnCoerceIncrement(baseValue);
        }

        /// <inheritdoc />
        protected override float? OnCoerceMaximum(float? baseValue)
        {
            if (baseValue.HasValue && float.IsNaN(baseValue.Value))
                throw new ArgumentException("NaN is invalid for Maximum.");

            return base.OnCoerceMaximum(baseValue);
        }

        /// <inheritdoc />
        protected override float? OnCoerceMinimum(float? baseValue)
        {
            if (baseValue.HasValue && float.IsNaN(baseValue.Value))
                throw new ArgumentException("NaN is invalid for Minimum.");

            return base.OnCoerceMinimum(baseValue);
        }

        /// <inheritdoc />
        protected override float IncrementValue(float value, float increment) => value + increment;

        /// <inheritdoc />
        protected override float DecrementValue(float value, float increment) => value - increment;

        /// <inheritdoc />
        protected override void SetValidSpinDirection()
        {
            if (Value.HasValue && float.IsInfinity(Value.Value) && (Spinner != null))
            {
                Spinner.ValidSpinDirection = ValidSpinDirections.None;
            }
            else
            {
                base.SetValidSpinDirection();
            }
        }

        /// <inheritdoc />
        protected override float? ConvertTextToValue(string text)
        {
            var result = base.ConvertTextToValue(text);
            if (result != null)
            {
                if (float.IsNaN(result.Value))
                {
                    TestInputSpecialValue(AllowInputSpecialValues, AllowedSpecialValues.NaN);
                }
                else if (float.IsPositiveInfinity(result.Value))
                {
                    TestInputSpecialValue(AllowInputSpecialValues, AllowedSpecialValues.PositiveInfinity);
                }
                else if (float.IsNegativeInfinity(result.Value))
                {
                    TestInputSpecialValue(AllowInputSpecialValues, AllowedSpecialValues.NegativeInfinity);
                }
            }
            return result;
        }
    }
}