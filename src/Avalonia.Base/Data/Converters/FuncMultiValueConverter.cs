using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Avalonia.Data.Converters
{
    /// <summary>
    /// A general purpose <see cref="IValueConverter"/> that uses a <see cref="Func{T1, TResult}"/>
    /// to provide the converter logic.
    /// </summary>
    /// <typeparam name="TIn">The type of the inputs.</typeparam>
    /// <typeparam name="TOut">The output type.</typeparam>
    public class FuncMultiValueConverter<TIn, TOut> : IMultiValueConverter
    {
        private readonly Func<IEnumerable<TIn?>, TOut> _convert;

        /// <summary>
        /// Initializes a new instance of the <see cref="FuncValueConverter{TIn, TOut}"/> class.
        /// </summary>
        /// <param name="convert">The convert function.</param>
        public FuncMultiValueConverter(Func<IEnumerable<TIn?>, TOut> convert)
        {
            _convert = convert;
        }

        /// <inheritdoc/>
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            //standard OfType skip null values, even they are valid for the Type
            static IEnumerable<TIn?> OfTypeWithDefaultSupport(IList<object?> list)
            {
                foreach (var obj in list)
                {
                    if (obj is TIn result)
                    {
                        yield return result;
                    }
                    else if (Equals(obj, default(TIn)))
                    {
                        yield return default;
                    }
                }
            }

            var converted = OfTypeWithDefaultSupport(values).ToList();

            if (converted.Count == values.Count)
            {
                return _convert(converted);
            }
            else
            {
                return AvaloniaProperty.UnsetValue;
            }
        }
    }
}
