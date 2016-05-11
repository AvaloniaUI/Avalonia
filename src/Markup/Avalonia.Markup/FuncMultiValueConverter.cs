// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Avalonia.Markup
{
    /// <summary>
    /// A general purpose <see cref="IValueConverter"/> that uses a <see cref="Func{T1, TResult}"/>
    /// to provide the converter logic.
    /// </summary>
    /// <typeparam name="TIn">The type of the inputs.</typeparam>
    /// <typeparam name="TOut">The output type.</typeparam>
    public class FuncMultiValueConverter<TIn, TOut> : IMultiValueConverter
    {
        private readonly Func<IEnumerable<TIn>, TOut> _convert;

        /// <summary>
        /// Initializes a new instance of the <see cref="FuncValueConverter{TIn, TOut}"/> class.
        /// </summary>
        /// <param name="convert">The convert function.</param>
        public FuncMultiValueConverter(Func<IEnumerable<TIn>, TOut> convert)
        {
            _convert = convert;
        }

        /// <inheritdoc/>
        public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
        {
            var converted = values.OfType<TIn>().ToList();

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
