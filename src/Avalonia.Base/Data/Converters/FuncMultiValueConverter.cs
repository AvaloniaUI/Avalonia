using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

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
        private readonly Func<IReadOnlyList<TIn?>, TOut> _convert;

        /// <summary>
        /// Initializes a new instance of the <see cref="FuncMultiValueConverter{TIn, TOut}"/> class.
        /// </summary>
        /// <param name="convert">The convert function.</param>
        public FuncMultiValueConverter(Func<IReadOnlyList<TIn?>, TOut> convert)
        {
            _convert = convert;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FuncMultiValueConverter{TIn, TOut}"/> class.
        /// </summary>
        /// <param name="convert">The convert function.</param>
        public FuncMultiValueConverter(Func<IEnumerable<TIn?>, TOut> convert)
            : this(new Func<IReadOnlyList<TIn?>, TOut>(convert))
        {
        }

        /// <inheritdoc/>
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            var count = values.Count;
            
            // Rent an array from the pool to avoid allocation
            var rentedArray = ArrayPool<TIn?>.Shared.Rent(count);
            
            try
            {
                var validCount = 0;
                
                for (var i = 0; i < count; i++)
                {
                    var obj = values[i];
                    if (obj is TIn result)
                    {
                        rentedArray[validCount++] = result;
                    }
                    else if (Equals(obj, default(TIn)))
                    {
                        rentedArray[validCount++] = default;
                    }
                    // If the value doesn't match, we don't increment validCount
                    // This will cause the count check below to fail
                }
                
                if (validCount != count)
                {
                    return AvaloniaProperty.UnsetValue;
                }
                
                // Create a lightweight wrapper around the array segment
                var wrapper = new ArraySegmentWrapper<TIn?>(rentedArray, count);
                return _convert(wrapper);
            }
            finally
            {
                ArrayPool<TIn?>.Shared.Return(rentedArray, clearArray: true);
            }
        }
        
        /// <summary>
        /// A lightweight wrapper around an array segment to implement IReadOnlyList without additional allocation.
        /// </summary>
        private readonly struct ArraySegmentWrapper<T> : IReadOnlyList<T>
        {
            private readonly T[] _array;
            private readonly int _count;
            
            public ArraySegmentWrapper(T[] array, int count)
            {
                _array = array;
                _count = count;
            }
            
            public int Count => _count;
            
            public T this[int index]
            {
                get
                {
                    if ((uint)index >= (uint)_count)
                        throw new ArgumentOutOfRangeException(nameof(index));
                    return _array[index];
                }
            }
            
            public IEnumerator<T> GetEnumerator()
            {
                for (var i = 0; i < _count; i++)
                {
                    yield return _array[i];
                }
            }
            
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
