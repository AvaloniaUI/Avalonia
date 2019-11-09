using System.Collections.Concurrent;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using SharpDX.DirectWrite;
using FontFamily = Avalonia.Media.FontFamily;
using FontStyle = SharpDX.DirectWrite.FontStyle;
using FontWeight = SharpDX.DirectWrite.FontWeight;

namespace Avalonia.Direct2D1.Media
{
    internal static class Direct2D1FontCollectionCache
    {
        private static readonly ConcurrentDictionary<FontFamilyKey, FontCollection> s_cachedCollections;
        internal static readonly FontCollection InstalledFontCollection;

        static Direct2D1FontCollectionCache()
        {
            s_cachedCollections = new ConcurrentDictionary<FontFamilyKey, FontCollection>();

            InstalledFontCollection = Direct2D1Platform.DirectWriteFactory.GetSystemFontCollection(false);
        }

        public static Font GetFont(Typeface typeface)
        {
            var fontFamily = typeface.FontFamily;
            var fontCollection = GetOrAddFontCollection(fontFamily);

            foreach (var familyName in fontFamily.FamilyNames)
            {
                if (fontCollection.FindFamilyName(familyName, out var index))
                {
                    return fontCollection.GetFontFamily(index).GetFirstMatchingFont(
                        (FontWeight)typeface.Weight,
                        FontStretch.Normal,
                        (FontStyle)typeface.Style);
                }
            }

            InstalledFontCollection.FindFamilyName(FontFamily.Default.Name, out var i);

            return InstalledFontCollection.GetFontFamily(i).GetFirstMatchingFont(
                (FontWeight)typeface.Weight,
                FontStretch.Normal,
                (FontStyle)typeface.Style);
        }

        private static FontCollection GetOrAddFontCollection(FontFamily fontFamily)
        {
            return fontFamily.Key == null ? InstalledFontCollection : s_cachedCollections.GetOrAdd(fontFamily.Key, CreateFontCollection);
        }

        private static FontCollection CreateFontCollection(FontFamilyKey key)
        {
            var assets = FontFamilyLoader.LoadFontAssets(key);

            var fontLoader = new DWriteResourceFontLoader(Direct2D1Platform.DirectWriteFactory, assets);

            return new FontCollection(Direct2D1Platform.DirectWriteFactory, fontLoader, fontLoader.Key);
        }
    }
}
