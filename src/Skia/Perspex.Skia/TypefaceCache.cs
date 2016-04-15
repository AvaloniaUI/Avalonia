using System;
using System.Collections.Generic;
using System.Text;
using Perspex.Media;
using SkiaSharp;

namespace Perspex.Skia
{
    static class TypefaceCache
    {
        static readonly Dictionary<string, Dictionary<SKTypefaceStyle, SKTypeface>> Cache = new Dictionary<string, Dictionary<SKTypefaceStyle, SKTypeface>>();

        unsafe static SKTypeface GetTypeface(string name, SKTypefaceStyle style)
        {
            if (name == null)
                name = "Arial";

            Dictionary<SKTypefaceStyle, SKTypeface> entry;
            if (!Cache.TryGetValue(name, out entry))
                Cache[name] = entry = new Dictionary<SKTypefaceStyle, SKTypeface>();

            SKTypeface typeface = null;
            if (!entry.TryGetValue(style, out typeface))
            {
                typeface = SKTypeface.FromFamilyName(name, style);
                if (typeface == null)
                {
                    typeface = SKTypeface.FromFamilyName(null, style);
                }
                entry[style] = typeface;
            }
            return typeface;
        }

        public static SKTypeface GetTypeface(string name, FontStyle style, FontWeight weight)
        {
            SKTypefaceStyle sstyle = SKTypefaceStyle.Normal;
            if (style != FontStyle.Normal)
                sstyle |= SKTypefaceStyle.Italic;

            if (weight > FontWeight.Normal)
                sstyle |= SKTypefaceStyle.Bold;

            return GetTypeface(name, sstyle);
        }

    }
}