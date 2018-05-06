// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Concurrent;

namespace Avalonia.Media.Fonts
{
    public static class FontFamilyCache
    {
        private static readonly ConcurrentDictionary<FontFamilyKey, CachedFontFamily> s_cachedFontFamilies = new ConcurrentDictionary<FontFamilyKey, CachedFontFamily>();

        public static CachedFontFamily GetOrAddFontFamily(FontFamilyKey key)
        {
            return s_cachedFontFamilies.GetOrAdd(key, CreateCachedFontFamily);
        }

        private static CachedFontFamily CreateCachedFontFamily(FontFamilyKey fontFamilyKey)
        {
            return new CachedFontFamily(fontFamilyKey, new FontResourceCollection(fontFamilyKey));
        }
    }
}