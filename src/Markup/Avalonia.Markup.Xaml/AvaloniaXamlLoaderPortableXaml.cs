// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Context;
using Avalonia.Markup.Xaml.Data;
using Avalonia.Markup.Xaml.PortableXaml;
using Avalonia.Platform;
using Portable.Xaml;

namespace Avalonia.Markup.Xaml
{
    /// <summary>
    /// Loads XAML for a avalonia application.
    /// </summary>
    public class AvaloniaXamlLoaderPortableXaml
    {
        internal static readonly AvaloniaXamlSchemaContext _context
            = new AvaloniaXamlSchemaContext(new AvaloniaRuntimeTypeProvider());

        /// <summary>
        /// Initializes a new instance of the <see cref="AvaloniaXamlLoader"/> class.
        /// </summary>
        public AvaloniaXamlLoaderPortableXaml()
        {
        }

        /// <summary>
        /// Gets the URI of the XAML file currently being loaded.
        /// </summary>
        /// <remarks>
        /// TODO: Making this internal for now as I'm not sure that this is the correct
        /// thing to do, but its needed by <see cref="StyleInclude"/> to get the URL of
        /// the currently loading XAML file, as we can't use the OmniXAML parsing context
        /// there. Maybe we need a way to inject OmniXAML context into the objects its
        /// constructing?
        /// </remarks>
        [Obsolete]
        internal static Uri UriContext => s_uriStack.Count > 0 ? s_uriStack.Peek() : null;

        private static Stack<Uri> s_uriStack = new Stack<Uri>();

        /// <summary>
        /// Loads the XAML into a Avalonia component.
        /// </summary>
        /// <param name="obj">The object to load the XAML into.</param>
        public static void Load(object obj)
        {
            Contract.Requires<ArgumentNullException>(obj != null);

            var loader = new AvaloniaXamlLoader();
            loader.Load(obj.GetType(), obj);
        }

        /// <summary>
        /// Loads the XAML for a type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="rootInstance">
        /// The optional instance into which the XAML should be loaded.
        /// </param>
        /// <returns>The loaded object.</returns>
        public object Load(Type type, object rootInstance = null)
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
                        return Load(stream, type, rootInstance, uri);
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

            using (var stream = assetLocator.Open(uri, baseUri))
            {
                return Load(stream, null, rootInstance, uri);
            }
        }

        /// <summary>
        /// Loads XAML from a string.
        /// </summary>
        /// <param name="xaml">The string containing the XAML.</param>
        /// <param name="rootInstance">
        /// The optional instance into which the XAML should be loaded.
        /// </param>
        /// <returns>The loaded object.</returns>
        public object Load(string xaml, Type type = null, object rootInstance = null)
        {
            Contract.Requires<ArgumentNullException>(xaml != null);

            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(xaml)))
            {
                return Load(stream, type, rootInstance);
            }
        }

        /// <summary>
        /// Loads XAML from a stream.
        /// </summary>
        /// <param name="stream">The stream containing the XAML.</param>
        /// <param name="rootInstance">
        /// The optional instance into which the XAML should be loaded.
        /// </param>
        /// <param name="uri">The URI of the XAML</param>
        /// <returns>The loaded object.</returns>
        public object Load(Stream stream, Type type = null, object rootInstance = null, Uri uri = null)
        {
            try
            {
                if (uri != null)
                {
                    s_uriStack.Push(uri);
                }

                var readerSettings = new XamlXmlReaderSettings();

                if (rootInstance != null && type == null)
                {
                    type = rootInstance.GetType();
                }

                if (type != null)
                {
                    readerSettings.LocalAssembly = type.GetTypeInfo().Assembly;
                }

                var reader = new XamlXmlReader(stream, _context, readerSettings);
                object result = Load(reader, rootInstance);

                var topLevel = result as TopLevel;

                if (topLevel != null)
                {
                    DelayedBinding.ApplyBindings(topLevel);
                }

                return result;
            }
            finally
            {
                if (uri != null)
                {
                    s_uriStack.Pop();
                }
            }
        }

        internal static object Load(XamlXmlReader reader, object instance)
        {
            var writer = AvaloniaXamlObjectWriter.Create(_context, instance);

            XamlServices.Transform(reader, writer);

            return writer.Result;
        }

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