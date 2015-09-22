// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using OmniXaml;
using Perspex.Markup.Xaml.Context;
using Perspex.Platform;
using Splat;

namespace Perspex.Markup.Xaml
{
    /// <summary>
    /// Loads XAML for a perspex application.
    /// </summary>
    public class PerspexXamlLoader : XamlLoader
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PerspexXamlLoader"/> class.
        /// </summary>
        public PerspexXamlLoader()
            : this(new PerspexParserFactory())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PerspexXamlLoader"/> class.
        /// </summary>
        /// <param name="xamlParserFactory">The parser factory to use.</param>
        public PerspexXamlLoader(IXamlParserFactory xamlParserFactory)
            : base(xamlParserFactory)
        {
        }

        /// <summary>
        /// Loads the XAML into a Perspex component.
        /// </summary>
        /// <param name="obj">The object to load the XAML into.</param>
        public static void Load(object obj)
        {
            var loader = new PerspexXamlLoader();
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
            // HACK: Currently Visual Studio is forcing us to change the extension of xaml files
            // in certain situations, so we try to load .xaml and if that's not found we try .paml.
            // Ideally we'd be able to use .xaml everywhere
            var assetLocator = Locator.Current.GetService<IAssetLoader>();
            if (assetLocator == null)
            {
                throw new InvalidOperationException(
                    "Could not create IAssetLoader : maybe Application.RegisterServices() wasn't called?");
            }
            foreach (var uri in GetUrisFor(type))
            {
                Stream stream;
                try
                {
                    stream= assetLocator.Open(uri);
                }
                catch (FileNotFoundException)
                {
                    continue;
                }
                using (stream)
                    return Load(stream, rootInstance);
            }
            throw new FileNotFoundException("Unable to find view for " + type.FullName);
        }

        /// <summary>
        /// Loads XAML from a URI.
        /// </summary>
        /// <param name="uri">The URI of the XAML file.</param>
        /// <param name="rootInstance">
        /// The optional instance into which the XAML should be loaded.
        /// </param>
        /// <returns>The loaded object.</returns>
        public object Load(Uri uri, object rootInstance = null)
        {
            var assetLocator = Locator.Current.GetService<IAssetLoader>();

            if (assetLocator == null)
            {
                throw new InvalidOperationException(
                    "Could not create IAssetLoader : maybe Application.RegisterServices() wasn't called?");
            }

            using (var stream = assetLocator.Open(uri))
            {
                return Load(stream, rootInstance);
            }
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
            yield return new Uri("resource://application/" + asm + "/" + typeName+".xaml");
            yield return new Uri("resource://application/" + asm + "/" + typeName + ".paml");

        }
    }
}
