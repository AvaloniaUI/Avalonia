// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.IO;
using System.Reflection;

namespace Avalonia.Platform
{
    /// <summary>
    /// Loads assets compiled into the application binary.
    /// </summary>
    public interface IAssetLoader
    {
        /// <summary>
        /// We need a way to override the default assembly selected by the host platform
        /// because right now it is selecting the wrong one for PCL based Apps. The 
        /// AssetLoader needs a refactor cause right now it lives in 3+ platforms which 
        /// can all be loaded on Windows. 
        /// </summary>
        /// <param name="asm"></param>
        void SetDefaultAssembly(Assembly asm);

        /// <summary>
        /// Checks if an asset with the specified URI exists.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="baseUri">
        /// A base URI to use if <paramref name="uri"/> is relative.
        /// </param>
        /// <returns>True if the asset could be found; otherwise false.</returns>
        bool Exists(Uri uri, Uri baseUri = null);

        /// <summary>
        /// Opens the resource with the requested URI.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="baseUri">
        /// A base URI to use if <paramref name="uri"/> is relative.
        /// </param>
        /// <returns>A stream containing the resource contents.</returns>
        /// <exception cref="FileNotFoundException">
        /// The resource was not found.
        /// </exception>
        Stream Open(Uri uri, Uri baseUri = null);
    }
}
