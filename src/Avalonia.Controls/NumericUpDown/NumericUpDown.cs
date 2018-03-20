using System;
using System.Globalization;

namespace Avalonia.Controls
{
    /// <summary>
    /// Base class for controls that represents a TextBox with button spinners that allow incrementing and decrementing numeric values.
    /// </summary>
    public abstract class NumericUpDown<T> : UpDownBase<T>
    {
        /// <summary>
        /// Defines the <see cref="FormatString"/> property.
        /// </summary>
        public static readonly StyledProperty<string> FormatStringProperty =
            AvaloniaProperty.Register<NumericUpDown<T>, string>(nameof(FormatString), string.Empty);

        /// <summary>
        /// Defines the <see cref="Increment"/> property.
        /// </summary>
        public static readonly StyledProperty<T> IncrementProperty =
            AvaloniaProperty.Register<NumericUpDown<T>, T>(nameof(Increment), default(T), validate: OnCoerceIncrement);

        /// <summary>
        /// Initializes static members of the <see cref="NumericUpDown{T}"/> class.
        /// </summary>
        static NumericUpDown()
        {
            FormatStringProperty.Changed.Subscribe(FormatStringChanged);
            IncrementProperty.Changed.Subscribe(IncrementChanged);
        }

        /// <summary>
        /// Gets or sets the display format of the <see cref="UpDownBase{T}.Value"/>.
        /// </summary>
        public string FormatString
        {
            get { return GetValue(FormatStringProperty); }
            set { SetValue(FormatStringProperty, value); }
        }

        /// <summary>
        /// Gets or sets the amount in which to increment the <see cref="UpDownBase{T}.Value"/>.
        /// </summary>
        public T Increment
        {
            get { return GetValue(IncrementProperty); }
            set { SetValue(IncrementProperty, value); }
        }

        /// <summary>
        /// Called when the <see cref="FormatString"/> property value changed.
        /// </summary>
        /// <param name="oldValue">The old value.</param>
        /// <param name="newValue">The new value.</param>
        protected virtual void OnFormatStringChanged(string oldValue, string newValue)
        {
            if (IsInitialized)
            {
                SyncTextAndValueProperties(false, null);
            }
        }

        /// <summary>
        /// Called when the <see cref="Increment"/> property value changed.
        /// </summary>
        /// <param name="oldValue">The old value.</param>
        /// <param name="newValue">The new value.</param>
        protected virtual void OnIncrementChanged(T oldValue, T newValue)
        {
            if (IsInitialized)
            {
                SetValidSpinDirection();
            }
        }

        /// <summary>
        /// Called when the <see cref="Increment"/> property has to be coerced.
        /// </summary>
        /// <param name="baseValue">The value.</param>
        protected virtual T OnCoerceIncrement(T baseValue)
        {
            return baseValue;
        }

        /// <summary>
        /// Called when the <see cref="Increment"/> property value changed.
        /// </summary>
        /// <param name="e">The event args.</param>
        private static void IncrementChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Sender is NumericUpDown<T> upDown)
            {
                var oldValue = (T)e.OldValue;
                var newValue = (T)e.NewValue;
                upDown.OnIncrementChanged(oldValue, newValue);
            }
        }

        /// <summary>
        /// Called when the <see cref="FormatString"/> property value changed.
        /// </summary>
        /// <param name="e">The event args.</param>
        private static void FormatStringChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Sender is NumericUpDown<T> upDown)
            {
                var oldValue = (string) e.OldValue;
                var newValue = (string) e.NewValue;
                upDown.OnFormatStringChanged(oldValue, newValue);
            }
        }

        private static T OnCoerceIncrement(NumericUpDown<T> numericUpDown, T value)
        {
            return numericUpDown.OnCoerceIncrement(value);
        }

        /// <summary>
        /// Parse percent format text
        /// </summary>
        /// <param name="text">Text to parse.</param>
        /// <param name="cultureInfo">The culture info.</param>
        protected static decimal ParsePercent(string text, IFormatProvider cultureInfo)
        {
            var info = NumberFormatInfo.GetInstance(cultureInfo);
            text = text.Replace(info.PercentSymbol, null);
            var result = decimal.Parse(text, NumberStyles.Any, info);
            result = result / 100;
            return result;
        }
    }
}