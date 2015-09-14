// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
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
            return Load(GetUriFor(type), rootInstance);
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
        private static Uri GetUriFor(Type type)
        {
            if (type.Namespace != null)
            {
                var toRemove = type.GetTypeInfo().Assembly.GetName().Name;
                var substracted = toRemove.Length < type.Namespace.Length ? type.Namespace.Remove(0, toRemove.Length + 1) : "";
                var replace = substracted.Replace('.', '/');

                if (replace != string.Empty)
                {
                    replace = replace + "/";
                }

                return new Uri(replace + type.Name + ".xaml", UriKind.Relative);
            }

            return null;
        }
    }
}
