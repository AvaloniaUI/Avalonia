using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Avalonia.Controls.Converters
{
    /// <summary>
    /// Converter that checks if an enum value is equal to the given parameter enum value.
    /// </summary>
    public class EnumValueEqualsConverter : IValueConverter
    {
        /// <inheritdoc/>
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // Note: Unlike string comparisons, null/empty is not supported
            // Both 'value' and 'parameter' must exist and if both are missing they are not considered equal
            if (value != null &&
                parameter != null)
            {
                Type type = value.GetType();

                if (type.IsEnum)
                {
                    var valueStr = value?.ToString();
                    var paramStr = parameter?.ToString();

                    if (string.Equals(valueStr, paramStr, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                /*
                // TODO: When .net Standard 2.0 is no longer supported the code can be changed to below
                // This is a little more type safe
                if (type.IsEnum &&
                    Enum.TryParse(type, value?.ToString(), true, out object? valueEnum) &&
                    Enum.TryParse(type, parameter?.ToString(), true, out object? paramEnum))
                {
                    return valueEnum == paramEnum;
                }
                */
            }

            return false;
        }

        /// <inheritdoc/>
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new System.NotImplementedException();
        }
    }
}
