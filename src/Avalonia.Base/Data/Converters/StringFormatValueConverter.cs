using System;
using System.Globalization;

namespace Avalonia.Data.Converters
{
    /// <summary>
    /// A value converter which calls <see cref="string.Format(string, object)"/>
    /// </summary>
    public class StringFormatValueConverter : IValueConverter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StringFormatValueConverter"/> class.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="inner">
        /// An optional inner converter to be called before the format takes place.
        /// </param>
        public StringFormatValueConverter(string format, IValueConverter? inner)
        {
            Format = format ?? throw new ArgumentNullException(nameof(format));
            Inner = inner;
        }

        /// <summary>
        /// Gets an inner value converter which will be called before the string format takes place.
        /// </summary>
        public IValueConverter? Inner { get; }

        /// <summary>
        /// Gets the format string.
        /// </summary>
        public string Format { get; }

        /// <inheritdoc/>
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            value = Inner?.Convert(value, targetType, parameter, culture) ?? value;
            var format = Format!;
            if (!format.Contains('{'))
            {
                format = $"{{0:{format}}}";
            }
            return string.Format(culture, format, value);
        }

        /// <inheritdoc/>
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException("Two way bindings are not supported with a string format");
        }
    }
}
