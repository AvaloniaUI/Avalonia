using System;
using System.Collections.Generic;
using System.Text;
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

        unsafe static SKTypeface GetTypeface(FontFamily fontFamily, FontKey key)
        {
            var familyKey = fontFamily.Name;

            if (!Cache.TryGetValue(familyKey, out var entry))
            {
                Cache[familyKey] = entry = new Dictionary<FontKey, SKTypeface>();
            }

            if (!entry.TryGetValue(key, out var typeface))
            {
                if (fontFamily.Key != null)
                {
                    var cachedFontFamily = FontFamilyCache.GetOrAddFontFamily(fontFamily.Key);

                    var assetLoader = AvaloniaLocator.Current.GetService<IAssetLoader>();

                    foreach (var fontResource in cachedFontFamily.FontResources)
                    {
                        var stream = assetLoader.Open(fontResource.Source);

                        typeface = SKTypeface.FromStream(stream);

                        if (typeface.FamilyName != familyKey) continue;

                        var fontKey = new FontKey((SKFontStyleWeight)typeface.FontWeight, typeface.FontSlant);

                        entry[fontKey] = typeface;
                    }

                    entry.TryGetValue(key, out typeface);

                    if (typeface == null)
                    {
                        typeface = SKTypeface.FromFamilyName(null);
                    }

                    return typeface;
                }

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
}