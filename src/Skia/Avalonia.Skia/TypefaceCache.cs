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
        private static readonly string s_defaultFamilyName = CreateDefaultFamilyName();

        private static readonly Dictionary<string, Dictionary<FontKey, SKTypeface>> s_cache =
            new Dictionary<string, Dictionary<FontKey, SKTypeface>>();

        private static string CreateDefaultFamilyName()
        {
            var defaultTypeface = SKTypeface.CreateDefault();

            return defaultTypeface.FamilyName;
        }

        private static SKTypeface GetTypeface(string name, FontKey key)
        {
            var familyKey = name;

            if (!s_cache.TryGetValue(familyKey, out var entry))
            {
                s_cache[familyKey] = entry = new Dictionary<FontKey, SKTypeface>();
            }

            if (!entry.TryGetValue(key, out var typeface))
            {
                typeface = SKTypeface.FromFamilyName(familyKey, key.Weight, SKFontStyleWidth.Normal, key.Slant) ??
                           GetTypeface(s_defaultFamilyName, key);

                entry[key] = typeface;
            }

            return typeface;
        }

        public static SKTypeface GetTypeface(Typeface typeface)
        {
            var skStyle = SKFontStyleSlant.Upright;

            switch (typeface.Style)
            {
                case FontStyle.Italic:
                    skStyle = SKFontStyleSlant.Italic;
                    break;

                case FontStyle.Oblique:
                    skStyle = SKFontStyleSlant.Oblique;
                    break;
            }

            var key = new FontKey((SKFontStyleWeight)typeface.Weight, skStyle);

            if (typeface.FontFamily.Key == null)
            {
                return GetTypeface(typeface.FontFamily.Name, key);
            }

            var typefaceCollection = SKTypefaceCollectionCache.GetOrAddTypefaceCollection(typeface.FontFamily);

            return typefaceCollection.GetTypeFace(typeface.FontFamily.Name, key) ??
                   GetTypeface(s_defaultFamilyName, key);
        }
    }
}
