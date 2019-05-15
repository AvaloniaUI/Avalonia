// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Avalonia.Platform;
using Avalonia.Utilities;

namespace Avalonia.Shared.PlatformSupport
{
    /// <summary>
    /// Loads assets compiled into the application binary.
    /// </summary>
    public class AssetLoader : IAssetLoader
    {
        private const string AvaloniaResourceName = "!AvaloniaResources";
        private static readonly Dictionary<string, AssemblyDescriptor> AssemblyNameCache
            = new Dictionary<string, AssemblyDescriptor>();

        private AssemblyDescriptor _defaultResmAssembly;

        /// <summary>
        /// Initializes a new instance of the <see cref="AssetLoader"/> class.
        /// </summary>
        /// <param name="assembly">
        /// The default assembly from which to load resm: assets for which no assembly is specified.
        /// </param>
        public AssetLoader(Assembly assembly = null)
        {
            if (assembly == null)
                assembly = Assembly.GetEntryAssembly();
            if (assembly != null)
                _defaultResmAssembly = new AssemblyDescriptor(assembly);
        }

        /// <summary>
        /// Sets the default assembly from which to load assets for which no assembly is specified.
        /// </summary>
        /// <param name="assembly">The default assembly.</param>
        public void SetDefaultAssembly(Assembly assembly)
        {
            _defaultResmAssembly = new AssemblyDescriptor(assembly);
        }

        /// <summary>
        /// Checks if an asset with the specified URI exists.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="baseUri">
        /// A base URI to use if <paramref name="uri"/> is relative.
        /// </param>
        /// <returns>True if the asset could be found; otherwise false.</returns>
        public bool Exists(Uri uri, Uri baseUri = null)
        {
            return GetAsset(uri, baseUri) != null;
        }

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
        public Stream Open(Uri uri, Uri baseUri = null) => OpenAndGetAssembly(uri, baseUri).Item1;

        /// <summary>
        /// Opens the asset with the requested URI and returns the asset stream and the
        /// assembly containing the asset.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="baseUri">
        /// A base URI to use if <paramref name="uri"/> is relative.
        /// </param>
        /// <returns>
        /// The stream containing the resource contents together with the assembly.
        /// </returns>
        /// <exception cref="FileNotFoundException">
        /// The asset could not be found.
        /// </exception>
        public (Stream stream, Assembly assembly) OpenAndGetAssembly(Uri uri, Uri baseUri = null)
        {
            var asset = GetAsset(uri, baseUri);

            if (asset == null)
            {
                throw new FileNotFoundException($"The resource {uri} could not be found.");
            }

            return (asset.GetStream(), asset.Assembly);
        }

        public Assembly GetAssembly(Uri uri, Uri baseUri)
        {
            if (!uri.IsAbsoluteUri && baseUri != null)
                uri = new Uri(baseUri, uri);
            return GetAssembly(uri).Assembly;
        }

        /// <summary>
        /// Gets all assets of a folder and subfolders that match specified uri.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="baseUri">Base URI that is used if <paramref name="uri"/> is relative.</param>
        /// <returns>All matching assets as a tuple of the absolute path to the asset and the assembly containing the asset</returns>
        public IEnumerable<Uri> GetAssets(Uri uri, Uri baseUri)
        {
            if (uri.IsAbsoluteUri && uri.Scheme == "resm")
            {
                var assembly = GetAssembly(uri);

                return assembly?.Resources.Where(x => x.Key.Contains(uri.AbsolutePath))
                           .Select(x =>new Uri($"resm:{x.Key}?assembly={assembly.Name}")) ??
                       Enumerable.Empty<Uri>();
            }

            uri = EnsureAbsolute(uri, baseUri);
            if (uri.Scheme == "avares")
            {
                var (asm, path) = GetResAsmAndPath(uri);
                if (asm == null)
                {
                    throw new ArgumentException(
                        "No default assembly, entry assembly or explicit assembly specified; " +
                        "don't know where to look up for the resource, try specifying assembly explicitly.");
                }

                if (asm?.AvaloniaResources == null)
                    return Enumerable.Empty<Uri>();
                path = path.TrimEnd('/') + '/';
                return asm.AvaloniaResources.Where(r => r.Key.StartsWith(path))
                    .Select(x => new Uri($"avares://{asm.Name}{x.Key}"));
            }

            return Enumerable.Empty<Uri>();
        }

        private Uri EnsureAbsolute(Uri uri, Uri baseUri)
        {
            if (uri.IsAbsoluteUri)
                return uri;
            if(baseUri == null)
                throw new ArgumentException($"Relative uri {uri} without base url");
            if (!baseUri.IsAbsoluteUri)
                throw new ArgumentException($"Base uri {baseUri} is relative");
            if (baseUri.Scheme == "resm")
                throw new ArgumentException(
                    $"Relative uris for 'resm' scheme aren't supported; {baseUri} uses resm");
            return new Uri(baseUri, uri);
        }
        
        private IAssetDescriptor GetAsset(Uri uri, Uri baseUri)
        {           
            if (uri.IsAbsoluteUri && uri.Scheme == "resm")
            {
                var asm = GetAssembly(uri) ?? GetAssembly(baseUri) ?? _defaultResmAssembly;

                if (asm == null)
                {
                    throw new ArgumentException(
                        "No default assembly, entry assembly or explicit assembly specified; " +
                        "don't know where to look up for the resource, try specifying assembly explicitly.");
                }

                IAssetDescriptor rv;

                var resourceKey = uri.AbsolutePath;
                asm.Resources.TryGetValue(resourceKey, out rv);
                return rv;
            }

            uri = EnsureAbsolute(uri, baseUri);

            if (uri.Scheme == "avares")
            {
                var (asm, path) = GetResAsmAndPath(uri);
                if (asm.AvaloniaResources == null)
                    return null;
                asm.AvaloniaResources.TryGetValue(path, out var desc);
                return desc;
            }

            throw new ArgumentException($"Unsupported url type: " + uri.Scheme, nameof(uri));
        }

