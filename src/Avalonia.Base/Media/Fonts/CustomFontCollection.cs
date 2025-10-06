using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using Avalonia.Platform;

namespace Avalonia.Media.Fonts
{
    public class CustomFontCollection : FontCollectionBase, IFontCollection2
    {
        private readonly List<FontFamily> _fontFamilies = [];

        public CustomFontCollection(Uri key)
        {
            Key = key;
        }

        public override Uri Key { get; }

        public override FontFamily this[int index] => _fontFamilies[index];

        public override int Count => _fontFamilies.Count;

        public override void Initialize(IFontManagerImpl fontManager)
        {
            // Nothing to do
        }

        public override IEnumerator<FontFamily> GetEnumerator() => _fontFamilies.GetEnumerator();

        public override bool TryGetGlyphTypeface(string familyName, FontStyle style, FontWeight weight, FontStretch stretch, [NotNullWhen(true)] out IGlyphTypeface? glyphTypeface)
        {
            var typeface = new Typeface(familyName, style, weight, stretch).Normalize(out familyName);

            style = typeface.Style;

            weight = typeface.Weight;

            stretch = typeface.Stretch;

            var key = new FontCollectionKey(style, weight, stretch);

            if (_glyphTypefaceCache.TryGetValue(familyName, out var glyphTypefaces))
            {
                if (glyphTypefaces.TryGetValue(key, out glyphTypeface) && glyphTypeface != null)
                {
                    return true;
                }

                if (TryGetNearestMatch(glyphTypefaces, key, out glyphTypeface))
                {
                    var matchedKey = new FontCollectionKey(glyphTypeface.Style, glyphTypeface.Weight, glyphTypeface.Stretch);

                    if (matchedKey != key)
                    {
                        if (TryCreateSyntheticGlyphTypeface(glyphTypeface, style, weight, stretch, out var syntheticGlyphTypeface))
                        {
                            glyphTypeface = syntheticGlyphTypeface;
                        }
                    }

                    return true;
                }
            }

            //Try to find a partially matching font
            for (var i = 0; i < Count; i++)
            {
                var fontFamily = _fontFamilies[i];

                if (fontFamily.Name.ToLower(CultureInfo.InvariantCulture).StartsWith(familyName.ToLower(CultureInfo.InvariantCulture)))
                {
                    if (_glyphTypefaceCache.TryGetValue(fontFamily.Name, out glyphTypefaces) &&
                        TryGetNearestMatch(glyphTypefaces, key, out glyphTypeface))
                    {
                        return true;
                    }
                }
            }

            glyphTypeface = null;

            return false;
        }

        public bool TryAddGlyphTypeface(IGlyphTypeface glyphTypeface)
        {
            if (glyphTypeface == null || string.IsNullOrEmpty(glyphTypeface.FamilyName))
            {
                return false;
            }

            var familyName = glyphTypeface.FamilyName;

            if (!_glyphTypefaceCache.TryGetValue(familyName, out var glyphTypefaces))
            {
                glyphTypefaces = _glyphTypefaceCache.GetOrAdd(familyName,
                    (_) => new System.Collections.Concurrent.ConcurrentDictionary<FontCollectionKey, IGlyphTypeface?>());

                _fontFamilies.Add(new FontFamily(Key + "#" + familyName));
            }
            var key = new FontCollectionKey(glyphTypeface.Style, glyphTypeface.Weight, glyphTypeface.Stretch);

            return glyphTypefaces.TryAdd(key, glyphTypeface);
        }

        public bool TryAddGlyphTypeface(Stream stream) 
        {
            var fontManager = FontManager.Current?.PlatformImpl;

            if(fontManager == null)
            {
                return false;
            }

            if(!fontManager.TryCreateGlyphTypeface(stream, FontSimulations.None, out var glyphTypeface))
            {
                return false;
            }

            return TryAddGlyphTypeface(glyphTypeface);
        }

        public bool TryGetFamilyTypefaces(string familyName, [NotNullWhen(true)] out IReadOnlyList<Typeface>? familyTypefaces)
        {
            familyTypefaces = null;

            if (_glyphTypefaceCache.TryGetValue(familyName, out var glyphTypefaces))
            {
                var typefaces = new List<Typeface>(glyphTypefaces.Count);

                foreach (var key in glyphTypefaces.Keys)
                {
                    typefaces.Add(new Typeface(new FontFamily(Key + "#" + familyName), key.Style, key.Weight, key.Stretch));
                }

                familyTypefaces = typefaces;

                return true;
            }

            return false;
        }
    }
}
