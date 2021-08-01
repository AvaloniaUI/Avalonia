using System;

namespace Avalonia.Media
{
    /// <summary>
    /// Extension methods for transform classes.
    /// </summary>
    public static class TransformExtensions
    {
        /// <summary>
        /// Converts a transform to an immutable transform.
        /// </summary>
        /// <param name="transform">The transform.</param>
        /// <returns>
        /// The result of calling <see cref="IMutableTransform.ToImmutable"/> if the transform is mutable,
        /// otherwise <paramref name="transform"/>.
        /// </returns>
        public static ITransform ToImmutable(this ITransform transform)
        {
            Contract.Requires<ArgumentNullException>(transform != null);

            return (transform as IMutableTransform)?.ToImmutable() ?? transform;
        }
    }
}
