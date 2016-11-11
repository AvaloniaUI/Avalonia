// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Avalonia.Data;
using Avalonia.Logging;
using Avalonia.Utilities;

namespace Avalonia.Markup
{
    /// <summary>
    /// Provides a default set of value conversions for bindings that do not specify a value
    /// converter.
    /// </summary>
    public class DefaultValueConverter : IValueConverter
    {
        /// <summary>
        /// Gets an instance of a <see cref="DefaultValueConverter"/>.
        /// </summary>
        public static readonly DefaultValueConverter Instance = new DefaultValueConverter();

        /// <summary>
        /// Converts a value.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="targetType">The type of the target.</param>
        /// <param name="parameter">A user-defined parameter.</param>
        /// <param name="culture">The culture to use.</param>
        /// <returns>The converted value.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            object result;

            if (value != null && 
                (TypeUtilities.TryConvert(targetType, value, culture, out result) ||
                 TryConvertEnum(value, targetType, culture, out result)))
            {
                return result;
            }

            if (value != null)
            {
                string message;

                if (TypeUtilities.IsNumeric(targetType))
                {
                    message = $"'{value}' is not a valid number.";
                }
                else
                {
                    message = $"Could not convert '{value}' to '{targetType.Name}'.";
                }

                return new BindingNotification(new InvalidCastException(message), BindingErrorType.Error);
            }

            return AvaloniaProperty.UnsetValue;
        }

        /// <summary>
        /// Converts a value.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="targetType">The type of the target.</param>
        /// <param name="parameter">A user-defined parameter.</param>
        /// <param name="culture">The culture to use.</param>
        /// <returns>The converted value.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Convert(value, targetType, parameter, culture);
        }

        private bool TryConvertEnum(object value, Type targetType, CultureInfo cultur, out object result)
        {
            var valueTypeInfo = value.GetType().GetTypeInfo();
            var targetTypeInfo = targetType.GetTypeInfo();

            if (valueTypeInfo.IsEnum && !targetTypeInfo.IsEnum)
            {
                var enumValue = (int)value;

                if (TypeUtilities.TryCast(targetType, enumValue, out result))
                {
                    return true;
                }
            }
            else if (!valueTypeInfo.IsEnum && targetTypeInfo.IsEnum)
            {
                object intValue;

                if (TypeUtilities.TryCast(typeof(int), value, out intValue))
                {
                    result = Enum.ToObject(targetType, intValue);
                    return true;
                }
            }

            result = null;
            return false;
        }
    }
}
