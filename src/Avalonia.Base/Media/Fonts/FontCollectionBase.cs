using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using Avalonia.Platform;
using Avalonia.Utilities;

namespace Avalonia.Media.Fonts
{
    public abstract class FontCollectionBase : IFontCollection
    {
        protected readonly ConcurrentDictionary<string, ConcurrentDictionary<FontCollectionKey, GlyphTypeface?>> _glyphTypefaceCache = new();

        public abstract Uri Key { get; }

        public abstract int Count { get; }

        public abstract FontFamily this[int index] { get; }

        public abstract bool TryGetGlyphTypeface(string familyName, FontStyle style, FontWeight weight, FontStretch stretch,
           [NotNullWhen(true)] out GlyphTypeface? glyphTypeface);

        public virtual bool TryMatchCharacter(int codepoint, FontStyle style, FontWeight weight, FontStretch stretch,
            string? familyName, CultureInfo? culture, out Typeface match)
        {
            match = default;
        
            //If a font family is defined we try to find a match inside that family first
            if (familyName != null && _glyphTypefaceCache.TryGetValue(familyName, out var glyphTypefaces))
            {
                if (TryGetNearestMatch(glyphTypefaces, new FontCollectionKey { Style = style, Weight = weight, Stretch = stretch }, out var glyphTypeface))
                {
                    if (glyphTypeface.CharacterToGlyphMap.TryGetValue(codepoint, out _))
                    {
                        match = new Typeface(new FontFamily(null, Key.AbsoluteUri + "#" + glyphTypeface.FamilyName), style, weight, stretch);

                        return true;
                    }
                }
            }

            //Try to find a match in any font family
            foreach (var pair in _glyphTypefaceCache)
            {
                if(pair.Key == familyName)
                {
                    //We already tried this before
                    continue;
                }

                glyphTypefaces = pair.Value;

                if (TryGetNearestMatch(glyphTypefaces, new FontCollectionKey { Style = style, Weight = weight, Stretch = stretch }, out var glyphTypeface))
                {
                    if (glyphTypeface.CharacterToGlyphMap.TryGetValue(codepoint, out _))
                    {
                        match = new Typeface(new FontFamily(null, Key.AbsoluteUri + "#" + glyphTypeface.FamilyName), style, weight, stretch);

                        return true;
                    }
                }
            }

            return false;
        }

        public virtual bool TryCreateSyntheticGlyphTypeface(
            GlyphTypeface glyphTypeface,
            FontStyle style, 
            FontWeight weight, 
            FontStretch stretch, 
            [NotNullWhen(true)] out GlyphTypeface? syntheticGlyphTypeface)
        {
            syntheticGlyphTypeface = null;

            //Source family should be present in the cache.
            if (!_glyphTypefaceCache.TryGetValue(glyphTypeface.FamilyName, out var glyphTypefaces))
            {
                return false;
            }

            var fontManager = FontManager.Current.PlatformImpl;

            var key = new FontCollectionKey(style, weight, stretch);

            var currentKey =
                new FontCollectionKey(glyphTypeface.Style, glyphTypeface.Weight, glyphTypeface.Stretch);

            if (currentKey == key)
            {
                return false;
            }

            var fontSimulations = FontSimulations.None;

            if (style != FontStyle.Normal && glyphTypeface.Style != style)
            {
                fontSimulations |= FontSimulations.Oblique;
            }

            if ((int)weight >= 600 && glyphTypeface.Weight < weight)
            {
                fontSimulations |= FontSimulations.Bold;
            }

            if (fontSimulations != FontSimulations.None && glyphTypeface.PlatformTypeface.TryGetStream(out var stream))
            {
                using (stream)
                {
                    if (fontManager.TryCreateGlyphTypeface(stream, fontSimulations, out var platformTypeface))
                    {
                        syntheticGlyphTypeface = new GlyphTypeface(platformTypeface, fontSimulations);

                        //Add the TypographicFamilyName to the cache
                        if (!string.IsNullOrEmpty(glyphTypeface.TypographicFamilyName))
                        {
                            AddGlyphTypefaceByFamilyName(glyphTypeface.TypographicFamilyName, syntheticGlyphTypeface);
                        }

                        foreach (var kvp in glyphTypeface.FamilyNames)
                        {
                            AddGlyphTypefaceByFamilyName(kvp.Value, syntheticGlyphTypeface);
                        }

                        return true;
                    }

                    return false;
                }
            }

            return false;

            void AddGlyphTypefaceByFamilyName(string familyName, GlyphTypeface glyphTypeface)
            {
                var typefaces = _glyphTypefaceCache.GetOrAdd(familyName,
                    x =>
                    {
                        return new ConcurrentDictionary<FontCollectionKey, GlyphTypeface?>();
                    });

                typefaces.TryAdd(key, glyphTypeface);
            }
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
            GlyphTypeface?> glyphTypefaces,
            FontCollectionKey key,
            [NotNullWhen(true)] out GlyphTypeface? glyphTypeface)
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
            GlyphTypeface?> glyphTypefaces,
            FontCollectionKey key,
            [NotNullWhen(true)] out GlyphTypeface? glyphTypeface)
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
            GlyphTypeface?> glyphTypefaces,
            FontCollectionKey key,
            [NotNullWhen(true)] out GlyphTypeface? glyphTypeface)
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

            StringBuilder? normalizedFamilyNameBuilder = null;
            var totalCharsRemoved = 0;

            var tokenizer = new SpanStringTokenizer(normalizedFamilyName, ' ');

            // Skip initial family name.
            tokenizer.ReadSpan();

            while (tokenizer.TryReadSpan(out var token))
            {
                // Don't try to match numbers.
                if (new SpanStringTokenizer(token).TryReadInt32(out _))
                {
                    continue;
                }

                // Try match with font style, weight or stretch and update accordingly.
                var match = false;
                if (EnumHelper.TryParse<FontStyle>(token, true, out var newStyle))
                {
                    style = newStyle;
                    match = true;
                }
                else if (EnumHelper.TryParse<FontWeight>(token, true, out var newWeight))
                {
                    weight = newWeight;
                    match = true;
                }
                else if (EnumHelper.TryParse<FontStretch>(token, true, out var newStretch))
                {
                    stretch = newStretch;
                    match = true;
                }

                if (match)
                {
                    // Carve out matched word from the normalized name.
                    normalizedFamilyNameBuilder ??= new StringBuilder(normalizedFamilyName);
                    normalizedFamilyNameBuilder.Remove(tokenizer.CurrentTokenIndex - totalCharsRemoved, token.Length);
                    totalCharsRemoved += token.Length;
                }
            }

            // Get rid of any trailing spaces.
            normalizedFamilyName = (normalizedFamilyNameBuilder?.ToString() ?? normalizedFamilyName).TrimEnd();

            //Preserve old font source
            return new Typeface(typeface.FontFamily, style, weight, stretch);
        }
    }
}
