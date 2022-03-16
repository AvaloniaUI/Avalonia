using System;
using Avalonia.Media.Immutable;

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
        /// The result of calling <see cref="Transform.ToImmutable"/> if the transform is mutable,
        /// otherwise <paramref name="transform"/>.
        /// </returns>
        public static ImmutableTransform ToImmutable(this ITransform transform)
        {
            _ = transform ?? throw new ArgumentNullException(nameof(transform));

            return (transform as Transform)?.ToImmutable() ?? new ImmutableTransform(transform.Value);
        }

        /// <summary>
        /// Merge two transforms to an one transform.
        /// </summary>
        /// <param name="srcTransform">The original transform.</param>
        /// <param name="additionalTransform">The additional transform to be added.</param>
        /// <returns>
        /// One ITransform that includes both.
        /// </returns>
        public static ITransform MergeTransforms(this ITransform srcTransform, ITransform additionalTransform)
        {
            TransformGroup groupTransform = new TransformGroup();

            groupTransform.Children.Add(srcTransform);
            groupTransform.Children.Add(additionalTransform);

            return groupTransform;
        }
    }
}
