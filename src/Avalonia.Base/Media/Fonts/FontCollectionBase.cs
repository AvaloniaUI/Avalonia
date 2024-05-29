﻿using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Avalonia.Platform;
using Avalonia.Utilities;

namespace Avalonia.Media.Fonts
{
    public abstract class FontCollectionBase : IFontCollection
    {
        protected readonly ConcurrentDictionary<string, ConcurrentDictionary<FontCollectionKey, IGlyphTypeface?>> _glyphTypefaceCache = new();

        public abstract Uri Key { get; }

        public abstract int Count { get; }

        public abstract FontFamily this[int index] { get; }

        public abstract bool TryGetGlyphTypeface(string familyName, FontStyle style, FontWeight weight, FontStretch stretch,
           [NotNullWhen(true)] out IGlyphTypeface? glyphTypeface);

        public bool TryMatchCharacter(int codepoint, FontStyle style, FontWeight weight, FontStretch stretch,
            string? familyName, CultureInfo? culture, out Typeface match)
        {
            match = default;

            if (string.IsNullOrEmpty(familyName))
            {
                foreach (var typefaces in _glyphTypefaceCache.Values)
                {
                    if (TryGetNearestMatch(typefaces, new FontCollectionKey { Style = style, Weight = weight, Stretch = stretch }, out var glyphTypeface))
                    {
                        if (glyphTypeface.TryGetGlyph((uint)codepoint, out _))
                        {
                            match = new Typeface(Key.AbsoluteUri + "#" + glyphTypeface.FamilyName, style, weight, stretch);

                            return true;
                        }
                    }
                }
            }
            else
            {
                if (TryGetGlyphTypeface(familyName, style, weight, stretch, out var glyphTypeface))
                {
                    if (glyphTypeface.FamilyName.Contains(familyName) && glyphTypeface.TryGetGlyph((uint)codepoint, out _))
                    {
                        match = new Typeface(Key.AbsoluteUri + "#" + familyName, style, weight, stretch);

                        return true;
                    }
                }
            }

            return false;
        }

        public abstract void Initialize(IFontManagerImpl fontManager);

        public abstract IEnumerator<FontFamily> GetEnumerator();

