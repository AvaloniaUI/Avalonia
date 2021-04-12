using System.Collections.Concurrent;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using JetBrains.Annotations;
using Vortice.DirectWrite;
using FontStyle = Vortice.DirectWrite.FontStyle;
using FontWeight = Vortice.DirectWrite.FontWeight;

namespace Avalonia.Direct2D1.Media
{
    internal static class Direct2D1FontCollectionCache
    {
        private static readonly ConcurrentDictionary<FontFamilyKey, IDWriteFontCollection> s_cachedCollections;
        internal static readonly IDWriteFontCollection InstalledFontCollection;

        static Direct2D1FontCollectionCache()
        {
            s_cachedCollections = new ConcurrentDictionary<FontFamilyKey, IDWriteFontCollection>();

            InstalledFontCollection = Direct2D1Platform.DirectWriteFactory.GetSystemFontCollection(false);
        }

        public static IDWriteFont GetFont(Typeface typeface) =>
            GetFontFamily(typeface.FontFamily)?.GetFirstMatchingFont(
                (FontWeight)typeface.Weight, FontStretch.Normal, (FontStyle)typeface.Style
            ) ?? throw new AvaloniaInternalException($"Failed to find DirectWrite font matching typeface [{typeface}]");

        [CanBeNull]
        private static IDWriteFontFamily GetFontFamily(FontFamily fontFamily)
        {
            var fontCollection = GetOrAddFontCollection(fontFamily);
            int index;

            foreach (var name in fontFamily.FamilyNames)
            {
                if (fontCollection.FindFamilyName(name, out index))
                {
                    return fontCollection.GetFontFamily(index);
                }
            }

            return InstalledFontCollection.FindFamilyName("Segoe UI", out index)
                       ? InstalledFontCollection.GetFontFamily(index)
                       : null;
        }

        private static IDWriteFontCollection GetOrAddFontCollection(FontFamily fontFamily)
        {
            return fontFamily.Key == null ? InstalledFontCollection : s_cachedCollections.GetOrAdd(fontFamily.Key, CreateFontCollection);
        }

        private static IDWriteFontCollection CreateFontCollection(FontFamilyKey key)
        {
            var assets = FontFamilyLoader.LoadFontAssets(key);

            var fontLoader = new DWriteResourceFontLoader(Direct2D1Platform.DirectWriteFactory, assets);

            return Direct2D1Platform.DirectWriteFactory.CreateCustomFontCollection(
                fontLoader, fontLoader.Key.BasePointer, checked((int)fontLoader.Key.Length)
            );
        }
    }
}
