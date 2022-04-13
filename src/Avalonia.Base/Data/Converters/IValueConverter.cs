using System;
using System.Globalization;

namespace Avalonia.Data.Converters
{
    /// <summary>
    /// Converts a binding value.
    /// </summary>
    public interface IValueConverter
    {
        /// <summary>
        /// Converts a value.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="targetType">The type of the target.</param>
        /// <param name="parameter">A user-defined parameter.</param>
        /// <param name="culture">The culture to use.</param>
        /// <returns>The converted value.</returns>
        /// <remarks>
        /// This method should not throw exceptions. If the value is not convertible, return
        /// a <see cref="BindingNotification"/> in an error state. Any exceptions thrown will be
        /// treated as an application exception.
        /// </remarks>
        object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture);

        /// <summary>
        /// Converts a value.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="targetType">The type of the target.</param>
        /// <param name="parameter">A user-defined parameter.</param>
        /// <param name="culture">The culture to use.</param>
        /// <returns>The converted value.</returns>
        /// <remarks>
        /// This method should not throw exceptions. If the value is not convertible, return
        /// a <see cref="BindingNotification"/> in an error state. Any exceptions thrown will be
        /// treated as an application exception.
        /// </remarks>
        object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture);
    }
}
