using System;
using Avalonia.Media.Immutable;

namespace Avalonia.Media
{
    /// <summary>
    /// Represents a mutable pen which can return an immutable clone of itself.
    /// </summary>
    public interface IMutablePen : IPen, IAffectsRender
    {
        /// <summary>
        /// Creates an immutable clone of the pen.
        /// </summary>
        /// <returns>The immutable clone.</returns>
        ImmutablePen ToImmutable();
    }
}
