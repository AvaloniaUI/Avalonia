// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Concurrent;

namespace Avalonia.Media.Fonts
{
    /// <summary>
    /// Caches all <see cref="CachedFontFamily"/> instances to reduce memory usage and speed up loading times of custom font families
    /// </summary>
    public static class FontFamilyCache
    {
        private static readonly ConcurrentDictionary<FontFamilyKey, CachedFontFamily> s_cachedFontFamilies = new ConcurrentDictionary<FontFamilyKey, CachedFontFamily>();

        /// <summary>
        /// Gets the or add cached font family.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public static CachedFontFamily GetOrAddFontFamily(FontFamilyKey key)
        {
            return s_cachedFontFamilies.GetOrAdd(key, CreateCachedFontFamily);
        }

        /// <summary>
        /// Creates the cached font family.
        /// </summary>
        /// <param name="fontFamilyKey">The font family key.</param>
        /// <returns></returns>
        private static CachedFontFamily CreateCachedFontFamily(FontFamilyKey fontFamilyKey)
        {
            return new CachedFontFamily(fontFamilyKey, new FontResourceCollection(fontFamilyKey));
        }
    }
}