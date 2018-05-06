// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Avalonia.Media.Fonts
{
    public class FontResourceCollection
    {
        private Dictionary<Uri, FontResource> _fontResources;
        private readonly IFontResourceLoader _fontResourceLoader = new FontResourceLoader();

        public FontResourceCollection(FontFamilyKey key)
        {
            Key = key;
        }

        public FontFamilyKey Key { get; }

        public IEnumerable<FontResource> FontResources
        {
            get
            {
                if (_fontResources == null)
                {
                    _fontResources = CreateFontResources();
                }

                return _fontResources.Values;
            }
        }

        private Dictionary<Uri, FontResource> CreateFontResources()
        {
            return _fontResourceLoader.GetFontResources(Key).ToDictionary(x => x.Source);
        }
    }
}