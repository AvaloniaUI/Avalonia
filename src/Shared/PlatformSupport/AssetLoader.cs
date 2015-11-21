// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Perspex.Platform;

namespace Perspex.Shared.PlatformSupport
{
    /// <summary>
    /// Loads assets compiled into the application binary.
    /// </summary>
    public class AssetLoader : IAssetLoader
    {
        private static readonly Dictionary<string, Assembly> AssemblyNameCache
            = new Dictionary<string, Assembly>();

        private Assembly _defaultAssembly;

        public AssetLoader(Assembly assembly = null)
        {
            _defaultAssembly = assembly;
        }

        static Assembly GetAssembly(string name)
        {
            Assembly rv;
            if (!AssemblyNameCache.TryGetValue(name, out rv))
                AssemblyNameCache[name] = rv =
                    AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == name)
                    ?? Assembly.Load(name);
            return rv;
        }

        /// <summary>
        /// Checks if an asset with the specified URI exists.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <returns>True if the asset could be found; otherwise false.</returns>
        public bool Exists(Uri uri)
        {
            var parts = uri.AbsolutePath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            var asm = parts.Length == 1 ? (_defaultAssembly ?? Assembly.GetEntryAssembly()) : GetAssembly(parts[0]);
            var typeName = parts[parts.Length == 1 ? 0 : 1];
            var rv = asm.GetManifestResourceStream(typeName);
            return rv != null;
        }

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
            var parts = uri.AbsolutePath.Split(new[] {'/'}, StringSplitOptions.RemoveEmptyEntries);
            var asm = parts.Length == 1 ? (_defaultAssembly ?? Assembly.GetEntryAssembly()) : GetAssembly(parts[0]);
            var typeName = parts[parts.Length == 1 ? 0 : 1];
            var rv = asm.GetManifestResourceStream(typeName);
            if (rv == null)
            {
#if DEBUG
                var names = asm.GetManifestResourceNames().ToList();
#endif
                throw new FileNotFoundException($"The resource {uri} could not be found.");
            }
            return rv;
        }
    }
}
