using System;
using Avalonia.Media.Immutable;

namespace Avalonia.Media
{
    /// <summary>
    /// Extension methods for brush classes.
    /// </summary>
    public static class BrushExtensions
    {
        /// <summary>
        /// Converts a brush to an immutable brush.
        /// </summary>
        /// <param name="brush">The brush.</param>
        /// <returns>
        /// The result of calling <see cref="IMutableBrush.ToImmutable"/> if the brush is mutable,
        /// otherwise <paramref name="brush"/>.
        /// </returns>
        public static IBrush ToImmutable(this IBrush brush)
        {
            Contract.Requires<ArgumentNullException>(brush != null);

            return (brush as IMutableBrush)?.ToImmutable() ?? brush;
        }

        /// <summary>
        /// Converts a pen to a pen with an immutable brush
        /// </summary>
        /// <param name="pen">The pen.</param>
        /// <returns>
        /// A copy of the pen with an immutable brush, or <paramref name="pen"/> if the pen's brush
        /// is already immutable or null.
        /// </returns>
        public static ImmutablePen ToImmutable(this IPen pen)
        {
            Contract.Requires<ArgumentNullException>(pen != null);

            return pen as ImmutablePen ?? ((IMutablePen)pen).ToImmutable();
        }
    }
}
