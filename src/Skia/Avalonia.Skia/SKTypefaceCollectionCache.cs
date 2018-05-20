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

        public static SKTypefaceCollection GetOrAddTypefaceCollection(FontFamily fontFamily)
        {
            return s_cachedCollections.GetOrAdd(fontFamily.Key, x => CreateCustomFontCollection(fontFamily));
        }

        private static SKTypefaceCollection CreateCustomFontCollection(FontFamily fontFamily)
        {
            var assets = FontFamilyLoader.LoadFontAssets(fontFamily.Key);

            var typeFaceCollection = new SKTypefaceCollection();

            var assetLoader = AvaloniaLocator.Current.GetService<IAssetLoader>();

            foreach (var fontAsset in assets)
            {
                var assetStream = assetLoader.Open(fontAsset.Source);

                var typeface = SKTypeface.FromStream(assetStream);

                typeFaceCollection.AddTypeFace(typeface);
            }

            return typeFaceCollection;
        }
    }
}