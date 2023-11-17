using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using Avalonia.Metadata;
using Avalonia.Platform.Internal;
using Avalonia.Utilities;

namespace Avalonia.Platform;

/// <summary>
/// Loads assets compiled into the application binary.
/// </summary>
[Unstable("StandardAssetLoader is considered unstable. Please use AssetLoader static class instead.")]
public class StandardAssetLoader : IAssetLoader
{
    private readonly IAssemblyDescriptorResolver _assemblyDescriptorResolver;
    private AssemblyDescriptor? _defaultResmAssembly;

    internal StandardAssetLoader(IAssemblyDescriptorResolver resolver, Assembly? assembly = null)
    {
        if (assembly == null)
            assembly = Assembly.GetEntryAssembly();
        if (assembly != null)
            _defaultResmAssembly = new AssemblyDescriptor(assembly);
        _assemblyDescriptorResolver = resolver;
    }

    public StandardAssetLoader(Assembly? assembly = null) : this(new AssemblyDescriptorResolver(), assembly)
    {
            
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
    public bool Exists(Uri uri, Uri? baseUri = null)
    {
        return TryGetAsset(uri, baseUri, out _);
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
    public Stream Open(Uri uri, Uri? baseUri = null) => OpenAndGetAssembly(uri, baseUri).Item1;

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
    public (Stream stream, Assembly assembly) OpenAndGetAssembly(Uri uri, Uri? baseUri = null)
    {
        if (TryGetAsset(uri, baseUri, out var assetDescriptor))
        {
            return (assetDescriptor.GetStream(), assetDescriptor.Assembly);
        }

        throw new FileNotFoundException($"The resource {uri} could not be found.");
    }

    public Assembly? GetAssembly(Uri uri, Uri? baseUri)
    {
        if (!uri.IsAbsoluteUri && baseUri != null)
        {
            uri = new Uri(baseUri, uri);
        }

        if (TryGetAssembly(uri, out var assemblyDescriptor))
        {
            return assemblyDescriptor.Assembly;
        }

        return null;
    }

    /// <summary>
    /// Gets all assets of a folder and subfolders that match specified uri.
    /// </summary>
    /// <param name="uri">The URI.</param>
    /// <param name="baseUri">Base URI that is used if <paramref name="uri"/> is relative.</param>
    /// <returns>All matching assets as a tuple of the absolute path to the asset and the assembly containing the asset</returns>
    public IEnumerable<Uri> GetAssets(Uri uri, Uri? baseUri)
    {
        if (uri.IsAbsoluteResm())
        {
            if (!TryGetAssembly(uri, out var assembly))
            {
                assembly = _defaultResmAssembly;
            }

            return assembly?.Resources?
                       .Where(x => x.Key.Contains(uri.GetUnescapeAbsolutePath()))
                       .Select(x => new Uri($"resm:{x.Key}?assembly={assembly.Name}")) ??
                   Enumerable.Empty<Uri>();
        }

        uri = uri.EnsureAbsolute(baseUri);

        if (uri.IsAvares())
        {
            if (!TryGetResAsmAndPath(uri, out var assembly, out var path))
            {
                return Enumerable.Empty<Uri>();
            }

            if (assembly?.AvaloniaResources == null)
            {
                return Enumerable.Empty<Uri>();
            }

            if (path.Length > 0 && path[path.Length - 1] != '/')
            {
                path += '/';
            }

            return assembly.AvaloniaResources
                .Where(r => r.Key.StartsWith(path, StringComparison.Ordinal))
                .Select(x => new Uri($"avares://{assembly.Name}{x.Key}"));
        }

        return Enumerable.Empty<Uri>();
    }

    public static void RegisterResUriParsers() => AssetLoader.RegisterResUriParsers();

    private bool TryGetAsset(Uri uri, Uri? baseUri, [NotNullWhen(true)] out IAssetDescriptor? assetDescriptor)
    {
        assetDescriptor = null;

        if (uri.IsAbsoluteResm())
        {
            if (!TryGetAssembly(uri, out var assembly) && !TryGetAssembly(baseUri, out assembly))
            {
                assembly = _defaultResmAssembly;
            }

            if (assembly?.Resources != null)
            {
                var resourceKey = uri.AbsolutePath;

                if (assembly.Resources.TryGetValue(resourceKey, out assetDescriptor))
                {
                    return true;
                }
            }
        }

        uri = uri.EnsureAbsolute(baseUri);

        if (uri.IsAvares())
        {
            if (TryGetResAsmAndPath(uri, out var assembly, out var path))
            {
                if (assembly.AvaloniaResources == null)
                {
                    return false;
                }

                if (assembly.AvaloniaResources.TryGetValue(path, out assetDescriptor))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool TryGetResAsmAndPath(Uri uri, [NotNullWhen(true)] out IAssemblyDescriptor? assembly, out string path)
    {
        path = uri.GetUnescapeAbsolutePath();

        if (TryLoadAssembly(uri.Authority, out assembly))
        {
            return true;
        }

        return false;
    }

    private bool TryGetAssembly(Uri? uri, [NotNullWhen(true)] out IAssemblyDescriptor? assembly)
    {
        assembly = null;

        if (uri != null)
        {
            if (!uri.IsAbsoluteUri)
            {
                return false;
            }

            if (uri.IsAvares() && TryGetResAsmAndPath(uri, out assembly, out _))
            {
                return true;
            }

            if (uri.IsResm())
            {
                var assemblyName = uri.GetAssemblyNameFromQuery();

                if (assemblyName.Length > 0 && TryLoadAssembly(assemblyName, out assembly))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool TryLoadAssembly(string assemblyName, [NotNullWhen(true)] out IAssemblyDescriptor? assembly)
    {
        assembly = null;

        try
        {
            assembly = _assemblyDescriptorResolver.GetAssembly(assemblyName);

            return true;
        }
        catch (Exception) { }

        return false;
    }
}
