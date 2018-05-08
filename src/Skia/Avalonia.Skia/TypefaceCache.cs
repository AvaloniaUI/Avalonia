using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Avalonia.Platform;
using SkiaSharp;

namespace Avalonia.Skia
{
    static class TypefaceCache
    {
        static readonly Dictionary<string, Dictionary<FontKey, SKTypeface>> Cache = new Dictionary<string, Dictionary<FontKey, SKTypeface>>();

        struct FontKey
        {
            public readonly SKFontStyleSlant Slant;
            public readonly SKFontStyleWeight Weight;

            public FontKey(SKFontStyleWeight weight, SKFontStyleSlant slant)
            {
                Slant = slant;
                Weight = weight;
            }

            public override int GetHashCode()
            {
                int hash = 17;
                hash = hash * 31 + (int)Slant;
                hash = hash * 31 + (int)Weight;

                return hash;
            }

            public override bool Equals(object other)
            {
                return other is FontKey ? Equals((FontKey)other) : false;
            }

            public bool Equals(FontKey other)
            {
                return Slant == other.Slant &&
                       Weight == other.Weight;
            }

            // Equals and GetHashCode ommitted
        }

        static SKTypeface GetTypeface(FontFamily fontFamily, FontKey key)
        {
            var familyKey = fontFamily.Name;

            if (!Cache.TryGetValue(familyKey, out var entry))
            {
                Cache[familyKey] = entry = new Dictionary<FontKey, SKTypeface>();
            }

            if (!entry.TryGetValue(key, out var typeface))
            {
                typeface = SKTypeface.FromFamilyName(familyKey, key.Weight, SKFontStyleWidth.Normal, key.Slant);

                if (typeface == null)
                {
                    typeface = SKTypeface.FromFamilyName(null);
                }

                entry[key] = typeface;
            }

            return typeface;
        }

        public static SKTypeface GetTypeface(FontFamily fontFamily, FontStyle style, FontWeight weight)
        {
            SKFontStyleSlant skStyle = SKFontStyleSlant.Upright;

            switch (style)
            {
                case FontStyle.Italic:
                    skStyle = SKFontStyleSlant.Italic;
                    break;

                case FontStyle.Oblique:
                    skStyle = SKFontStyleSlant.Oblique;
                    break;
            }

            return GetTypeface(fontFamily, new FontKey((SKFontStyleWeight)weight, skStyle));
        }

    }

    internal class SKTypefaceCollection
    {
        private static readonly SKTypeface s_defaultTypeface = SKTypeface.FromFamilyName(null);

        struct FontKey
        {
            public readonly string Name;
            public readonly SKFontStyleSlant Slant;
            public readonly SKFontStyleWeight Weight;

            public FontKey(string name, SKFontStyleWeight weight, SKFontStyleSlant slant)
            {
                Name = name;
                Slant = slant;
                Weight = weight;
            }

            public override int GetHashCode()
            {
                int hash = 17;
                hash = hash * 31 + Name.GetHashCode();
                hash = hash * 31 + (int)Slant;
                hash = hash * 31 + (int)Weight;

                return hash;
            }

            public override bool Equals(object other)
            {
                return other is FontKey ? Equals((FontKey)other) : false;
            }

            public bool Equals(FontKey other)
            {
                return Name == other.Name && Slant == other.Slant &&
                       Weight == other.Weight;
            }

            // Equals and GetHashCode ommitted
        }

        private readonly ConcurrentDictionary<FontKey, SKTypeface> _cachedTypefaces =
            new ConcurrentDictionary<FontKey, SKTypeface>();

        public void AddTypeFace(FontFamily fontFamily, SKTypeface typeface)
        {
            var key = new FontKey(fontFamily.Name, (SKFontStyleWeight)typeface.FontWeight, typeface.FontSlant);

            _cachedTypefaces.TryAdd(key, typeface);
        }

        public SKTypeface GetTypeFace(Typeface typeface)
        {
            SKFontStyleSlant skStyle = SKFontStyleSlant.Upright;

            switch (typeface.Style)
            {
                case FontStyle.Italic:
                    skStyle = SKFontStyleSlant.Italic;
                    break;

                case FontStyle.Oblique:
                    skStyle = SKFontStyleSlant.Oblique;
                    break;
            }

            var key = new FontKey(typeface.FontFamily.Name, (SKFontStyleWeight)typeface.Weight, skStyle);

            return _cachedTypefaces.TryGetValue(key, out var skTypeface) ? skTypeface : s_defaultTypeface;
        }
    }

    internal static class SKTypefaceCollectionCache
    {
        private static readonly ConcurrentDictionary<FontFamilyKey, SKTypefaceCollection> s_cachedCollections =
            new ConcurrentDictionary<FontFamilyKey, SKTypefaceCollection>();

        public static SKTypefaceCollection GetOrAddTypefaceCollection(FontFamily fontFamily)
        {
            return s_cachedCollections.GetOrAdd(fontFamily.Key, x => CreateCustomFontCollection(fontFamily));
        }

        private static SKTypefaceCollection CreateCustomFontCollection(FontFamily fontFamily)
        {
            var cachedFontFamily = FontFamilyCache.GetOrAddFontFamily(fontFamily.Key);

            var typeFaceCollection = new SKTypefaceCollection();

            if (!cachedFontFamily.FontResources.Any()) return typeFaceCollection;

            var assetLoader = AvaloniaLocator.Current.GetService<IAssetLoader>();

            foreach (var fontResource in cachedFontFamily.FontResources)
            {
                var stream = assetLoader.Open(fontResource.Source);

                var typeface = SKTypeface.FromStream(stream);              

                typeFaceCollection.AddTypeFace(fontFamily, typeface);
            }

            return typeFaceCollection;
        }
    }
}