// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Text;
using Avalonia.Controls;
using Avalonia.Markup.Data;
using Avalonia.Markup.Xaml.Context;
using Avalonia.Platform;

#if SYSTEM_XAML
using System.Xaml;
#else
using Portable.Xaml;
#endif

namespace Avalonia.Markup.Xaml
{
    /// <summary>
    /// Loads XAML for a avalonia application.
    /// </summary>
    public class AvaloniaXamlLoader
    {
        private static readonly RuntimeTypeProvider s_typeProvider = new RuntimeTypeProvider();
        private static readonly AvaloniaXamlSchemaContext s_context = new AvaloniaXamlSchemaContext(s_typeProvider);

        /// <summary>
        /// Loads the XAML into a Avalonia component.
        /// </summary>
        /// <param name="obj">The object to load the XAML into.</param>
        public static void Load(object obj)
        {
            Contract.Requires<ArgumentNullException>(obj != null);

            Load(obj.GetType(), obj);
        }

        /// <summary>
        /// Loads the XAML for a type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="rootInstance">
        /// The optional instance into which the XAML should be loaded.
        /// </param>
        /// <returns>The loaded object.</returns>
        public static object Load(Type type, object rootInstance = null)
        {
            Contract.Requires<ArgumentNullException>(type != null);

            // HACK: Currently Visual Studio is forcing us to change the extension of xaml files
            // in certain situations, so we try to load .xaml and if that's not found we try .xaml.
            // Ideally we'd be able to use .xaml everywhere
            var assetLocator = AvaloniaLocator.Current.GetService<IAssetLoader>();

            if (assetLocator == null)
            {
                throw new InvalidOperationException(
                    "Could not create IAssetLoader : maybe Application.RegisterServices() wasn't called?");
            }

            foreach (var uri in GetUrisFor(type))
            {
                if (assetLocator.Exists(uri))
                {
                    using (var stream = assetLocator.Open(uri))
                    {
                        var initialize = rootInstance as ISupportInitialize;
                        initialize?.BeginInit();
                        try
                        {
                            return Load(stream, type.Assembly, rootInstance, uri);
                        }
                        finally
                        {
                            initialize?.EndInit();
                        }
                    }
                }
            }

            throw new FileNotFoundException("Unable to find view for " + type.FullName);
        }

        /// <summary>
        /// Loads XAML from a URI.
        /// </summary>
        /// <param name="uri">The URI of the XAML file.</param>
        /// <param name="baseUri">
        /// A base URI to use if <paramref name="uri"/> is relative.
        /// </param>
        /// <param name="rootInstance">
        /// The optional instance into which the XAML should be loaded.
        /// </param>
        /// <returns>The loaded object.</returns>
        public object Load(Uri uri, Uri baseUri = null, object rootInstance = null)
        {
            Contract.Requires<ArgumentNullException>(uri != null);

            var assetLocator = AvaloniaLocator.Current.GetService<IAssetLoader>();

            if (assetLocator == null)
            {
                throw new InvalidOperationException(
                    "Could not create IAssetLoader : maybe Application.RegisterServices() wasn't called?");
            }

            var asset = assetLocator.OpenAndGetAssembly(uri, baseUri);
            using (var stream = asset.stream)
            {
                try
                {
                    return Load(stream, asset.assembly, rootInstance, uri);
                }
                catch (Exception e)
                {
                    var uriString = uri.ToString();
                    if (!uri.IsAbsoluteUri)
                    {
                        uriString = new Uri(baseUri, uri).AbsoluteUri;
                    }
                    throw new XamlLoadException("Error loading xaml at " + uriString + ": " + e.Message, e);
                }
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
        public static object Load(Stream stream, Assembly localAssembly, object rootInstance = null, Uri uri = null)
        {
            var readerSettings = new XamlXmlReaderSettings()
            {
                BaseUri = uri,
                LocalAssembly = localAssembly
            };

            var namescope = new NameScopeAdapter(rootInstance as INameScope);
            var settings = new XamlObjectWriterSettings
            {
                RootObjectInstance = rootInstance,
                ExternalNameScope = namescope,
                RegisterNamesOnExternalNamescope = true,
            };

            var reader = new XamlXmlReader(stream, s_context, readerSettings);
            var writer = new XamlObjectWriter(s_context, settings);

            XamlServices.Transform(reader, writer);
            namescope.Apply(writer.Result);

            if (writer.Result is TopLevel topLevel)
            {
                DelayedBinding.ApplyBindings(topLevel);
            }

            return writer.Result;
        }

        public static object Parse(string xaml, Assembly localAssembly = null) =>
            new AvaloniaXamlLoader().Load(xaml, localAssembly);

        public static T Parse<T>(string xaml, Assembly localAssembly = null) => (T)Parse(xaml, localAssembly);

        /// <summary>
        /// Gets the URI for a type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The URI.</returns>
        private static IEnumerable<Uri> GetUrisFor(Type type)
        {
            var asm = type.GetTypeInfo().Assembly.GetName().Name;
            var typeName = type.FullName;
            yield return new Uri("resm:" + typeName + ".xaml?assembly=" + asm);
            yield return new Uri("resm:" + typeName + ".paml?assembly=" + asm);
        }
    }
}
