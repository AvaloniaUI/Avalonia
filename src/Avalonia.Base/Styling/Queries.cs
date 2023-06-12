using Avalonia.Platform;
using System.Collections.Generic;

namespace Avalonia.Styling
{
    internal static class Queries
    {
        public static Query IsOs(this Query? previous, string argument)
        {
            return new IsOsMediaQuery(previous, argument);
        }

        public static Query MaxHeight(this Query? previous, double argument)
        {
            return new MaxHeightMediaQuery(previous, argument);
        }

        public static Query MaxWidth(this Query? previous, double argument)
        {
            return new MaxWidthMediaQuery(previous, argument);
        }

        public static Query MinHeight(this Query? previous, double argument)
        {
            return new MinHeightMediaQuery(previous, argument);
        }

        public static Query MinWidth(this Query? previous, double argument)
        {
            return new MinWidthMediaQuery(previous, argument);
        }

        public static Query Orientation(this Query? previous, DeviceOrientation argument)
        {
            return new OrientationMediaQuery(previous, argument);
        }

        /// <summary>
        /// Returns a query which ORs queries.
        /// </summary>
        /// <param name="queries">The queries to be OR'd.</param>
        /// <returns>The query.</returns>
        public static Query Or(params Query[] queries)
        {
            return new OrQuery(queries);
        }

        /// <summary>
        /// Returns a query which ORs queries.
        /// </summary>
        /// <param name="query">The queries to be OR'd.</param>
        /// <returns>The query.</returns>
        public static Query Or(IReadOnlyList<Query> query)
        {
            return new OrQuery(query);
        }
    }
}
