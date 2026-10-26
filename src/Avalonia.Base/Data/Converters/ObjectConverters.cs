using System.Linq;
using System.Collections.Generic;

namespace Avalonia.Data.Converters
{
    /// <summary>
    /// Provides a set of useful <see cref="IValueConverter"/>s for working with objects.
    /// </summary>
    public static class ObjectConverters
    {
        /// <summary>
        /// A value converter that returns true if the input object is a null reference.
        /// </summary>
        public static readonly IValueConverter IsNull =
            new FuncValueConverter<object?, bool>(x => x is null);

        /// <summary>
        /// A value converter that returns true if the input object is not null.
        /// </summary>
        public static readonly IValueConverter IsNotNull =
            new FuncValueConverter<object?, bool>(x => x is not null);

        /// <summary>
        /// A value converter that returns true if the input object is equal to a parameter object.
        /// </summary>
        public static readonly IValueConverter Equal =
            new FuncValueConverter<object?, object?, bool>((a, b) => a?.Equals(b) ?? b is null);
 
        /// <summary>
        /// A value converter that returns true if the input object is not equal to a parameter object.
        /// </summary>
        public static readonly IValueConverter NotEqual =
            new FuncValueConverter<object?, object?, bool>((a, b) => !a?.Equals(b) ?? b is not null);

        /// <summary>
        /// A multi-value converter that returns true if all inputs are null.
        /// </summary>
        public static readonly IMultiValueConverter AreAllNull =
            new FuncMultiValueConverter<object?, bool>(x => x.All(item => item is null));

        /// <summary>
        /// A multi-value converter that returns true if at least one input is null.
        /// </summary>
        public static readonly IMultiValueConverter AreAnyNull =
            new FuncMultiValueConverter<object?, bool>(x => x.Any(item => item is null));

        /// <summary>
        /// A multi-value converter that returns true if all inputs are equal to each other. 
        /// Null values are not considered equal to anything since they don't have Equals method.
        /// </summary>
        public static readonly IMultiValueConverter AreAllEqual =
            new FuncMultiValueConverter<object?, bool>(EqualityFunction);

        /// <summary>
        /// Helper function for AreAllEqual converter. Returns true if all inputs are equal to each other.
        /// </summary>
        /// <param name="values">The values to compare.</param>
        /// <returns>True if all values are equal, false otherwise.</returns>
        private static bool EqualityFunction(IEnumerable<object?> values)
        {
            using var enumerator = values.GetEnumerator();

            //Empty collection is considered equal.
            if (!enumerator.MoveNext())
                return true;

            //Null values are not considered equal to anything since they don't have Equals method.
            var first = enumerator.Current;
            if (first is null)
                return false;

            while (enumerator.MoveNext())
            {
                var item = enumerator.Current;
                if (item is null || !item.Equals(first))
                    return false;
            }

            return true;
        }
    }
}
