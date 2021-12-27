using System;
using System.Globalization;
using Avalonia.Utilities;

namespace Avalonia.Data.Converters
{
    /// <summary>
    /// A general purpose <see cref="IValueConverter"/> that uses a <see cref="Func{T1, TResult}"/>
    /// to provide the converter logic.
    /// </summary>
    /// <typeparam name="TIn">The input type.</typeparam>
    /// <typeparam name="TOut">The output type.</typeparam>
    public class FuncValueConverter<TIn, TOut> : IValueConverter
    {
        private readonly Func<TIn?, TOut> _convert;

        /// <summary>
        /// Initializes a new instance of the <see cref="FuncValueConverter{TIn, TOut}"/> class.
        /// </summary>
        /// <param name="convert">The convert function.</param>
        public FuncValueConverter(Func<TIn?, TOut> convert)
        {
            _convert = convert;
        }

        /// <inheritdoc/>
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (TypeUtilities.CanCast<TIn>(value))
            {
                return _convert((TIn?)value);
            }
            else
            {
                return AvaloniaProperty.UnsetValue;
            }
        }

        /// <inheritdoc/>
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
