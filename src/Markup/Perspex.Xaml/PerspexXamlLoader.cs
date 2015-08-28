// -----------------------------------------------------------------------
// <copyright file="PerspexXamlLoader.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Xaml
{
    using System;
    using OmniXaml;
    using Platform;
    using Perspex.Xaml.Context;
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
    }
}
