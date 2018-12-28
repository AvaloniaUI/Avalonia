// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Concurrent;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Avalonia.Platform;
using SkiaSharp;

namespace Avalonia.Skia
{
    internal static class SKTypefaceCollectionCache
    {
        private static readonly ConcurrentDictionary<FontFamilyKey, SKTypefaceCollection> s_cachedCollections;

        static SKTypefaceCollectionCache()
        {
            s_cachedCollections = new ConcurrentDictionary<FontFamilyKey, SKTypefaceCollection>();
        }

        /// <summary>
        /// Gets the or add typeface collection.
        /// </summary>
        /// <param name="fontFamily">The font family.</param>
        /// <returns></returns>
        public static SKTypefaceCollection GetOrAddTypefaceCollection(FontFamily fontFamily)
        {
            return s_cachedCollections.GetOrAdd(fontFamily.Key, x => CreateCustomFontCollection(fontFamily));
        }

        /// <summary>
        /// Creates the custom font collection.
        /// </summary>
        /// <param name="fontFamily">The font family.</param>
        /// <returns></returns>
        private static SKTypefaceCollection CreateCustomFontCollection(FontFamily fontFamily)
        {
            var fontAssets = FontFamilyLoader.LoadFontAssets(fontFamily.Key);

            var typeFaceCollection = new SKTypefaceCollection();

            var assetLoader = AvaloniaLocator.Current.GetService<IAssetLoader>();

            foreach (var asset in fontAssets)
            {
                var assetStream = assetLoader.Open(asset);

                var typeface = SKTypeface.FromStream(assetStream);

                typeFaceCollection.AddTypeFace(typeface);
            }

            return typeFaceCollection;
        }
    }
}
