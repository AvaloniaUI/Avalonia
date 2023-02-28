using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Avalonia.Platform;
using Avalonia.Utilities;

namespace Avalonia.Media.Fonts
{
    public class EmbeddedFontCollection : IFontCollection
    {
        private readonly Dictionary<string, Dictionary<FontCollectionKey, IGlyphTypeface>> _glyphTypefaceCache =
            new Dictionary<string, Dictionary<FontCollectionKey, IGlyphTypeface>>();

        private readonly List<FontFamily> _fontFamilies = new List<FontFamily>(1);

        private readonly Uri _key;

        private readonly Uri _source;

        public EmbeddedFontCollection(Uri key, Uri source)
        {
            _key = key;

            if(!source.IsAvares() && !source.IsAbsoluteResm())
            {
                throw new ArgumentOutOfRangeException(nameof(source), "Specified source uri does not follow the resm: or avares: scheme.");
            }

            _source = source;
        }

        public Uri Key => _key;

        public FontFamily this[int index] => _fontFamilies[index];

        public int Count => _fontFamilies.Count;

        public void Initialize(IFontManagerImpl fontManager)
        {
            var assetLoader = AvaloniaLocator.Current.GetRequiredService<IAssetLoader>();

            var fontAssets = FontFamilyLoader.LoadFontAssets(_source);

            foreach (var fontAsset in fontAssets)
            {
                var stream = assetLoader.Open(fontAsset);

                if (fontManager.TryCreateGlyphTypeface(stream, out var glyphTypeface))
                {
                    if (!_glyphTypefaceCache.TryGetValue(glyphTypeface.FamilyName, out var glyphTypefaces))
                    {
                        glyphTypefaces = new Dictionary<FontCollectionKey, IGlyphTypeface>();

                        _glyphTypefaceCache.Add(glyphTypeface.FamilyName, glyphTypefaces);

                        _fontFamilies.Add(new FontFamily(_key, glyphTypeface.FamilyName));
                    }

                    var key = new FontCollectionKey(
                           glyphTypeface.Style,
                           glyphTypeface.Weight,
                           glyphTypeface.Stretch);

                    if (!glyphTypefaces.ContainsKey(key))
                    {
                        glyphTypefaces.Add(key, glyphTypeface);
                    }
                }
            }
        }

        public void Dispose()
        {
            foreach (var fontFamily in _fontFamilies)
            {
                if (_glyphTypefaceCache.TryGetValue(fontFamily.Name, out var glyphTypefaces))
                {
                    foreach (var glyphTypeface in glyphTypefaces.Values)
                    {
                        glyphTypeface.Dispose();
                    }
                }
            }

            GC.SuppressFinalize(this);
        }

        public IEnumerator<FontFamily> GetEnumerator() => _fontFamilies.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public bool TryGetGlyphTypeface(string familyName, FontStyle style, FontWeight weight,
            FontStretch stretch, [NotNullWhen(true)] out IGlyphTypeface? glyphTypeface)
        {
            var key = new FontCollectionKey(style, weight, stretch);

            if (_glyphTypefaceCache.TryGetValue(familyName, out var glyphTypefaces))
            {
                if (TryGetNearestMatch(glyphTypefaces, key, out glyphTypeface))
                {
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

        private static bool TryGetNearestMatch(
            Dictionary<FontCollectionKey, IGlyphTypeface> glyphTypefaces,
            FontCollectionKey key,
            [NotNullWhen(true)] out IGlyphTypeface? glyphTypeface)
        {
            if (glyphTypefaces.TryGetValue(key, out glyphTypeface))
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
                glyphTypeface = typeface;

                return true;
            }

            return false;
        }

        private static bool TryFindStretchFallback(
            Dictionary<FontCollectionKey, IGlyphTypeface> glyphTypefaces,
            FontCollectionKey key,
            [NotNullWhen(true)] out IGlyphTypeface? glyphTypeface)
        {
            glyphTypeface = null;

            var stretch = (int)key.Stretch;

            if (stretch < 5)
            {
                for (var i = 0; stretch + i < 9; i++)
                {
                    if (glyphTypefaces.TryGetValue(key with { Stretch = (FontStretch)(stretch + i) }, out glyphTypeface))
                    {
                        return true;
                    }
                }
            }
            else
            {
                for (var i = 0; stretch - i > 1; i++)
                {
                    if (glyphTypefaces.TryGetValue(key with { Stretch = (FontStretch)(stretch - i) }, out glyphTypeface))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool TryFindWeightFallback(
            Dictionary<FontCollectionKey, IGlyphTypeface> glyphTypefaces,
            FontCollectionKey key,
            [NotNullWhen(true)] out IGlyphTypeface? typeface)
        {
            typeface = null;
            var weight = (int)key.Weight;

            //If the target weight given is between 400 and 500 inclusive          
            if (weight >= 400 && weight <= 500)
            {
                //Look for available weights between the target and 500, in ascending order.
                for (var i = 0; weight + i <= 500; i += 50)
                {
                    if (glyphTypefaces.TryGetValue(key with { Weight = (FontWeight)(weight + i) }, out typeface))
                    {
                        return true;
                    }
                }

                //If no match is found, look for available weights less than the target, in descending order.
                for (var i = 0; weight - i >= 100; i += 50)
                {
                    if (glyphTypefaces.TryGetValue(key with { Weight = (FontWeight)(weight - i) }, out typeface))
                    {
                        return true;
                    }
                }

                //If no match is found, look for available weights greater than 500, in ascending order.
                for (var i = 0; weight + i <= 900; i += 50)
                {
                    if (glyphTypefaces.TryGetValue(key with { Weight = (FontWeight)(weight + i) }, out typeface))
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
                    if (glyphTypefaces.TryGetValue(key with { Weight = (FontWeight)(weight - i) }, out typeface))
                    {
                        return true;
                    }
                }

                //If no match is found, look for available weights less than the target, in descending order.
                for (var i = 0; weight + i <= 900; i += 50)
                {
                    if (glyphTypefaces.TryGetValue(key with { Weight = (FontWeight)(weight + i) }, out typeface))
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
                    if (glyphTypefaces.TryGetValue(key with { Weight = (FontWeight)(weight + i) }, out typeface))
                    {
                        return true;
                    }
                }

                //If no match is found, look for available weights less than the target, in descending order.
                for (var i = 0; weight - i >= 100; i += 50)
                {
                    if (glyphTypefaces.TryGetValue(key with { Weight = (FontWeight)(weight - i) }, out typeface))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
