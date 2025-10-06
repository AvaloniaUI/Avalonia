using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Avalonia.Platform;

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

        public virtual bool TryMatchCharacter(int codepoint, FontStyle style, FontWeight weight, FontStretch stretch,
            string? familyName, CultureInfo? culture, out Typeface match)
        {
            match = default;
        
            //If a font family is defined we try to find a match inside that family first
            if (familyName != null && _glyphTypefaceCache.TryGetValue(familyName, out var glyphTypefaces))
            {
                if (TryGetNearestMatch(glyphTypefaces, new FontCollectionKey { Style = style, Weight = weight, Stretch = stretch }, out var glyphTypeface))
                {
                    if (glyphTypeface.TryGetGlyph((uint)codepoint, out _))
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
                    if (glyphTypeface.TryGetGlyph((uint)codepoint, out _))
                    {
                        match = new Typeface(new FontFamily(null, Key.AbsoluteUri + "#" + glyphTypeface.FamilyName), style, weight, stretch);

                        return true;
                    }
                }
            }

            return false;
        }

        public virtual bool TryCreateSyntheticGlyphTypeface(
            IGlyphTypeface glyphTypeface,
            FontStyle style, 
            FontWeight weight, 
            FontStretch stretch, 
            [NotNullWhen(true)] out IGlyphTypeface? syntheticGlyphTypeface)
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

            if (glyphTypeface is not IGlyphTypeface2 glyphTypeface2)
            {
                return false;
            }

            var fontSimulations = FontSimulations.None;

            if (style != FontStyle.Normal && glyphTypeface2.Style != style)
            {
                fontSimulations |= FontSimulations.Oblique;
            }

            if ((int)weight >= 600 && glyphTypeface2.Weight < weight)
            {
                fontSimulations |= FontSimulations.Bold;
            }

            if (fontSimulations != FontSimulations.None && glyphTypeface2.TryGetStream(out var stream))
            {
                using (stream)
                {
                    if (fontManager.TryCreateGlyphTypeface(stream, fontSimulations, out syntheticGlyphTypeface))
                    {
                        //Add the TypographicFamilyName to the cache
                        if (!string.IsNullOrEmpty(glyphTypeface2.TypographicFamilyName))
                        {
                            AddGlyphTypefaceByFamilyName(glyphTypeface2.TypographicFamilyName, syntheticGlyphTypeface);
                        }

                        foreach (var kvp in glyphTypeface2.FamilyNames)
                        {
                            AddGlyphTypefaceByFamilyName(kvp.Value, syntheticGlyphTypeface);
                        }

                        return true;
                    }

                    return false;
                }
            }

            return false;

            void AddGlyphTypefaceByFamilyName(string familyName, IGlyphTypeface glyphTypeface)
            {
                var typefaces = _glyphTypefaceCache.GetOrAdd(familyName,
                    x =>
                    {
                        return new ConcurrentDictionary<FontCollectionKey, IGlyphTypeface?>();
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
      
        /// <summary>
        /// Attempts to retrieve the glyph typeface that most closely matches the specified font family name, style,
        /// weight, and stretch.
        /// </summary>
        /// <remarks>This method searches for a glyph typeface in the font collection cache that matches
        /// the specified parameters. If an exact match is not found, fallback mechanisms are applied to find the
        /// closest available match based on the specified style, weight, and stretch. If no suitable match is found,
        /// the method returns <see langword="false"/> and <paramref name="glyphTypeface"/> is set to <see
        /// langword="null"/>.</remarks>
        /// <param name="familyName">The name of the font family to search for. This parameter cannot be <see langword="null"/> or empty.</param>
        /// <param name="style">The desired font style.</param>
        /// <param name="weight">The desired font weight.</param>
        /// <param name="stretch">The desired font stretch.</param>
        /// <param name="glyphTypeface">When this method returns, contains the <see cref="IGlyphTypeface"/> that most closely matches the specified
        /// parameters, if a match is found; otherwise, <see langword="null"/>. This parameter is passed uninitialized.</param>
        /// <returns><see langword="true"/> if a matching glyph typeface is found; otherwise, <see langword="false"/>.</returns>
        public bool TryGetNearestMatch(string familyName, FontStyle style, FontWeight weight, FontStretch stretch, [NotNullWhen(true)] out IGlyphTypeface? glyphTypeface)
        {
            if(!_glyphTypefaceCache.TryGetValue(familyName, out var glyphTypefaces))
            {
                glyphTypeface = null;

                return false;
            }

            var key = new FontCollectionKey { Style = style, Weight = weight, Stretch = stretch };

            return TryGetNearestMatch(glyphTypefaces, key, out glyphTypeface);
        }

        protected bool TryGetNearestMatch(IDictionary<FontCollectionKey, IGlyphTypeface?> glyphTypefaces, FontCollectionKey key, [NotNullWhen(true)] out IGlyphTypeface? glyphTypeface)
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
                if (typeface != null)
                {
                    glyphTypeface = typeface;

                    return true;
                }
            }

            return false;
        }

        internal static bool TryFindStretchFallback(
            IDictionary<FontCollectionKey, IGlyphTypeface?> glyphTypefaces,
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
            IDictionary<FontCollectionKey, IGlyphTypeface?> glyphTypefaces,
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
    }
}
