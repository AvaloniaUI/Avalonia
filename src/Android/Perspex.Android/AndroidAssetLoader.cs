// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Android.Platform.Specific;
using Perspex.Platform;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Perspex.Android.PlatformSupport
{
    /// <summary>
    ///     Loads assets compiled into the application binary.
    /// </summary>
    public class AndroidAssetLoader : IAssetLoader
    {
        internal Assembly DefaultAssetAssembly { get; set; }
        internal string DefaultResourcePrefix { get; set; }

        public AndroidAssetLoader()
        {
            //Assembly.GetEntryAssembly() not working on mono/xamarin for android!!!
            //may be this is better way to have at least the application assembly as default one
            DefaultAssetAssembly = PerspexLocator.Current.GetService<IAndroidActivity>().Activity.GetType().Assembly;
        }

        private static readonly Dictionary<string, Assembly> AssemblyNameCache = new Dictionary<string, Assembly>();

        /// <summary>
        ///     Checks if an asset with the specified URI exists.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <returns>True if the asset could be found; otherwise false.</returns>
        public bool Exists(Uri uri)
        {
            return OpenInternal(uri, false) != null;
        }

        /// <summary>
        ///     Opens the resource with the requested URI.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <returns>A stream containing the resource contents.</returns>
        /// <exception cref="FileNotFoundException">
        ///     The resource was not found.
        /// </exception>
        public Stream Open(Uri uri)
        {
            return OpenInternal(uri, true);
        }

        private Stream OpenInternal(Uri uri, bool throwIfNotSuccess)
        {
            //uri.AbsolutePath is NotFiniteNumberException escaped adn can cause problems in assembly names with spaces
            bool isAbsolute = uri.IsAbsoluteUri;
            var parts = (isAbsolute ? uri.LocalPath : uri.OriginalString).Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            var asm = parts.Length == 1 ? DefaultAssetAssembly : GetAssembly(parts[0]);
            var typeName = parts[parts.Length == 1 ? 0 : 1];
            if (typeName != null && !isAbsolute && !typeName.StartsWith(DefaultResourcePrefix))
            {
                typeName = DefaultResourcePrefix + typeName;
            }

            var rv = asm.GetManifestResourceStream(typeName);
            if (rv == null && throwIfNotSuccess)
                throw new FileNotFoundException($"The resource {uri} could not be found.");
            return rv;
        }

        private static Assembly GetAssembly(string name)
        {
            Assembly rv;
            if (!AssemblyNameCache.TryGetValue(name, out rv))
                AssemblyNameCache[name] = rv =
                    AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == name)
                    ?? Assembly.Load(name);
            return rv;
        }
    }
}