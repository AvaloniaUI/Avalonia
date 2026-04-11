using System.Collections.Generic;
using System.Linq;

namespace Avalonia.Data.Converters
{
    /// <summary>
    /// Provides a set of utilities <see cref="IMultiValueConverter"/>s for working with multiple objects.
    /// </summary>
    public static class ObjectMultiConverter
    {
        /// <summary>
        /// A multi-value converter that returns true if all inputs are null.
        /// </summary>
        public static readonly IMultiValueConverter AreNull =
            new FuncMultiValueConverter<object?, bool>(x => x.All(item => item is null));

        /// <summary>
        /// A multi-value converter that returns true if all inputs are not null.
        /// </summary>
        public static readonly IMultiValueConverter AreNotNull =
            new FuncMultiValueConverter<object?, bool>(x => x.All(item => item is not null));

        /// <summary>
        /// A multi-value converter that returns true if all inputs are equal to each other. 
        /// Null values are not considered equal to anything since they don't have Equals method.
        /// </summary>
        public static readonly IMultiValueConverter AreAllEqual =
            new FuncMultiValueConverter<object?, bool>(EqualityFunction);

        /// <summary>
        /// A multi-value converter that returns true if not all inputs are equal to each other.
        /// </summary>
        public static readonly IMultiValueConverter AreNotEqual =
            new FuncMultiValueConverter<object?, bool>(NotEqualityFunction);

        /// <summary>
        /// Helper function for AreAllEqual converter. Returns true if all inputs are equal to each other.
        /// </summary>
        /// <param name="values">The values to compare.</param>
        /// <returns>True if all values are equal, false otherwise.</returns>
        private static bool EqualityFunction(IEnumerable<object?> values)
        {
            using var enumerator = values.GetEnumerator();

            //Empty collection is considered equal.
            if (!enumerator.MoveNext()) return true;

            //Null values are not considered equal to anything since they don't have Equals method.
            var first = enumerator.Current;
            if (first is null) return false;

            while (enumerator.MoveNext())
            {
                var item = enumerator.Current;
                if (item is null || !item.Equals(first)) return false;
            }

            return true;
        }

        /// <summary>
        /// Helper function for AreNotEqual converter. Returns true if not all inputs are equal to each other.
        /// </summary>
        /// <param name="values">The values to compare.</param>
        /// <returns>True if not all values are equal, false otherwise.</returns>
        private static bool NotEqualityFunction(IEnumerable<object?> values) => !EqualityFunction(values);
    }
}
