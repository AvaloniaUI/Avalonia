// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
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
        /// <param name="assembly"></param>
        void SetDefaultAssembly(Assembly assembly);

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
        /// Opens the asset with the requested URI.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="baseUri">
        /// A base URI to use if <paramref name="uri"/> is relative.
        /// </param>
        /// <returns>A stream containing the asset contents.</returns>
        /// <exception cref="FileNotFoundException">
        /// The asset could not be found.
        /// </exception>
        Stream Open(Uri uri, Uri baseUri = null);

        /// <summary>
        /// Opens the asset with the requested URI and returns the asset stream and the
        /// assembly containing the asset.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="baseUri">
        /// A base URI to use if <paramref name="uri"/> is relative.
        /// </param>
        /// <returns>
        /// The stream containing the asset contents together with the assembly.
        /// </returns>
        /// <exception cref="FileNotFoundException">
        /// The asset could not be found.
        /// </exception>
        (Stream stream, Assembly assembly) OpenAndGetAssembly(Uri uri, Uri baseUri = null);

        /// <summary>
        /// Extracts assembly information from URI
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="baseUri">
        /// A base URI to use if <paramref name="uri"/> is relative.
        /// </param>
        /// <returns>Assembly associated with the Uri</returns>
        Assembly GetAssembly(Uri uri, Uri baseUri = null);

        /// <summary>
        /// Gets all assets of a folder and subfolders that match specified uri.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="baseUri">The base URI.</param>
        /// <returns>All matching assets as a tuple of the absolute path to the asset and the assembly containing the asset</returns>
        IEnumerable<Uri> GetAssets(Uri uri, Uri baseUri);
    }
}
