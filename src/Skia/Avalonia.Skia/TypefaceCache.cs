// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
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
        private static readonly ConcurrentDictionary<FontFamily, ConcurrentDictionary<FontKey, TypefaceCollectionEntry>> s_cache =
            new ConcurrentDictionary<FontFamily, ConcurrentDictionary<FontKey, TypefaceCollectionEntry>>();

        public static TypefaceCollectionEntry Get(FontFamily fontFamily, FontWeight fontWeight, FontStyle fontStyle)
        {
            if (fontFamily.Key != null)
            {
                return SKTypefaceCollectionCache.GetOrAddTypefaceCollection(fontFamily)
                    .Get(fontFamily.Name, fontWeight, fontStyle);
            }

            var typefaceCollection = s_cache.GetOrAdd(fontFamily, new ConcurrentDictionary<FontKey, TypefaceCollectionEntry>());

            var key = new FontKey(fontWeight, fontStyle);

            if (typefaceCollection.TryGetValue(key, out var entry))
            {
                return entry;
            }

            SKTypeface skTypeface = null;

            if (fontFamily.FamilyNames.HasFallbacks)
            {
                foreach (var currentName in fontFamily.FamilyNames)
                {
                    var sktf = SKTypeface.FromFamilyName(currentName, (SKFontStyleWeight)fontWeight,
                                 SKFontStyleWidth.Normal, (SKFontStyleSlant)fontStyle);
                    if (currentName.Equals(sktf.FamilyName, StringComparison.OrdinalIgnoreCase) ||
                        sktf.FamilyName != SKTypeface.Default.FamilyName)
                    {
                        skTypeface = sktf;
                        break;
                    }
                }
            }

            skTypeface = skTypeface ?? SKTypeface.FromFamilyName(fontFamily.Name, (SKFontStyleWeight)fontWeight,
                                 SKFontStyleWidth.Normal, (SKFontStyleSlant)fontStyle) ?? SKTypeface.Default;

            var typeface = new Typeface(fontFamily, fontWeight, fontStyle);

            entry = new TypefaceCollectionEntry(typeface, skTypeface);

            typefaceCollection[key] = entry;

            return entry;
        }
    }
}
