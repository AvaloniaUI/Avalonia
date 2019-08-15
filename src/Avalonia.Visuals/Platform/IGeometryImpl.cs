// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Media;

namespace Avalonia.Platform
{
    /// <summary>
    /// Defines the platform-specific interface for a <see cref="Geometry"/>.
    /// </summary>
    public interface IGeometryImpl
    {
        /// <summary>
        /// Gets the geometry's bounding rectangle.
        /// </summary>
        Rect Bounds { get; }

        /// <summary>
        /// Gets the geometry's bounding rectangle with the specified pen.
        /// </summary>
        /// <param name="pen">The pen to use. May be null.</param>
        /// <returns>The bounding rectangle.</returns>
        Rect GetRenderBounds(IPen pen);

        /// <summary>
        /// Indicates whether the geometry's fill contains the specified point.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns><c>true</c> if the geometry contains the point; otherwise, <c>false</c>.</returns>
        bool FillContains(Point point);

        /// <summary>
        /// Intersects the geometry with another geometry.
        /// </summary>
        /// <param name="geometry">The other geometry.</param>
        /// <returns>A new <see cref="IGeometryImpl"/> representing the intersection.</returns>
        IGeometryImpl Intersect(IGeometryImpl geometry);

        /// <summary>
        /// Indicates whether the geometry's stroke contains the specified point.
        /// </summary>
        /// <param name="pen">The stroke to use.</param>
        /// <param name="point">The point.</param>
        /// <returns><c>true</c> if the geometry contains the point; otherwise, <c>false</c>.</returns>
        bool StrokeContains(IPen pen, Point point);

        /// <summary>
        /// Makes a clone of the geometry with the specified transform.
        /// </summary>
        /// <param name="transform">The transform.</param>
        /// <returns>The cloned geometry.</returns>
        ITransformedGeometryImpl WithTransform(Matrix transform);
    }
}
