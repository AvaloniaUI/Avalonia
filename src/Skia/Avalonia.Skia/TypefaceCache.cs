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
        public static SKTypeface Default = CreateDefaultTypeface();
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

        private static SKTypeface CreateDefaultTypeface()
        {
            var defaultTypeface = SKTypeface.FromFamilyName(FontFamily.Default.Name) ?? SKTypeface.FromFamilyName(null);

            return defaultTypeface;
        }

        private static SKTypeface GetTypeface(string name, FontKey key)
        {
            var familyKey = name;

            if (!Cache.TryGetValue(familyKey, out var entry))
            {
                Cache[familyKey] = entry = new Dictionary<FontKey, SKTypeface>();
            }

            if (!entry.TryGetValue(key, out var typeface))
            {
                typeface = SKTypeface.FromFamilyName(familyKey, key.Weight, SKFontStyleWidth.Normal, key.Slant)
                           ?? Default;

                entry[key] = typeface;
            }

            return typeface;
        }

        public static SKTypeface GetTypeface(string name, FontStyle style, FontWeight weight)
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

            return GetTypeface(name, new FontKey((SKFontStyleWeight)weight, skStyle));
        }

    }
}
