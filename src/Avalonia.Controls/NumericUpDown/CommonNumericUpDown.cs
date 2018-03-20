using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Avalonia.Controls
{
    /// <summary>
    /// Base class for controls that represents a TextBox with button spinners that allow incrementing and decrementing numeric values.
    /// </summary>
    public abstract class CommonNumericUpDown<T> : NumericUpDown<T?> where T : struct, IFormattable, IComparable<T>
    {
        protected delegate bool FromText(string s, NumberStyles style, IFormatProvider provider, out T result);
        protected delegate T FromDecimal(decimal d);

        private readonly FromText _fromText;
        private readonly FromDecimal _fromDecimal;
        private readonly Func<T, T, bool> _fromLowerThan;
        private readonly Func<T, T, bool> _fromGreaterThan;

        private NumberStyles _parsingNumberStyle = NumberStyles.Any;

        /// <summary>
        /// Defines the <see cref="ParsingNumberStyle"/> property.
        /// </summary>
        public static readonly DirectProperty<CommonNumericUpDown<T>, NumberStyles> ParsingNumberStyleProperty =
            AvaloniaProperty.RegisterDirect<CommonNumericUpDown<T>, NumberStyles>(nameof(ParsingNumberStyle),
                updown => updown.ParsingNumberStyle, (updown, style) => updown.ParsingNumberStyle = style);


        /// <summary>
        /// Initializes new instance of the <see cref="CommonNumericUpDown{T}"/> class.
        /// </summary>
        /// <param name="fromText">Delegate to parse value from text.</param>
        /// <param name="fromDecimal">Delegate to parse value from decimal.</param>
        /// <param name="fromLowerThan">Delegate to compare if one value is lower than another.</param>
        /// <param name="fromGreaterThan">Delegate to compare if one value is greater than another.</param>
        protected CommonNumericUpDown(FromText fromText, FromDecimal fromDecimal, Func<T, T, bool> fromLowerThan, Func<T, T, bool> fromGreaterThan)
        {
            _fromText = fromText ?? throw new ArgumentNullException(nameof(fromText));
            _fromDecimal = fromDecimal ?? throw new ArgumentNullException(nameof(fromDecimal));
            _fromLowerThan = fromLowerThan ?? throw new ArgumentNullException(nameof(fromLowerThan));
            _fromGreaterThan = fromGreaterThan ?? throw new ArgumentNullException(nameof(fromGreaterThan));
        }

        /// <summary>
        /// Gets or sets the parsing style (AllowLeadingWhite, Float, AllowHexSpecifier, ...). By default, Any.
        /// </summary>
        public NumberStyles ParsingNumberStyle
        {
            get { return _parsingNumberStyle; }
            set { SetAndRaise(ParsingNumberStyleProperty, ref _parsingNumberStyle, value); }
        }

        /// <inheritdoc />
        protected override void OnIncrement()
        {
            if (!HandleNullSpin())
            {
                var result = IncrementValue(Value.Value, Increment.Value);
                Value = CoerceValueMinMax(result);
            }
        }

        /// <inheritdoc />
        protected override void OnDecrement()
        {
            if (!HandleNullSpin())
            {
                var result = DecrementValue(Value.Value, Increment.Value);
                Value = CoerceValueMinMax(result);
            }
        }

        /// <inheritdoc />
        protected override void OnMinimumChanged(T? oldValue, T? newValue)
        {
            base.OnMinimumChanged(oldValue, newValue);

            if (Value.HasValue && ClipValueToMinMax)
            {
                Value = CoerceValueMinMax(Value.Value);
            }
        }

        /// <inheritdoc />
        protected override void OnMaximumChanged(T? oldValue, T? newValue)
        {
            base.OnMaximumChanged(oldValue, newValue);

            if (Value.HasValue && ClipValueToMinMax)
            {
                Value = CoerceValueMinMax(Value.Value);
            }
        }

        /// <inheritdoc />
        protected override T? ConvertTextToValue(string text)
        {
            T? result = null;

            if (string.IsNullOrEmpty(text))
            {
                return result;
            }

            // Since the conversion from Value to text using a FormartString may not be parsable,
            // we verify that the already existing text is not the exact same value.
            var currentValueText = ConvertValueToText();
            if (Equals(currentValueText, text))
            {
                return Value;
            }

            result = ConvertTextToValueCore(currentValueText, text);

            if (ClipValueToMinMax)
            {
                return GetClippedMinMaxValue(result);
            }

            ValidateDefaultMinMax(result);

            return result;
        }

        /// <inheritdoc />
        protected override string ConvertValueToText()
        {
            if (Value == null)
            {
                return string.Empty;
            }

            //Manage FormatString of type "{}{0:N2} °" (in xaml) or "{0:N2} °" in code-behind.
            if (FormatString.Contains("{0"))
            {
                return string.Format(CultureInfo, FormatString, Value.Value);
            }

            return Value.Value.ToString(FormatString, CultureInfo);
        }

        /// <inheritdoc />
        protected override void SetValidSpinDirection()
        {
            var validDirections = ValidSpinDirections.None;

            // Null increment always prevents spin.
            if (Increment != null && !IsReadOnly)
            {
                if (IsLowerThan(Value, Maximum) || !Value.HasValue || !Maximum.HasValue)
                {
                    validDirections = validDirections | ValidSpinDirections.Increase;
                }

                if (IsGreaterThan(Value, Minimum) || !Value.HasValue || !Minimum.HasValue)
                {
                    validDirections = validDirections | ValidSpinDirections.Decrease;
                }
            }

            if (Spinner != null)
            {
                Spinner.ValidSpinDirection = validDirections;
            }
        }

        /// <summary>
        /// Checks if provided value is within allowed values.
        /// </summary>
        /// <param name="allowedValues">The alowed values.</param>
        /// <param name="valueToCompare">The value to check.</param>
        protected void TestInputSpecialValue(AllowedSpecialValues allowedValues, AllowedSpecialValues valueToCompare)
        {
            if ((allowedValues & valueToCompare) != valueToCompare)
            {
                switch (valueToCompare)
                {
                    case AllowedSpecialValues.NaN:
                        throw new InvalidDataException("Value to parse shouldn't be NaN.");
                    case AllowedSpecialValues.PositiveInfinity:
                        throw new InvalidDataException("Value to parse shouldn't be Positive Infinity.");
                    case AllowedSpecialValues.NegativeInfinity:
                        throw new InvalidDataException("Value to parse shouldn't be Negative Infinity.");
                }
            }
        }

        protected static void UpdateMetadata(Type type, T? increment, T? minimun, T? maximum)
        {
            IncrementProperty.OverrideDefaultValue(type, increment);
            MinimumProperty.OverrideDefaultValue(type, minimun);
            MaximumProperty.OverrideDefaultValue(type, maximum);
        }

        protected abstract T IncrementValue(T value, T increment);

        protected abstract T DecrementValue(T value, T increment);

        private bool IsLowerThan(T? value1, T? value2)
        {
            if (value1 == null || value2 == null)
            {
                return false;
            }
            return _fromLowerThan(value1.Value, value2.Value);
        }

        private bool IsGreaterThan(T? value1, T? value2)
        {
            if (value1 == null || value2 == null)
            {
                return false;
            }
            return _fromGreaterThan(value1.Value, value2.Value);
        }

        private bool HandleNullSpin()
        {
            if (!Value.HasValue)
            {
                var forcedValue = DefaultValue ?? default(T);
                Value = CoerceValueMinMax(forcedValue);
                return true;
            }
            else if (!Increment.HasValue)
            {
                return true;
            }
            return false;
        }

        internal bool IsValid(T? value)
        {
            return !IsLowerThan(value, Minimum) && !IsGreaterThan(value, Maximum);
        }

        private T? CoerceValueMinMax(T value)
        {
            if (IsLowerThan(value, Minimum))
            {
                return Minimum;
            }
            else if (IsGreaterThan(value, Maximum))
            {
                return Maximum;
            }
            else
            {
                return value;
            }
        }

        private bool IsPercent(string stringToTest)
        {
            var PIndex = stringToTest.IndexOf("P", StringComparison.Ordinal);
            if (PIndex >= 0)
            {
                //stringToTest contains a "P" between 2 "'", it's considered as text, not percent
                var isText = stringToTest.Substring(0, PIndex).Contains("'")
                             && stringToTest.Substring(PIndex, FormatString.Length - PIndex).Contains("'");

                return !isText;
            }
            return false;
        }

        private T? ConvertTextToValueCore(string currentValueText, string text)
        {
            T? result;

            if (IsPercent(FormatString))
            {
                result = _fromDecimal(ParsePercent(text, CultureInfo));
            }
            else
            {
                // Problem while converting new text
                if (!_fromText(text, ParsingNumberStyle, CultureInfo, out T outputValue))
                {
                    var shouldThrow = true;

                    // Check if CurrentValueText is also failing => it also contains special characters. ex : 90°
                    if (!_fromText(currentValueText, ParsingNumberStyle, CultureInfo, out T _))
                    {
                        // extract non-digit characters
                        var currentValueTextSpecialCharacters = currentValueText.Where(c => !char.IsDigit(c));
                        var textSpecialCharacters = text.Where(c => !char.IsDigit(c));
                        // same non-digit characters on currentValueText and new text => remove them on new Text to parse it again.
                        if (currentValueTextSpecialCharacters.Except(textSpecialCharacters).ToList().Count == 0)
                        {
                            foreach (var character in textSpecialCharacters)
                            {
                                text = text.Replace(character.ToString(), string.Empty);
                            }
                            // if without the special characters, parsing is good, do not throw
                            if (_fromText(text, ParsingNumberStyle, CultureInfo, out outputValue))
                            {
                                shouldThrow = false;
                            }
                        }
                    }

                    if (shouldThrow)
                    {
                        throw new InvalidDataException("Input string was not in a correct format.");
                    }
                }
                result = outputValue;
            }
            return result;
        }

        private T? GetClippedMinMaxValue(T? result)
        {
            if (IsGreaterThan(result, Maximum))
            {
                return Maximum;
            }
            else if (IsLowerThan(result, Minimum))
            {
                return Minimum;
            }
            return result;
        }

        private void ValidateDefaultMinMax(T? value)
        {
            // DefaultValue is always accepted.
            if (Equals(value, DefaultValue))
            {
                return;
            }

            if (IsLowerThan(value, Minimum))
            {
                throw new ArgumentOutOfRangeException(nameof(Minimum), string.Format("Value must be greater than Minimum value of {0}", Minimum));
            }
            else if (IsGreaterThan(value, Maximum))
            {
                throw new ArgumentOutOfRangeException(nameof(Maximum), string.Format("Value must be less than Maximum value of {0}", Maximum));
            }
        }
    }
}