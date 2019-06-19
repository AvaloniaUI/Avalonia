// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

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

        public SKTypeface GetTypeFace(string familyName, FontKey key)
        {
            return _fontFamilies.TryGetValue(familyName, out var fontFamily) ?
                fontFamily.GetOrAdd(key, GetFallback(fontFamily, key)) :
                null;
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
    }
}
