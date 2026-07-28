using System;
using System.Globalization;
using Avalonia.Utilities;

namespace Avalonia.Data.Converters
{
    /// <summary>
    /// A general purpose <see cref="IValueConverter"/> that uses a <see cref="Func{TIn, TResult}"/>
    /// to provide the converter logic.
    /// </summary>
    /// <typeparam name="TIn">The input type.</typeparam>
    /// <typeparam name="TOut">The output type.</typeparam>
    public class FuncValueConverter<TIn, TOut> : IValueConverter
    {
        private readonly Func<TIn?, TOut> _convert;
        private readonly Func<TOut?, TIn>? _convertBack;

        /// <summary>
        /// Initializes a new instance of the <see cref="FuncValueConverter{TIn, TOut}"/> class.
        /// </summary>
        /// <param name="convert">The function to convert TIn to TOut.</param>
        public FuncValueConverter(Func<TIn?, TOut> convert)
        {
            _convert = convert;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FuncValueConverter{TIn, TOut}"/> class.
        /// </summary>
        /// <param name="convert">The function to convert TIn to TOut.</param>
        /// <param name="convertBack">The function to convert TOut back to In.</param>
        public FuncValueConverter(Func<TIn?, TOut> convert, Func<TOut?, TIn>? convertBack)
        {
            _convert = convert;
            _convertBack = convertBack;
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
            if (_convertBack == null)
            {
                throw new NotImplementedException();
            }

            if (TypeUtilities.CanCast<TOut>(value))
            {
                return _convertBack((TOut?)value);
            }
            else
            {
                return AvaloniaProperty.UnsetValue;
            }
        }
    }

    /// <summary>
    /// A general purpose <see cref="IValueConverter"/> that uses a <see cref="Func{TIn, TParam, TOut}"/>
    /// to provide the converter logic.
    /// </summary>
    /// <typeparam name="TIn">The input type.</typeparam>
    /// <typeparam name="TParam">The param type.</typeparam>
    /// <typeparam name="TOut">The output type.</typeparam>
    public class FuncValueConverter<TIn, TParam, TOut> : IValueConverter
    {
        private readonly Func<TIn?, TParam?, TOut> _convert;
        private readonly Func<TOut?, TParam?, TIn>? _convertBack;

        /// <summary>
        /// Initializes a new instance of the <see cref="FuncValueConverter{TIn, TParam, TOut}"/> class.
        /// </summary>
        /// <param name="convert">The function to convert TIn to TOut.</param>
        public FuncValueConverter(Func<TIn?, TParam?, TOut> convert)
        {
            _convert = convert;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FuncValueConverter{TIn, TParam, TOut}"/> class.
        /// </summary>
        /// <param name="convert">The function to convert TIn to TOut.</param>
        /// <param name="convertBack">The function to convert TOut back to In.</param>
        public FuncValueConverter(Func<TIn?, TParam?, TOut> convert, Func<TOut?, TParam?, TIn>? convertBack = null)
        {
            _convert = convert;
            _convertBack = convertBack;
        }

        /// <inheritdoc/>
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (TypeUtilities.CanCast<TIn>(value) && TypeUtilities.CanCast<TParam>(parameter))
            {
                return _convert((TIn?)value, (TParam?)parameter);
            }
            else
            {
                return AvaloniaProperty.UnsetValue;
            }
        }

        /// <inheritdoc/>
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (_convertBack == null)
            {
                throw new NotImplementedException();
            }

            if (TypeUtilities.CanCast<TOut>(value) && TypeUtilities.CanCast<TParam>(parameter))
            {
                return _convertBack((TOut?)value, (TParam?)parameter);
            }
            else
            {
                return AvaloniaProperty.UnsetValue;
            }
        }
    }
}
