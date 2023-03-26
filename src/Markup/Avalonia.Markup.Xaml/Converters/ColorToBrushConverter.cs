using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Avalonia.Markup.Xaml.Converters
{
    /// <summary>
    /// Converts a <see cref="Color"/> to an <see cref="IBrush"/>.
    /// </summary>
    public class ColorToBrushConverter : IValueConverter
    {
        /// <summary>
        /// Converts a <see cref="Color"/> to an <see cref="IBrush"/> if the arguments are of the
        /// correct type.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="targetType">The target type.</param>
        /// <param name="parameter">Not used.</param>
        /// <param name="culture">Not used.</param>
        /// <returns>
        /// If <paramref name="value"/> is a <see cref="Color"/> and <paramref name="targetType"/>
        /// is <see cref="IBrush"/> then converts the color to a solid color brush.
        /// </returns>
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return Convert(value, targetType);
        }

        /// <summary>
        /// Converts an <see cref="ISolidColorBrush"/> to a <see cref="Color"/> if the arguments are of the
        /// correct type.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="targetType">The target type.</param>
        /// <param name="parameter">Not used.</param>
        /// <param name="culture">Not used.</param>
        /// <returns>
        /// If <paramref name="value"/> is an <see cref="ISolidColorBrush"/> and <paramref name="targetType"/>
        /// is <see cref="Color"/> then converts the solid color brush to a color.
        /// </returns>
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return ConvertBack(value, targetType);
        }

        /// <summary>
        /// Converts a <see cref="Color"/> to an <see cref="IBrush"/> if the arguments are of the
        /// correct type.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="targetType">The target type.</param>
        /// <returns>
        /// If <paramref name="value"/> is a <see cref="Color"/> and <paramref name="targetType"/>
        /// is <see cref="IBrush"/> then converts the color to a solid color brush.
        /// </returns>
        public static object? Convert(object? value, Type? targetType)
        {
            if (targetType == typeof(IBrush) && value is Color c)
            {
                return new ImmutableSolidColorBrush(c);
            }

            return value;
        }

        /// <summary>
        /// Converts an <see cref="ISolidColorBrush"/> to a <see cref="Color"/> if the arguments are of the
        /// correct type.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="targetType">The target type.</param>
        /// <returns>
        /// If <paramref name="value"/> is an <see cref="ISolidColorBrush"/> and <paramref name="targetType"/>
        /// is <see cref="Color"/> then converts the solid color brush to a color.
        /// </returns>
        public static object? ConvertBack(object? value, Type? targetType)
        {
            if (targetType == typeof(Color) && value is ISolidColorBrush brush)
            {
                return brush.Color;
            }

            return value;
        }
    }
}