        void IDisposable.Dispose()
        {
            foreach (var glyphTypefaces in _glyphTypefaceCache.Values)
            {
                foreach (var pair in glyphTypefaces)
                {
                    pair.Value?.Dispose();
                }
            }

            GC.SuppressFinalize(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        internal static bool TryGetNearestMatch(
            ConcurrentDictionary<FontCollectionKey,
            IGlyphTypeface?> glyphTypefaces,
            FontCollectionKey key,
            [NotNullWhen(true)] out IGlyphTypeface? glyphTypeface)
        {
            if (glyphTypefaces.TryGetValue(key, out glyphTypeface) && glyphTypeface != null)
            {
                return true;
            }

            if (key.Style != FontStyle.Normal)
            {
                key = key with { Style = FontStyle.Normal };
            }

            if (key.Stretch != FontStretch.Normal)
            {
                if (TryFindStretchFallback(glyphTypefaces, key, out glyphTypeface))
                {
                    return true;
                }

                if (key.Weight != FontWeight.Normal)
                {
                    if (TryFindStretchFallback(glyphTypefaces, key with { Weight = FontWeight.Normal }, out glyphTypeface))
                    {
                        return true;
                    }
                }

                key = key with { Stretch = FontStretch.Normal };
            }

            if (TryFindWeightFallback(glyphTypefaces, key, out glyphTypeface))
            {
                return true;
            }

            if (TryFindStretchFallback(glyphTypefaces, key, out glyphTypeface))
            {
                return true;
            }

            //Take the first glyph typeface we can find.
            foreach (var typeface in glyphTypefaces.Values)
            {
                if(typeface != null)
                {
                    glyphTypeface = typeface;

                    return true;
                }            
            }

            return false;
        }

        internal static bool TryFindStretchFallback(
            ConcurrentDictionary<FontCollectionKey,
            IGlyphTypeface?> glyphTypefaces,
            FontCollectionKey key,
            [NotNullWhen(true)] out IGlyphTypeface? glyphTypeface)
        {
            glyphTypeface = null;

            var stretch = (int)key.Stretch;

            if (stretch < 5)
            {
                for (var i = 0; stretch + i < 9; i++)
                {
                    if (glyphTypefaces.TryGetValue(key with { Stretch = (FontStretch)(stretch + i) }, out glyphTypeface) && glyphTypeface != null)
                    {
                        return true;
                    }
                }
            }
            else
            {
                for (var i = 0; stretch - i > 1; i++)
                {
                    if (glyphTypefaces.TryGetValue(key with { Stretch = (FontStretch)(stretch - i) }, out glyphTypeface) && glyphTypeface != null)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        internal static bool TryFindWeightFallback(
            ConcurrentDictionary<FontCollectionKey,
            IGlyphTypeface?> glyphTypefaces,
            FontCollectionKey key,
            [NotNullWhen(true)] out IGlyphTypeface? glyphTypeface)
        {
            glyphTypeface = null;
            var weight = (int)key.Weight;

            //If the target weight given is between 400 and 500 inclusive          
            if (weight >= 400 && weight <= 500)
            {
                //Look for available weights between the target and 500, in ascending order.
                for (var i = 0; weight + i <= 500; i += 50)
                {
                    if (glyphTypefaces.TryGetValue(key with { Weight = (FontWeight)(weight + i) }, out glyphTypeface) && glyphTypeface != null)
                    {
                        return true;
                    }
                }

                //If no match is found, look for available weights less than the target, in descending order.
                for (var i = 0; weight - i >= 100; i += 50)
                {
                    if (glyphTypefaces.TryGetValue(key with { Weight = (FontWeight)(weight - i) }, out glyphTypeface) && glyphTypeface != null)
                    {
                        return true;
                    }
                }

                //If no match is found, look for available weights greater than 500, in ascending order.
                for (var i = 0; weight + i <= 900; i += 50)
                {
                    if (glyphTypefaces.TryGetValue(key with { Weight = (FontWeight)(weight + i) }, out glyphTypeface) && glyphTypeface != null)
                    {
                        return true;
                    }
                }
            }

            //If a weight less than 400 is given, look for available weights less than the target, in descending order.           
            if (weight < 400)
            {
                for (var i = 0; weight - i >= 100; i += 50)
                {
                    if (glyphTypefaces.TryGetValue(key with { Weight = (FontWeight)(weight - i) }, out glyphTypeface) && glyphTypeface != null)
                    {
                        return true;
                    }
                }

                //If no match is found, look for available weights less than the target, in descending order.
                for (var i = 0; weight + i <= 900; i += 50)
                {
                    if (glyphTypefaces.TryGetValue(key with { Weight = (FontWeight)(weight + i) }, out glyphTypeface) && glyphTypeface != null)
                    {
                        return true;
                    }
                }
            }

            //If a weight greater than 500 is given, look for available weights greater than the target, in ascending order.
            if (weight > 500)
            {
                for (var i = 0; weight + i <= 900; i += 50)
                {
                    if (glyphTypefaces.TryGetValue(key with { Weight = (FontWeight)(weight + i) }, out glyphTypeface) && glyphTypeface != null)
                    {
                        return true;
                    }
                }

                //If no match is found, look for available weights less than the target, in descending order.
                for (var i = 0; weight - i >= 100; i += 50)
                {
                    if (glyphTypefaces.TryGetValue(key with { Weight = (FontWeight)(weight - i) }, out glyphTypeface) && glyphTypeface != null)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        internal static Typeface GetImplicitTypeface(Typeface typeface, out string normalizedFamilyName)
        {
            normalizedFamilyName = typeface.FontFamily.FamilyNames.PrimaryFamilyName;

            //Return early if no separator is present.
            if (!normalizedFamilyName.Contains(' '))
            {
                return typeface;
            }

            var style = typeface.Style;
            var weight = typeface.Weight;
            var stretch = typeface.Stretch;

            if(TryGetStyle(ref normalizedFamilyName, out var foundStyle))
            {
                style = foundStyle;
            }

            if(TryGetWeight(ref normalizedFamilyName, out var foundWeight))
            {
                weight = foundWeight;
            }

            if(TryGetStretch(ref normalizedFamilyName, out var foundStretch))
            {
                stretch = foundStretch;
            }

            //Preserve old font source
            return new Typeface(typeface.FontFamily, style, weight, stretch);

        }

        internal static bool TryGetWeight(ref string familyName, out FontWeight weight)
        {
            weight = FontWeight.Normal;

            var tokenizer = new StringTokenizer(familyName, ' ');

            tokenizer.ReadString();

            while (tokenizer.TryReadString(out var weightString))
            {
                if (new StringTokenizer(weightString).TryReadInt32(out _))
                {
                    continue;
                }

                if (!Enum.TryParse(weightString, true, out weight))
                {
                    continue;
                }

                familyName = familyName.Replace(" " + weightString, "").TrimEnd();

                return true;
            }

            return false;
        }

        internal static bool TryGetStyle(ref string familyName, out FontStyle style)
        {
            style = FontStyle.Normal;

            var tokenizer = new StringTokenizer(familyName, ' ');

            tokenizer.ReadString();

            while (tokenizer.TryReadString(out var styleString))
            {
                //Do not try to parse an integer
                if (new StringTokenizer(styleString).TryReadInt32(out _))
                {
                    continue;
                }

                if (!Enum.TryParse(styleString, true, out style))
                {
                    continue;
                }

                familyName = familyName.Replace(" " + styleString, "").TrimEnd();

                return true;
            }

            return false;
        }

        internal static bool TryGetStretch(ref string familyName, out FontStretch stretch)
        {
            stretch = FontStretch.Normal;

            var tokenizer = new StringTokenizer(familyName, ' ');

            tokenizer.ReadString();

            while (tokenizer.TryReadString(out var stretchString))
            {
                if (new StringTokenizer(stretchString).TryReadInt32(out _))
                {
                    continue;
                }

                if (!Enum.TryParse(stretchString, true, out stretch))
                {
                    continue;
                }

                familyName = familyName.Replace(" " + stretchString, "").TrimEnd();

                return true;
            }

            return false;
        }
    }
}
