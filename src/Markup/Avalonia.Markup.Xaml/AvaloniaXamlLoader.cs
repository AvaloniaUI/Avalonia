using System;
using System.IO;
using Avalonia.Platform;
#nullable enable
namespace Avalonia.Markup.Xaml
{
    /// <summary>
    /// Loads XAML for a avalonia application.
    /// </summary>
    public static class AvaloniaXamlLoader
    {
        public interface IRuntimeXamlLoader
        {
            object Load(RuntimeXamlLoaderDocument document, RuntimeXamlLoaderConfiguration configuration);
        }
        
        /// <summary>
        /// Loads the XAML into a Avalonia component.
        /// </summary>
        /// <param name="obj">The object to load the XAML into.</param>
        public static void Load(object obj)
        {
            throw new XamlLoadException(
                $"No precompiled XAML found for {obj.GetType()}, make sure to specify x:Class and include your XAML file as AvaloniaResource");
        }

        /// <summary>
        /// Loads XAML from a URI.
        /// </summary>
        /// <param name="uri">The URI of the XAML file.</param>
        /// <param name="baseUri">
        /// A base URI to use if <paramref name="uri"/> is relative.
        /// </param>
        /// <returns>The loaded object.</returns>
        public static object Load(Uri uri, Uri? baseUri = null)
        {
            if (uri is null)
                throw new ArgumentNullException(nameof(uri));

            var assetLocator = AvaloniaLocator.Current.GetService<IAssetLoader>();

            if (assetLocator == null)
            {
                throw new InvalidOperationException(
                    "Could not create IAssetLoader : maybe Application.RegisterServices() wasn't called?");
            }

            var absoluteUri = uri.IsAbsoluteUri
                ? uri
                : new Uri(baseUri ?? throw new InvalidOperationException("Cannot load relative Uri when BaseUri is null"), uri);

            var compiledLoader = assetLocator.GetAssembly(uri, baseUri)
                ?.GetType("CompiledAvaloniaXaml.!XamlLoader")
                ?.GetMethod("TryLoad", new[] {typeof(string)});
            if (compiledLoader != null)
            {
                var compiledResult = compiledLoader.Invoke(null, new object[] {absoluteUri.ToString()});
                if (compiledResult != null)
                    return compiledResult;
            }

            // This is intended for unit-tests only
            var runtimeLoader = AvaloniaLocator.Current.GetService<IRuntimeXamlLoader>();
            if (runtimeLoader != null)
            {
                var asset = assetLocator.OpenAndGetAssembly(uri, baseUri);
                using (var stream = asset.stream)
                {
                    return runtimeLoader.Load(new RuntimeXamlLoaderDocument(absoluteUri, stream), new RuntimeXamlLoaderConfiguration
                    {
                        LocalAssembly = asset.assembly
                    });
                }
            }

            throw new XamlLoadException(
                $"No precompiled XAML found for {uri} (baseUri: {baseUri}), make sure to specify x:Class and include your XAML file as AvaloniaResource");
        }
        
    }
}
