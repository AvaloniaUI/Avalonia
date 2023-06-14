using Avalonia.Platform;
using System.Collections.Generic;

namespace Avalonia.Styling
{
    internal static class Queries
    {
        public static Query Platform(this Query? previous, string argument)
        {
            return new PlatformMediaQuery(previous, argument);
        }

        public static Query Orientation(this Query? previous, MediaOrientation argument)
        {
            return new OrientationMediaQuery(previous, argument);
        }

        public static Query Width(this Query? previous, QueryComparisonOperator leftOperator, double left, QueryComparisonOperator rightOperator, double right)
        {
            return new WidthMediaQuery(previous, leftOperator, left, rightOperator, right);
        }

        public static Query Height(this Query? previous, QueryComparisonOperator leftOperator, double left, QueryComparisonOperator rightOperator, double right)
        {
            return new HeightMediaQuery(previous, leftOperator, left, rightOperator, right);
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

        /// <summary>
        /// Returns a query which ANDs queries.
        /// </summary>
        /// <param name="queries">The queries to be AND'd.</param>
        /// <returns>The query.</returns>
        public static Query And(params Query[] queries)
        {
            return new AndQuery(queries);
        }

        /// <summary>
        /// Returns a query which ANDs queries.
        /// </summary>
        /// <param name="query">The queries to be AND'd.</param>
        /// <returns>The query.</returns>
        public static Query And(IReadOnlyList<Query> query)
        {
            return new AndQuery(query);
        }
    }
}
