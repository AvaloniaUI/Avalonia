// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Globalization;
using System.Windows.Input;
using Avalonia.Utilities;

namespace Avalonia.Data.Converters
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
            if (value == null)
            {
                return targetType.IsValueType ? AvaloniaProperty.UnsetValue : null;
            }

            if (typeof(ICommand).IsAssignableFrom(targetType) && value is Delegate d && d.Method.GetParameters().Length <= 1)
            {
                return new AlwaysEnabledDelegateCommand(d);
            }

            if (TypeUtilities.TryConvert(targetType, value, culture, out object result))
            {
                return result;
            }

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
    }
}
