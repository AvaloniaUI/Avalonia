﻿// -----------------------------------------------------------------------
// <copyright file="IGeometryImpl.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

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
