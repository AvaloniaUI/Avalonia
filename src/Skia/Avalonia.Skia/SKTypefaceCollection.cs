// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;

namespace Avalonia.Skia
{
    internal class SKTypefaceCollection
    {
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<FontKey, TypefaceCollectionEntry>> _fontFamilies =
            new ConcurrentDictionary<string, ConcurrentDictionary<FontKey, TypefaceCollectionEntry>>();

        public void AddEntry(string familyName, FontKey key, TypefaceCollectionEntry entry)
        {
            if (!_fontFamilies.TryGetValue(familyName, out var fontFamily))
            {
                fontFamily = new ConcurrentDictionary<FontKey, TypefaceCollectionEntry>();

                _fontFamilies.TryAdd(familyName, fontFamily);
            }

            fontFamily.TryAdd(key, entry);
        }

        public TypefaceCollectionEntry Get(string familyName, FontWeight fontWeight, FontStyle fontStyle)
        {
            var key = new FontKey(fontWeight, fontStyle);

            return _fontFamilies.TryGetValue(familyName, out var fontFamily) ?
                fontFamily.GetOrAdd(key, GetFallback(fontFamily, key)) :
                new TypefaceCollectionEntry(Typeface.Default, SkiaSharp.SKTypeface.Default);
        }

        private static TypefaceCollectionEntry GetFallback(IDictionary<FontKey, TypefaceCollectionEntry> fontFamily, FontKey key)
        {
            var keys = fontFamily.Keys.Where(
                x => ((int)x.Weight <= (int)key.Weight || (int)x.Weight > (int)key.Weight) && x.Style == key.Style).ToArray();

            if (!keys.Any())
            {
                keys = fontFamily.Keys.Where(
                    x => x.Weight == key.Weight && (x.Style >= key.Style || x.Style < key.Style)).ToArray();

                if (!keys.Any())
                {
                    keys = fontFamily.Keys.Where(
                        x => ((int)x.Weight <= (int)key.Weight || (int)x.Weight > (int)key.Weight) &&
                             (x.Style >= key.Style || x.Style < key.Style)).ToArray();
                }
            }

            key = keys.FirstOrDefault();

            fontFamily.TryGetValue(key, out var entry);

            return entry;
        }
    }
}
