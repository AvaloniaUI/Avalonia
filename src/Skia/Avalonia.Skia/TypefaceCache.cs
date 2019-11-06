// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Concurrent;
using Avalonia.Media;
using SkiaSharp;

namespace Avalonia.Skia
{
    /// <summary>
    /// Cache for Skia typefaces.
    /// </summary>
    internal static class TypefaceCache
    {
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<FontKey, TypefaceCollectionEntry>> s_cache =
            new ConcurrentDictionary<string, ConcurrentDictionary<FontKey, TypefaceCollectionEntry>>();

        public static TypefaceCollectionEntry Get(FontFamily fontFamily, FontWeight fontWeight, FontStyle fontStyle)
        {
            if (fontFamily.Key != null)
            {
                return SKTypefaceCollectionCache.GetOrAddTypefaceCollection(fontFamily)
                    .Get(fontFamily.Name, fontWeight, fontStyle);
            }

            var typefaceCollection = s_cache.GetOrAdd(fontFamily.Name, new ConcurrentDictionary<FontKey, TypefaceCollectionEntry>());

            var key = new FontKey(fontWeight, fontStyle);

            if (typefaceCollection.TryGetValue(key, out var entry))
            {
                return entry;
            }

            var skTypeface = SKTypeface.FromFamilyName(fontFamily.Name, (SKFontStyleWeight)fontWeight,
                                 SKFontStyleWidth.Normal, (SKFontStyleSlant)fontStyle) ?? SKTypeface.Default;

            var typeface = new Typeface(fontFamily.Name, fontWeight, fontStyle);

            entry = new TypefaceCollectionEntry(typeface, skTypeface);

            typefaceCollection[key] = entry;

            return entry;
        }
    }
}
