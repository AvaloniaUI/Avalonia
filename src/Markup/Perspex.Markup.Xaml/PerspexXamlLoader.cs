// -----------------------------------------------------------------------
// <copyright file="PerspexXamlLoader.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Markup.Xaml
{
    using System;
    using System.Reflection;
    using OmniXaml;
    using Perspex.Markup.Xaml.Context;
    using Platform;
    using Splat;

    public class PerspexXamlLoader : XamlLoader
    {
        public PerspexXamlLoader()
            : this(new PerspexParserFactory())
        {
        }

        public PerspexXamlLoader(IXamlParserFactory xamlParserFactory)
            : base(xamlParserFactory)
        {
        }

        public static void Load(object obj)
        {
            var loader = new PerspexXamlLoader();
            loader.Load(obj.GetType(), obj);
        }

        public object Load(Type type, object rootInstance = null)
        {
            return this.Load(GetUriFor(type), rootInstance);
        }

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
                return this.Load(stream, rootInstance);
            }
        }

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
