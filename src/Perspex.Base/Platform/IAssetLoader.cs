// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.IO;

namespace Perspex.Platform
{
    /// <summary>
    /// Loads assets compiled into the application binary.
    /// </summary>
    public interface IAssetLoader
    {
        /// <summary>
        /// Checks if an asset with the specified URI exists.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <returns>True if the asset could be found; otherwise false.</returns>
        bool Exists(Uri uri);

        /// <summary>
        /// Opens the resource with the requested URI.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <returns>A stream containing the resource contents.</returns>
        /// <exception cref="FileNotFoundException">
        /// The resource was not found.
        /// </exception>
        Stream Open(Uri uri);
    }
}
