using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using SkiaSharp;

namespace Avalonia.Skia
{
    public class SKTypefaceCollection
    {
        private readonly ConcurrentDictionary<Typeface, SKTypeface> _typefaces =
            new ConcurrentDictionary<Typeface, SKTypeface>();

        public void AddTypeface(Typeface key, SKTypeface typeface)
        {
            _typefaces.TryAdd(key, typeface);
        }

        public SKTypeface Get(Typeface typeface)
        {
            return GetNearestMatch(_typefaces, typeface);
        }

        private static SKTypeface GetNearestMatch(IDictionary<Typeface, SKTypeface> typefaces, Typeface key)
        {
            if (typefaces.TryGetValue(key, out var typeface))
            {
                return typeface;
            }
            
            var initialWeight = (int)key.Weight;

            var weight = (int)key.Weight;

            weight -= weight % 50; // make sure we start at a full weight

            for (var i = 0; i < 2; i++)
            {
                for (var j = 0; j < initialWeight; j += 50)
                {
                    if (weight - j >= 100)
                    {
                        if (typefaces.TryGetValue(new Typeface(key.FontFamily, (FontStyle)i, (FontWeight)(weight - j)), out typeface))
                        {
                            return typeface;
                        }
                    }

                    if (weight + j > 900)
                    {
                        continue;
                    }

                    if (typefaces.TryGetValue(new Typeface(key.FontFamily, (FontStyle)i, (FontWeight)(weight + j)), out typeface))
                    {
                        return typeface;
                    }
                }
            }

            //Nothing was found so we try to get a regular typeface.
            return typefaces.TryGetValue(new Typeface(key.FontFamily), out typeface) ? typeface : null;
        }
    }
}
