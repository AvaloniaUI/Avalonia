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
        class AssemblyDescriptor
        {
            public AssemblyDescriptor(Assembly assembly)
            {
                Assembly = assembly;

                if (assembly != null)
                {
                    Resources = assembly.GetManifestResourceNames()
                        .ToDictionary(n => n, n => (IAssetDescriptor)new AssemblyResourceDescriptor(assembly, n));
                    Name = assembly.GetName().Name;
                }
            }

            public Assembly Assembly { get; }
            public Dictionary<string, IAssetDescriptor> Resources { get; }
            public string Name { get; }
        }


        private static readonly Dictionary<string, AssemblyDescriptor> AssemblyNameCache
            = new Dictionary<string, AssemblyDescriptor>();

        private AssemblyDescriptor _defaultAssembly;

        public AssetLoader(Assembly assembly = null)
        {
            if (assembly == null)
                assembly = Assembly.GetEntryAssembly();
            if (assembly != null)
                _defaultAssembly = new AssemblyDescriptor(assembly);
        }

        public void SetDefaultAssembly(Assembly assembly)
        {
            _defaultAssembly = new AssemblyDescriptor(assembly);
        }

        AssemblyDescriptor GetAssembly(string name)
        {
            if (name == null)
            {
                return _defaultAssembly;
            }

            AssemblyDescriptor rv;
            if (!AssemblyNameCache.TryGetValue(name, out rv))
            {
                var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
                var match = loadedAssemblies.FirstOrDefault(a => a.GetName().Name == name);
                if (match != null)
                {
                    AssemblyNameCache[name] = rv = new AssemblyDescriptor(Assembly.Load(name));
                }
            }

            return rv;
        }

        interface IAssetDescriptor
        {
            Stream GetStream();
        }


        class AssemblyResourceDescriptor : IAssetDescriptor
        {
            private readonly Assembly _asm;
            private readonly string _name;

            public AssemblyResourceDescriptor(Assembly asm, string name)
            {
                _asm = asm;
                _name = name;
            }

            public Stream GetStream()
            {
                return _asm.GetManifestResourceStream(_name);
            }
        }
        

        IAssetDescriptor GetAsset(Uri uri)
        {
            if (!uri.IsAbsoluteUri || uri.Scheme == "resm")
            {
                var qs = uri.Query.TrimStart('?')
                    .Split(new[] {'&'}, StringSplitOptions.RemoveEmptyEntries)
                    .Select(p => p.Split('='))
                    .ToDictionary(p => p[0], p => p[1]);
                //TODO: Replace _defaultAssembly by current one (need support from OmniXAML)
                var asm = _defaultAssembly;
                if (qs.ContainsKey("assembly"))
                    asm = GetAssembly(qs["assembly"]);

                if (asm == null && _defaultAssembly == null)
                    throw new ArgumentException(
                        "No defaultAssembly, entry assembly or explicit assembly specified, don't know where to look up for the resource, try specifiyng assembly explicitly");

                IAssetDescriptor rv;

                var resourceKey = uri.AbsolutePath;

#if __IOS__
                // TODO: HACK: to get iOS up and running. Using Shared projects for resources
                // is flawed as this alters the reource key locations across platforms
                // I think we need to use Portable libraries from now on to avoid that.
                if(asm.Name.Contains("iOS"))
                {
                    resourceKey = resourceKey.Replace("TestApplication", "Perspex.iOSTestApplication");
                }
#endif

                asm.Resources.TryGetValue(resourceKey, out rv);
                return rv;
            }
            throw new ArgumentException($"Invalid uri, see https://github.com/Perspex/Perspex/issues/282#issuecomment-166982104", nameof(uri));
        }

        /// <summary>
        /// Checks if an asset with the specified URI exists.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <returns>True if the asset could be found; otherwise false.</returns>
        public bool Exists(Uri uri)
        {
            return GetAsset(uri) != null;
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
            var asset = GetAsset(uri);
            if (asset == null)
                throw new FileNotFoundException($"The resource {uri} could not be found.");
            return asset.GetStream();
        }
    }
}
