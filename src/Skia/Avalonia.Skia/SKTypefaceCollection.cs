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
            var key = new FontKey(typeface.FontFamily.Name, typeface.Style, typeface.Weight);

            return GetNearestMatch(_typefaces, key);
        }

        private static SKTypeface GetNearestMatch(IDictionary<FontKey, SKTypeface> typefaces, FontKey key)
        {
            if (typefaces.TryGetValue(new FontKey(key.FamilyName, key.Style, key.Weight), out var typeface))
            {
                return typeface;
            }

            var weight = (int)key.Weight;

            weight -= weight % 100; // make sure we start at a full weight

            for (var i = (int)key.Style; i < 2; i++)
            {
                // only try 2 font weights in each direction
                for (var j = 0; j < 200; j += 100)
                {
                    if (weight - j >= 100)
                    {
                        if (typefaces.TryGetValue(new FontKey(key.FamilyName, (FontStyle)i, (FontWeight)(weight - j)), out typeface))
                        {
                            return typeface;
                        }
                    }

                    if (weight + j > 900)
                    {
                        continue;
                    }

                    if (typefaces.TryGetValue(new FontKey(key.FamilyName, (FontStyle)i, (FontWeight)(weight + j)), out typeface))
                    {
                        return typeface;
                    }
                }
            }

            //Nothing was found so we use the first typeface we can get.
            return typefaces.Values.FirstOrDefault();
        }
    }
}
