// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.IO;
using System.Reflection;
using System.Text;
using Avalonia.Markup.Xaml.XamlIl;
using Avalonia.Platform;

namespace Avalonia.Markup.Xaml
{
    /// <summary>
    /// Loads XAML for a avalonia application.
    /// </summary>
    public class AvaloniaXamlLoader
    {
        public bool IsDesignMode { get; set; }

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
        public object Load(Uri uri, Uri baseUri = null)
        {
            Contract.Requires<ArgumentNullException>(uri != null);

            var assetLocator = AvaloniaLocator.Current.GetService<IAssetLoader>();

            if (assetLocator == null)
            {
                throw new InvalidOperationException(
                    "Could not create IAssetLoader : maybe Application.RegisterServices() wasn't called?");
            }

            var compiledLoader = assetLocator.GetAssembly(uri, baseUri)
                ?.GetType("CompiledAvaloniaXaml.!XamlLoader")
                ?.GetMethod("TryLoad", new[] {typeof(string)});
            if (compiledLoader != null)
            {
                var uriString = (!uri.IsAbsoluteUri && baseUri != null ? new Uri(baseUri, uri) : uri)
                    .ToString();
                var compiledResult = compiledLoader.Invoke(null, new object[] {uriString});
                if (compiledResult != null)
                    return compiledResult;
            }
            
            
            var asset = assetLocator.OpenAndGetAssembly(uri, baseUri);
            using (var stream = asset.stream)
            {
                var absoluteUri = uri.IsAbsoluteUri ? uri : new Uri(baseUri, uri);
                return Load(stream, asset.assembly, null, absoluteUri);
            }
        }
        
        /// <summary>
        /// Loads XAML from a string.
        /// </summary>
        /// <param name="xaml">The string containing the XAML.</param>
        /// <param name="localAssembly">Default assembly for clr-namespace:</param>
        /// <param name="rootInstance">
        /// The optional instance into which the XAML should be loaded.
        /// </param>
        /// <returns>The loaded object.</returns>
        public object Load(string xaml, Assembly localAssembly = null, object rootInstance = null)
        {
            Contract.Requires<ArgumentNullException>(xaml != null);

            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(xaml)))
            {
                return Load(stream, localAssembly, rootInstance);
            }
        }

        /// <summary>
        /// Loads XAML from a stream.
        /// </summary>
        /// <param name="stream">The stream containing the XAML.</param>
        /// <param name="localAssembly">Default assembly for clr-namespace</param>
        /// <param name="rootInstance">
        /// The optional instance into which the XAML should be loaded.
        /// </param>
        /// <param name="uri">The URI of the XAML</param>
        /// <returns>The loaded object.</returns>
        public object Load(Stream stream, Assembly localAssembly, object rootInstance = null, Uri uri = null) 
            => AvaloniaXamlIlRuntimeCompiler.Load(stream, localAssembly, rootInstance, uri, IsDesignMode);

        public static object Parse(string xaml, Assembly localAssembly = null)
            => new AvaloniaXamlLoader().Load(xaml, localAssembly);

        public static T Parse<T>(string xaml, Assembly localAssembly = null)
            => (T)Parse(xaml, localAssembly);
    }
}
