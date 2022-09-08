using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace Avalonia.Controls.Converters
{
    /// <summary>
    /// Converter that will do nothing (not update bound values) when a null value is encountered.
    /// This converter enables binding nullable with non-nullable properties in some scenarios.
    /// </summary>
    public class DoNothingForNullConverter : IValueConverter
    {
        /// <inheritdoc/>
        public object? Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture)
        {
            return value ?? BindingOperations.DoNothing;
        }

        /// <inheritdoc/>
        public object? ConvertBack(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture)
        {
            return value ?? BindingOperations.DoNothing;
        }
    }
}

