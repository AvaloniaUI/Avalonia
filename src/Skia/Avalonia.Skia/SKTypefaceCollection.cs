// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using SkiaSharp;

namespace Avalonia.Skia
{
    internal class SKTypefaceCollection
    {
        private readonly ConcurrentDictionary<FontKey, SKTypeface> _typefaces =
            new ConcurrentDictionary<FontKey, SKTypeface>();

        public void AddTypeface(FontKey key, SKTypeface typeface)
        {
            _typefaces.TryAdd(key, typeface);
        }

        public SKTypeface Get(Typeface typeface)
        {
            var key = new FontKey(typeface.FontFamily, typeface.Weight, typeface.Style);

            return GetNearestMatch(_typefaces, key);
        }

        private static SKTypeface GetNearestMatch(IDictionary<FontKey, SKTypeface> typefaces, FontKey key)
        {
            if (typefaces.ContainsKey(key))
            {
                return typefaces[key];
            }

            var keys = typefaces.Keys.Where(
                x => ((int)x.Weight <= (int)key.Weight || (int)x.Weight > (int)key.Weight) && x.Style == key.Style).ToArray();

            if (!keys.Any())
            {
                keys = typefaces.Keys.Where(
                    x => x.Weight == key.Weight && (x.Style >= key.Style || x.Style < key.Style)).ToArray();

                if (!keys.Any())
                {
                    keys = typefaces.Keys.Where(
                        x => ((int)x.Weight <= (int)key.Weight || (int)x.Weight > (int)key.Weight) &&
                             (x.Style >= key.Style || x.Style < key.Style)).ToArray();
                }
            }

            if (keys.Length == 0)
            {
                return SKTypeface.Default;
            }

            key = keys[0];

            return typefaces[key];
        }
    }
}