        private (AssemblyDescriptor asm, string path) GetResAsmAndPath(Uri uri)
        {
            var asm = GetAssembly(uri.Authority);
            return (asm, uri.AbsolutePath);
        }
        
        private AssemblyDescriptor GetAssembly(Uri uri)
        {
            if (uri != null)
            {
                if (!uri.IsAbsoluteUri)
                    return null;
                if (uri.Scheme == "avares")
                    return GetResAsmAndPath(uri).asm;

                if (uri.Scheme == "resm")
                {
                    var qs = ParseQueryString(uri);
                    string assemblyName;

                    if (qs.TryGetValue("assembly", out assemblyName))
                    {
                        return GetAssembly(assemblyName);
                    }
                }
            }

            return null;
        }

        private AssemblyDescriptor GetAssembly(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            AssemblyDescriptor rv;
            if (!AssemblyNameCache.TryGetValue(name, out rv))
            {
                var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
                var match = loadedAssemblies.FirstOrDefault(a => a.GetName().Name == name);
                if (match != null)
                {
                    AssemblyNameCache[name] = rv = new AssemblyDescriptor(match);
                }
                else
                {
                    // iOS does not support loading assemblies dynamically!
                    //
#if __IOS__
                    throw new InvalidOperationException(
                        $"Assembly {name} needs to be referenced and explicitly loaded before loading resources");
#else
                    AssemblyNameCache[name] = rv = new AssemblyDescriptor(Assembly.Load(name));
#endif
                }
            }

            return rv;
        }

        private Dictionary<string, string> ParseQueryString(Uri uri)
        {
            return uri.Query.TrimStart('?')
                .Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Split('='))
                .ToDictionary(p => p[0], p => p[1]);
        }

        private interface IAssetDescriptor
        {
            Stream GetStream();
            Assembly Assembly { get; }
        }

        private class AssemblyResourceDescriptor : IAssetDescriptor
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

            public Assembly Assembly => _asm;
        }
        
        private class AvaloniaResourceDescriptor : IAssetDescriptor
        {
            private readonly int _offset;
            private readonly int _length;
            public Assembly Assembly { get; }

            public AvaloniaResourceDescriptor(Assembly asm, int offset, int length)
            {
                _offset = offset;
                _length = length;
                Assembly = asm;
            }
            
            public Stream GetStream()
            {
                return new SlicedStream(Assembly.GetManifestResourceStream(AvaloniaResourceName), _offset, _length);
            }
        }
        
        class SlicedStream : Stream
        {
            private readonly Stream _baseStream;
            private readonly int _from;

            public SlicedStream(Stream baseStream, int from, int length)
            {
                Length = length;
                _baseStream = baseStream;
                _from = from;
                _baseStream.Position = from;
            }
            public override void Flush()
            {
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return _baseStream.Read(buffer, offset, (int)Math.Min(count, Length - Position));
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                if (origin == SeekOrigin.Begin)
                    Position = offset;
                if (origin == SeekOrigin.End)
                    Position = _from + Length + offset;
                if (origin == SeekOrigin.Current)
                    Position = Position + offset;
                return Position;
            }

            public override void SetLength(long value) => throw new NotSupportedException();

            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

            public override bool CanRead => true;
            public override bool CanSeek => _baseStream.CanRead;
            public override bool CanWrite => false;
            public override long Length { get; }
            public override long Position
            {
                get => _baseStream.Position - _from;
                set => _baseStream.Position = value + _from;
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                    _baseStream.Dispose();
            }

            public override void Close() => _baseStream.Close();
        }

        private class AssemblyDescriptor
        {
            public AssemblyDescriptor(Assembly assembly)
            {
                Assembly = assembly;

                if (assembly != null)
                {
                    Resources = assembly.GetManifestResourceNames()
                        .ToDictionary(n => n, n => (IAssetDescriptor)new AssemblyResourceDescriptor(assembly, n));
                    Name = assembly.GetName().Name;
                    using (var resources = assembly.GetManifestResourceStream(AvaloniaResourceName))
                    {
                        if (resources != null)
                        {
                            Resources.Remove(AvaloniaResourceName);

                            var indexLength = new BinaryReader(resources).ReadInt32();
                            var index = AvaloniaResourcesIndexReaderWriter.Read(new SlicedStream(resources, 4, indexLength));
                            var baseOffset = indexLength + 4;
                            AvaloniaResources = index.ToDictionary(r => "/" + r.Path.TrimStart('/'), r => (IAssetDescriptor)
                                new AvaloniaResourceDescriptor(assembly, baseOffset + r.Offset, r.Size));
                        }
                    }
                }
            }

            public Assembly Assembly { get; }
            public Dictionary<string, IAssetDescriptor> Resources { get; }
            public Dictionary<string, IAssetDescriptor> AvaloniaResources { get; }
            public string Name { get; }
        }
        
        public static void RegisterResUriParsers()
        {
            if (!UriParser.IsKnownScheme("avares"))
                UriParser.Register(new GenericUriParser(
                    GenericUriParserOptions.GenericAuthority |
                    GenericUriParserOptions.NoUserInfo |
                    GenericUriParserOptions.NoPort |
                    GenericUriParserOptions.NoQuery |
                    GenericUriParserOptions.NoFragment), "avares", -1);
        }
    }
}
