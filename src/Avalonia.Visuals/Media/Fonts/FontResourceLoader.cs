// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Avalonia.Platform;

namespace Avalonia.Media.Fonts
{
    public class FontResourceLoader : IFontResourceLoader
    {
        private static readonly Dictionary<string, AssemblyDescriptor> s_assemblyNameCache
            = new Dictionary<string, AssemblyDescriptor>();

        private readonly AssemblyDescriptor _defaultAssembly;

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

        public IEnumerable<FontResource> GetFontResources(FontFamilyKey fontFamilyKey)
        {
            return fontFamilyKey.FileName != null
                ? GetFontResourcesByFileName(fontFamilyKey.Location, fontFamilyKey.FileName)
                : GetFontResourcesByLocation(fontFamilyKey.Location);
        }

        private IEnumerable<FontResource> GetFontResourcesByLocation(Uri location)
        {
            var assembly = GetAssembly(location);

            var locationPath = GetLocationPath(location);

            var matchingResources = assembly.Resources.Where(x => x.Contains(locationPath));

            return matchingResources.Select(x => new FontResource(GetResourceUri(x, assembly.Name)));
        }

        private IEnumerable<FontResource> GetFontResourcesByFileName(Uri location, string fileName)
        {
            var assembly = GetAssembly(location);

            var compareTo = GetLocationPath(location) + "." + fileName.Split('*').First();

            var matchingResources = assembly.Resources.Where(x => x.Contains(compareTo));

            return matchingResources.Select(x => new FontResource(GetResourceUri(x, assembly.Name)));
        }

        private static Uri GetResourceUri(string path, string assemblyName)
        {
            return new Uri("resm:" + path + "?assembly=" + assemblyName);
        }

        private static string GetLocationPath(Uri uri)
        {
            if (uri.Scheme == "resm") return uri.AbsolutePath;

            var path = uri.AbsolutePath.Replace("/", ".");

            return path;
        }

        private AssemblyDescriptor GetAssembly(Uri uri)
        {
            if (uri == null) return null;

            var parameters = ParseParameters(uri);

            return parameters.TryGetValue("assembly", out var assemblyName) ? GetAssembly(assemblyName) : null;
        }

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

            public string Name { get; }
            public Assembly Assembly { get; }
            public List<string> Resources { get; }
        }
    }
}