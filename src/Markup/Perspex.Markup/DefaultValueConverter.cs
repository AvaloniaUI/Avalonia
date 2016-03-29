// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Perspex.Data;
using Perspex.Logging;
using Perspex.Utilities;

namespace Perspex.Markup
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
                var message = $"Could not convert {value} to {targetType}";
                return new BindingError(new InvalidCastException(message));
            }

            return PerspexProperty.UnsetValue;
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
