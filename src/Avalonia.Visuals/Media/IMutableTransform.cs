using System;

namespace Avalonia.Media
{
    public interface IMutableTransform : ITransform
    {
        /// <summary>
        /// Raised when the transform changes.
        /// </summary>
        event EventHandler Changed;

        /// <summary>
        /// Converts a transform to an immutable transform.
        /// </summary>
        /// <returns>The immutable transform</returns>
        ITransform ToImmutable();
    }
}
