// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Avalonia.Platform;

namespace Avalonia.Media.Fonts
{
    /// <inheritdoc />
    /// <summary>
    /// Implementation of <see cref="T:Avalonia.Media.Fonts.IFontResourceLoader" />
    /// </summary>
    public class FontResourceLoader : IFontResourceLoader
    {
        private static readonly Dictionary<string, AssemblyDescriptor> s_assemblyNameCache
            = new Dictionary<string, AssemblyDescriptor>();

        private readonly AssemblyDescriptor _defaultAssembly;

        /// <summary>
        /// Initializes a new instance of the <see cref="FontResourceLoader"/> class.
        /// </summary>
        /// <param name="assembly">The default assembly.</param>
        public FontResourceLoader(Assembly assembly = null)
        {
            if (assembly == null)
            {
                assembly = Assembly.GetEntryAssembly();
            }
            if (assembly != null)
            {
                _defaultAssembly = new AssemblyDescriptor(assembly);
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// Returns a quanity of <see cref="T:Avalonia.Media.Fonts.FontResource" /> that belongs to a given <see cref="T:Avalonia.Media.Fonts.FontFamilyKey" />
        /// </summary>
        /// <param name="fontFamilyKey"></param>
        /// <returns></returns>
        public IEnumerable<FontResource> GetFontResources(FontFamilyKey fontFamilyKey)
        {
            return fontFamilyKey.FileName != null
                ? GetFontResourcesByFileName(fontFamilyKey.Location, fontFamilyKey.FileName)
                : GetFontResourcesByLocation(fontFamilyKey.Location);
        }

        /// <summary>
        /// Searches for font resources at a given location and returns a quanity of found resources
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        private IEnumerable<FontResource> GetFontResourcesByLocation(Uri location)
        {
            var assembly = GetAssembly(location);

            if (assembly == null) return Enumerable.Empty<FontResource>();

            var locationPath = location.AbsolutePath;

            var matchingResources = assembly.Resources.Where(x => x.Contains(locationPath));

            return matchingResources.Select(x => new FontResource(GetResourceUri(x, assembly.Name)));
        }

        /// <summary>
        /// Searches for font resources at a given location and only accepts resources that fit to a given filename expression.
        /// <para>Filenames can target multible files with * wildcard. For example "FontFile*.ttf"</para>
        /// </summary>
        /// <param name="location"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private IEnumerable<FontResource> GetFontResourcesByFileName(Uri location, string fileName)
        {
            var assembly = GetAssembly(location);

            if (assembly == null) return Enumerable.Empty<FontResource>();

            var compareTo = location.AbsolutePath + "." + fileName.Split('*').First();

            var matchingResources = assembly.Resources.Where(x => x.Contains(compareTo));

            return matchingResources.Select(x => new FontResource(GetResourceUri(x, assembly.Name)));
        }

        /// <summary>
        /// Returns a valid resource <see cref="Uri"/> that follows the resm scheme
        /// </summary>
        /// <param name="path"></param>
        /// <param name="assemblyName"></param>
        /// <returns></returns>
        private static Uri GetResourceUri(string path, string assemblyName)
        {
            return new Uri("resm:" + path + "?assembly=" + assemblyName);
        }

        /// <summary>
        /// Extracts a <see cref="AssemblyDescriptor"/> from a given <see cref="Uri"/>
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        private AssemblyDescriptor GetAssembly(Uri uri)
        {
            if (uri == null) return null;

            var parameters = ParseParameters(uri);

            return parameters.TryGetValue("assembly", out var assemblyName) ? GetAssembly(assemblyName) : _defaultAssembly;
        }

        /// <summary>
        /// Returns a <see cref="AssemblyDescriptor"/> that is identified by a given name.
        /// <para>
        /// If name is <value>null</value> the default assembly is used.
        /// </para>
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private AssemblyDescriptor GetAssembly(string name)
        {
            if (name == null)
            {
                return _defaultAssembly;
            }

            if (!s_assemblyNameCache.TryGetValue(name, out var rv))
            {
                var loadedAssemblies = AvaloniaLocator.Current.GetService<IRuntimePlatform>().GetLoadedAssemblies();
                var match = loadedAssemblies.FirstOrDefault(a => a.GetName().Name == name);
                if (match != null)
                {
                    s_assemblyNameCache[name] = rv = new AssemblyDescriptor(match);
                }
                else
                {
                    // iOS does not support loading assemblies dynamically!
                    //
#if NETCOREAPP1_0
                    s_assemblyNameCache[name] = rv = new AssemblyDescriptor(Assembly.Load(new AssemblyName(name)));
#elif __IOS__
                    throw new InvalidOperationException(
                        $"Assembly {name} needs to be referenced and explicitly loaded before loading resources");
#else
                    s_assemblyNameCache[name] = rv = new AssemblyDescriptor(Assembly.Load(name));
#endif
                }
            }

            return rv;
        }

        /// <summary>
        /// Parses the parameters.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <returns></returns>
        private static Dictionary<string, string> ParseParameters(Uri uri)
        {
            return uri.Query.TrimStart('?')
                .Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Split('='))
                .ToDictionary(p => p[0], p => p[1]);
        }

        private class AssemblyDescriptor
        {
            public AssemblyDescriptor(Assembly assembly)
            {
                Assembly = assembly;

                if (Assembly == null) return;

                Resources = assembly.GetManifestResourceNames().ToList();

                Name = Assembly.GetName().Name;
            }

            /// <summary>
            /// Gets the name.
            /// </summary>
            /// <value>
            /// The name.
            /// </value>
            public string Name { get; }

            /// <summary>
            /// Gets the assembly.
            /// </summary>
            /// <value>
            /// The assembly.
            /// </value>
            public Assembly Assembly { get; }

            /// <summary>
            /// Gets the resources.
            /// </summary>
            /// <value>
            /// The resources.
            /// </value>
            public List<string> Resources { get; }
        }
    }
}