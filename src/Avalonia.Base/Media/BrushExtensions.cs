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
        public static IImmutableBrush ToImmutable(this IBrush brush)
        {
            _ = brush ?? throw new ArgumentNullException(nameof(brush));

            return (brush as IMutableBrush)?.ToImmutable() ?? (IImmutableBrush)brush;
        }

        /// <summary>
        /// Converts a dash style to an immutable dash style.
        /// </summary>
        /// <param name="style">The dash style.</param>
        /// <returns>
        /// The result of calling <see cref="DashStyle.ToImmutable"/> if the style is mutable,
        /// otherwise <paramref name="style"/>.
        /// </returns>
        public static ImmutableDashStyle ToImmutable(this IDashStyle style)
        {
            _ = style ?? throw new ArgumentNullException(nameof(style));

            return style as ImmutableDashStyle ?? ((DashStyle)style).ToImmutable();
        }

        /// <summary>
        /// Converts a pen to an immutable pen.
        /// </summary>
        /// <param name="pen">The pen.</param>
        /// <returns>
        /// The result of calling <see cref="Pen.ToImmutable"/> if the brush is mutable,
        /// otherwise <paramref name="pen"/>.
        /// </returns>
        public static ImmutablePen ToImmutable(this IPen pen)
        {
            _ = pen ?? throw new ArgumentNullException(nameof(pen));

            return pen as ImmutablePen ?? ((Pen)pen).ToImmutable();
        }
    }
}
