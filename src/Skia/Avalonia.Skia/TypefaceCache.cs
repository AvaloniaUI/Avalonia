using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Media;
using SkiaSharp;

namespace Avalonia.Skia
{
    static class TypefaceCache
    {
        static readonly Dictionary<string, Dictionary<FontKey, SKTypeface>> Cache = new Dictionary<string, Dictionary<FontKey, SKTypeface>>();

        struct FontKey
        {
            public readonly SKFontStyleSlant Slant;
            public readonly SKFontStyleWeight Weight;

            public FontKey(SKFontStyleWeight weight, SKFontStyleSlant  slant)
            {
                Slant  = slant;
                Weight = weight;
            }
            
            // Equals and GetHashCode ommitted
        }

        unsafe static SKTypeface GetTypeface(string name, FontKey key)
        {
            if (name == null)
            {
                name = "Arial";
            }

            Dictionary<FontKey, SKTypeface> entry;

            if (!Cache.TryGetValue(name, out entry))
            {
                Cache[name] = entry = new Dictionary<FontKey, SKTypeface>();
            }

            SKTypeface typeface = null;

            if (!entry.TryGetValue(key, out typeface))
            {
                typeface = SKTypeface.FromFamilyName(name, key.Weight, SKFontStyleWidth.Normal, key.Slant);

                if (typeface == null)
                {
                    typeface = SKTypeface.FromFamilyName(null, SKTypefaceStyle.Normal);
                }

                entry[key] = typeface;
            }

            return typeface;
        }

        public static SKTypeface GetTypeface(string name, FontStyle style, FontWeight weight)
        {
            SKFontStyleSlant skStyle = SKFontStyleSlant.Upright;

            switch(style)
            {
                case FontStyle.Italic:
                    skStyle = SKFontStyleSlant.Italic;
                    break;

                case FontStyle.Oblique:
                    skStyle = SKFontStyleSlant.Oblique;
                    break;
            }

            return GetTypeface(name, new FontKey((SKFontStyleWeight)weight, skStyle));
        }

    }
}