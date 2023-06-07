using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace Avalonia.Controls.Converters
{
    /// <summary>
    /// Converter to convert an enum value to bool by comparing to the given parameter.
    /// Both value and parameter must be of the same enum type.
    /// </summary>
    /// <remarks>
    /// This converter is useful to enable binding of radio buttons with a selected enum value.
    /// </remarks>
    public class EnumToBoolConverter : IValueConverter
    {
        /// <inheritdoc/>
        public object? Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture)
        {
            if (value == null &&
                parameter == null)
            {
                return true;
            }
            else if (value == null ||
                     parameter == null)
            {
                return false;
            }
            else
            {
                return value!.Equals(parameter);
            }
        }

        /// <inheritdoc/>
        public object? ConvertBack(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture)
        {
            if (value is bool boolValue &&
                boolValue == true)
            {
                return parameter;
            }

            return BindingOperations.DoNothing;
        }
    }
}
