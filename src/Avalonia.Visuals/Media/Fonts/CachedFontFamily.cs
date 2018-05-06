// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;

namespace Avalonia.Media.Fonts
{
    public class CachedFontFamily
    {
        private readonly FontResourceCollection _fontResourceCollection;

        public CachedFontFamily(FontFamilyKey key, FontResourceCollection fontResourceCollection)
        {
            Key = key;
            _fontResourceCollection = fontResourceCollection;
        }

        public FontFamilyKey Key { get; }

        public IEnumerable<FontResource> FontResources => _fontResourceCollection.FontResources;
    }
}