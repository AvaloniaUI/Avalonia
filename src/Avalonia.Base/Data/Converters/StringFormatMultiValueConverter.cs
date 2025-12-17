using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;

namespace Avalonia.Data.Converters
{
    /// <summary>
    /// A multi-value converter which calls <see cref="string.Format(string, object)"/>
    /// </summary>
    public class StringFormatMultiValueConverter : IMultiValueConverter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StringFormatMultiValueConverter"/> class.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="inner">
        /// An optional inner converter to be called before the format takes place.
        /// </param>
        public StringFormatMultiValueConverter(string format, IMultiValueConverter? inner)
        {
            Format = format ?? throw new ArgumentNullException(nameof(format));
            Inner = inner;
        }

        /// <summary>
        /// Gets an inner value converter which will be called before the string format takes place.
        /// </summary>
        public IMultiValueConverter? Inner { get; }

        /// <summary>
        /// Gets the format string.
        /// </summary>
        public string Format { get; }

        /// <inheritdoc/>
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (Inner != null)
            {
                return string.Format(culture, Format, Inner.Convert(values, targetType, parameter, culture));
            }

            // Use ArrayPool to avoid allocation when converting IList to array for string.Format
            var count = values.Count;
            var rentedArray = ArrayPool<object?>.Shared.Rent(count);
            try
            {
                for (var i = 0; i < count; i++)
                {
                    rentedArray[i] = values[i];
                }
                return string.Format(culture, Format, rentedArray);
            }
            finally
            {
                ArrayPool<object?>.Shared.Return(rentedArray, clearArray: true);
            }
        }
    }
}
