// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;
using Avalonia.Media;
using SkiaSharp;

namespace Avalonia.Skia
{
    /// <summary>
    /// Cache for Skia typefaces.
    /// </summary>
    internal static class TypefaceCache
    {      
        private static readonly Dictionary<string, Dictionary<FontKey, SKTypeface>> s_cache = new Dictionary<string, Dictionary<FontKey, SKTypeface>>();

        public static SKTypeface Default { get; } =
            SKTypeface.FromFamilyName(FontFamily.Default.Name) ?? SKTypeface.FromFamilyName(null);

        public static SKTypeface GetSKTypeface(Typeface typeface)
        {
            if (typeface.FontFamily?.Name == null)
            {
                return SKTypeface.Default;
            }

            if (typeface.FontFamily.Key != null)
            {
                var typefaceCollection = SKTypefaceCollectionCache.GetOrAddTypefaceCollection(typeface.FontFamily);

                return typefaceCollection.GetTypeFace(typeface);
            }

            var fontStyleSlant = SKFontStyleSlant.Upright;

            switch (typeface.Style)
            {
                case FontStyle.Italic:
                    fontStyleSlant = SKFontStyleSlant.Italic;
                    break;

                case FontStyle.Oblique:
                    fontStyleSlant = SKFontStyleSlant.Oblique;
                    break;
            }

            if (typeface.FontFamily.FamilyNames.HasFallbacks)
            {
                var skiaTypeface = Default;

                foreach (var familyName in typeface.FontFamily.FamilyNames)
                {
                    skiaTypeface = GetSKTypeface(familyName, new FontKey((SKFontStyleWeight)typeface.Weight, fontStyleSlant));
                    if (skiaTypeface != Default)
                    {
                        break;
                    }
                }

                return skiaTypeface;
            }

            return GetSKTypeface(typeface.FontFamily.Name, new FontKey((SKFontStyleWeight)typeface.Weight, fontStyleSlant));
        }

        private static SKTypeface GetSKTypeface(string name, FontKey key)
        {
            var familyKey = name;

            if (!s_cache.TryGetValue(familyKey, out var entry))
            {
                s_cache[familyKey] = entry = new Dictionary<FontKey, SKTypeface>();
            }

            if (!entry.TryGetValue(key, out var typeface))
            {
                typeface = SKTypeface.FromFamilyName(familyKey, key.Weight, SKFontStyleWidth.Normal, key.Slant)
                           ?? Default;

                entry[key] = typeface;
            }

            return typeface;
        }

        private struct FontKey
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
                var hash = 17;
                hash = (hash * 31) + (int)Slant;
                hash = (hash * 31) + (int)Weight;

                return hash;
            }

            public override bool Equals(object other)
            {
                return other is FontKey a && Equals(a);
            }

            private bool Equals(FontKey other)
            {
                return Slant == other.Slant &&
                       Weight == other.Weight;
            }

            // Equals and GetHashCode ommitted
        }
    }
}
