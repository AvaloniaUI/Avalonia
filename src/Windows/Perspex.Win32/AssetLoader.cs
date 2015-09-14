// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;
using Perspex.Platform;

namespace Perspex.Win32
{
    /// <summary>
    /// Loads assets compiled into the application binary.
    /// </summary>
    public class AssetLoader : IAssetLoader
    {
        /// <summary>
        /// Opens the resource with the requested URI.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <returns>A stream containing the resource contents.</returns>
        /// <exception cref="FileNotFoundException">
        /// The resource was not found.
        /// </exception>
        public Stream Open(Uri uri)
        {
            var assembly = Assembly.GetEntryAssembly();
            var resourceName = assembly.GetName().Name + ".g";
            var manager = new ResourceManager(resourceName, assembly);

            using (var resourceSet = manager.GetResourceSet(CultureInfo.CurrentCulture, true, true))
            {
                var stream = (Stream)resourceSet.GetObject(uri.ToString(), true);

                if (stream == null)
                {
                    throw new FileNotFoundException($"The requested asset could not be found: {uri}");
                }

                return stream;
            }
        }
    }
}
