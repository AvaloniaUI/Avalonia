// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Perspex.Platform
{
    /// <summary>
    /// Defines the platform-specific interface for <see cref="Perspex.Media.Geometry"/>.
    /// </summary>
    public interface IGeometryImpl
    {
        /// <summary>
        /// Gets the geometry's bounding rectangle.
        /// </summary>
        Rect Bounds { get; }

        /// <summary>
        /// Gets or sets a transform to apply to the geometry.
        /// </summary>
        Matrix Transform { get; set; }

        /// <summary>
        /// Gets the geometry's bounding rectangle with the specified stroke thickness.
        /// </summary>
        /// <param name="strokeThickness">The stroke thickness.</param>
        /// <returns>The bounding rectangle.</returns>
        Rect GetRenderBounds(double strokeThickness);
    }
}
