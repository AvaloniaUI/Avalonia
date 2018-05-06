// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Avalonia.Platform;

namespace Avalonia.Media
{
    public class FontFamily
    {
        public FontFamily(string name = "Courier New")
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public FontFamily(string name, Uri source) : this(name)
        {
            Key = new FontFamilyKey(source);
        }

        public string Name { get; }

        public FontFamilyKey Key { get; }

        public override string ToString()
        {
            if (Key != null)
            {
                return Key + "#" + Name;
            }

            return Name;
        }
    }

    public class FontFamilyKey
    {
        public FontFamilyKey(Uri source)
        {
            if (source.AbsolutePath.Contains(".ttf"))
            {
                if (source.Scheme == "res")
                {
                    FileName = source.AbsolutePath.Split('/').Last();
                }
                else
                {
                    var filePathWithoutExtension = source.AbsolutePath.Replace(".ttf", "");
                    var fileNameWithoutExtension = filePathWithoutExtension.Split('.').Last();
                    FileName = fileNameWithoutExtension + ".ttf";
                }

                Location = new Uri(source.OriginalString.Replace("." + FileName, ""), UriKind.RelativeOrAbsolute);
            }
            else
            {
                Location = source;
            }
        }

        public Uri Location { get; }

        public string FileName { get; }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = (int)2166136261;

                if (Location != null)
                {
                    hash = (hash * 16777619) ^ Location.GetHashCode();
                }

                if (FileName != null)
                {
                    hash = (hash * 16777619) ^ FileName.GetHashCode();
                }

                return hash;
            }
        }

        public override bool Equals(object obj)
        {
            if (!(obj is FontFamilyKey other)) return false;

            if (Location != other.Location) return false;

            if (FileName != other.FileName) return false;

            return true;
        }

        public override string ToString()
        {
            if (FileName != null)
            {
                if (Location.Scheme == "resm")
                {
                    return Location + "." + FileName;
                }

                return Location + "/" + FileName;
            }

            return Location.ToString();
        }
    }

    public class FontResource
    {
        public FontResource(Uri source)
        {
            Source = source;
        }

        public Uri Source { get; }
    }

    public class FontResourceCollection
    {
        private Dictionary<Uri, FontResource> _fontResources;
        private readonly IFontResourceLoader _fontResourceLoader = new FontResourceLoader();

        public FontResourceCollection(FontFamilyKey key)
        {
            Key = key;
        }

        public FontFamilyKey Key { get; }

        public IEnumerable<FontResource> FontResources
        {
            get
            {
                if (_fontResources == null)
                {
                    _fontResources = CreateFontResources();
                }

                return _fontResources.Values;
            }
        }

        private Dictionary<Uri, FontResource> CreateFontResources()
        {
            return _fontResourceLoader.GetFontResources(Key).ToDictionary(x => x.Source);
        }
    }

    public interface IFontResourceLoader
    {
        IEnumerable<FontResource> GetFontResources(FontFamilyKey fontFamilyKey);
    }

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

            var compareTo = GetLocationPath(location) + fileName.Split('*').First();

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

    public class CachedFontFamily
    {
        private readonly FontResourceCollection _fontResourceCollection;

        public CachedFontFamily(FontFamilyKey key, FontResourceCollection fontResourceCollection)
        {
            Key = key;
            _fontResourceCollection = fontResourceCollection;
        }

        public FontFamilyKey Key { get; }

        public IEnumerable<FontResource> FontResources => _fontResourceCollection.FontResources;
    }

    public class FontFamilyCache
    {
        private static readonly ConcurrentDictionary<FontFamilyKey, CachedFontFamily> s_cachedFontFamilies = new ConcurrentDictionary<FontFamilyKey, CachedFontFamily>();

        public CachedFontFamily GetOrAddFontFamily(FontFamilyKey key)
        {
            return s_cachedFontFamilies.GetOrAdd(key, CreateCachedFontFamily);
        }

        private static CachedFontFamily CreateCachedFontFamily(FontFamilyKey fontFamilyKey)
        {
            return new CachedFontFamily(fontFamilyKey, new FontResourceCollection(fontFamilyKey));
        }
    }
}
