using System.Collections.Concurrent;
using Avalonia.Media;
using Avalonia.Media.Fonts;

namespace Avalonia.Direct2D1.Media
{
    internal static class Direct2D1FontCollectionCache
    {
        private static readonly ConcurrentDictionary<FontFamilyKey, SharpDX.DirectWrite.FontCollection> s_cachedCollections;
        internal static readonly SharpDX.DirectWrite.FontCollection s_installedFontCollection;

        static Direct2D1FontCollectionCache()
        {
            s_cachedCollections = new ConcurrentDictionary<FontFamilyKey, SharpDX.DirectWrite.FontCollection>();

            s_installedFontCollection = Direct2D1Platform.DirectWriteFactory.GetSystemFontCollection(false);
        }

        public static SharpDX.DirectWrite.TextFormat GetTextFormat(Typeface typeface)
        {
            var fontFamily = typeface.FontFamily;
            var fontCollection = GetOrAddFontCollection(fontFamily);
            var fontFamilyName = FontFamily.Default.Name;

            // Should this be cached?
            foreach (var familyName in fontFamily.FamilyNames)
            {
                if (!fontCollection.FindFamilyName(familyName, out _))
                {
                    continue;
                }

                fontFamilyName = familyName;

                break;
            }

            return new SharpDX.DirectWrite.TextFormat(
                Direct2D1Platform.DirectWriteFactory, 
                fontFamilyName, 
                fontCollection, 
                (SharpDX.DirectWrite.FontWeight)typeface.Weight,
                (SharpDX.DirectWrite.FontStyle)typeface.Style, 
                SharpDX.DirectWrite.FontStretch.Normal, 
                (float)typeface.FontSize);
        }

        private static SharpDX.DirectWrite.FontCollection GetOrAddFontCollection(FontFamily fontFamily)
        {
            return fontFamily.Key == null ? s_installedFontCollection : s_cachedCollections.GetOrAdd(fontFamily.Key, CreateFontCollection);
        }

        private static SharpDX.DirectWrite.FontCollection CreateFontCollection(FontFamilyKey key)
        {
            var assets = FontFamilyLoader.LoadFontAssets(key);

            var fontLoader = new DWriteResourceFontLoader(Direct2D1Platform.DirectWriteFactory, assets);

            return new SharpDX.DirectWrite.FontCollection(Direct2D1Platform.DirectWriteFactory, fontLoader, fontLoader.Key);
        }
    }
}
