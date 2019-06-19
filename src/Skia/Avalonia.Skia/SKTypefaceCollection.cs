// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

using Avalonia.Media;

using SkiaSharp;

namespace Avalonia.Skia
{
    internal class SKTypefaceCollection
    {
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<FontKey, SKTypeface>> _fontFamilies =
            new ConcurrentDictionary<string, ConcurrentDictionary<FontKey, SKTypeface>>();

        public void AddTypeFace(SKTypeface typeface)
        {
            var key = new FontKey((SKFontStyleWeight)typeface.FontWeight, typeface.FontSlant);

            if (!_fontFamilies.TryGetValue(typeface.FamilyName, out var fontFamily))
            {
                fontFamily = new ConcurrentDictionary<FontKey, SKTypeface>();

                _fontFamilies.TryAdd(typeface.FamilyName, fontFamily);
            }

            fontFamily.TryAdd(key, typeface);
        }

        public SKTypeface GetTypeFace(Typeface typeface)
        {
            var styleSlant = SKFontStyleSlant.Upright;

            switch (typeface.Style)
            {
                case FontStyle.Italic:
                    styleSlant = SKFontStyleSlant.Italic;
                    break;

                case FontStyle.Oblique:
                    styleSlant = SKFontStyleSlant.Oblique;
                    break;
            }

            if (!_fontFamilies.TryGetValue(typeface.FontFamily.Name, out var fontFamily))
            {
                return TypefaceCache.GetTypeface(TypefaceCache.DefaultFamilyName, typeface.Style, typeface.Weight);
            }

            var weight = (SKFontStyleWeight)typeface.Weight;

            var key = new FontKey(weight, styleSlant);

            return fontFamily.GetOrAdd(key, GetFallback(fontFamily, key));
        }

        private static SKTypeface GetFallback(IDictionary<FontKey, SKTypeface> fontFamily, FontKey key)
        {
            var keys = fontFamily.Keys.Where(
                x => ((int)x.Weight <= (int)key.Weight || (int)x.Weight > (int)key.Weight) && x.Slant == key.Slant).ToArray();

            if (!keys.Any())
            {
                keys = fontFamily.Keys.Where(
                    x => x.Weight == key.Weight && (x.Slant >= key.Slant || x.Slant < key.Slant)).ToArray();

                if (!keys.Any())
                {
                    keys = fontFamily.Keys.Where(
                        x => ((int)x.Weight <= (int)key.Weight || (int)x.Weight > (int)key.Weight) &&
                             (x.Slant >= key.Slant || x.Slant < key.Slant)).ToArray();
                }
            }

            key = keys.FirstOrDefault();

            fontFamily.TryGetValue(key, out var typeface);

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
                return other is FontKey key && this.Equals(key);
            }

            private bool Equals(FontKey other)
            {
                return Slant == other.Slant &&
                       Weight == other.Weight;
            }
        }
    }
}
