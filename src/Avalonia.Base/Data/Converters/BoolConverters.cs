using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;

namespace Avalonia.Data.Converters
{
    /// <summary>
    /// Provides a set of useful <see cref="IValueConverter"/>s for working with bool values.
    /// </summary>
    public static class BoolConverters
    {
        /// <summary>
        /// A multi-value converter that returns true if all inputs are true.
        /// </summary>
        public static readonly IMultiValueConverter And =
            new FuncMultiValueConverter<bool, bool>(x => x.All(y => y));

        /// <summary>
        /// A multi-value converter that returns true if any of the inputs is true.
        /// </summary>
        public static readonly IMultiValueConverter Or =
            new FuncMultiValueConverter<bool, bool>(x => x.Any(y => y));

        /// <summary>
        /// A value converter that returns true when input is false and false when input is true.
        /// </summary>
        public static readonly IValueConverter Not =
            new NotConverter();

        private class NotConverter : IValueConverter
        {
            public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            {
                if (value is bool val)
                {
                    return !val;
                }
                return AvaloniaProperty.UnsetValue;
            }

            public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            {
                if (value is bool val)
                {
                    return !val;
                }
                return AvaloniaProperty.UnsetValue;
            }
        }
    }
}
