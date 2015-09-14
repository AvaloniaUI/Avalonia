// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Perspex.Platform
{
    /// <summary>
    /// Defines the platform-specific interface for a <see cref="Perspex.Media.StreamGeometry"/>.
    /// </summary>
    public interface IStreamGeometryImpl : IGeometryImpl
    {
        /// <summary>
        /// Clones the geometry.
        /// </summary>
        /// <returns>A cloned geometry.</returns>
        IStreamGeometryImpl Clone();

        /// <summary>
        /// Opens the geometry to start defining it.
        /// </summary>
        /// <returns>
        /// An <see cref="IStreamGeometryContextImpl"/> which can be used to define the geometry.
        /// </returns>
        IStreamGeometryContextImpl Open();
    }
}
