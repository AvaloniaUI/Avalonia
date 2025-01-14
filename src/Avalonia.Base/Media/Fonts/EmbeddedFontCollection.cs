using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Avalonia.Platform;

namespace Avalonia.Media.Fonts
{
    public class EmbeddedFontCollection : FontCollectionBase
    {
        private readonly List<FontFamily> _fontFamilies = new List<FontFamily>(1);

        private readonly Uri _key;

        private readonly Uri _source;

        private IFontManagerImpl? _fontManager;

        public EmbeddedFontCollection(Uri key, Uri source)
        {
            _key = key;

            _source = source;
        }

        public override Uri Key => _key;

        public override FontFamily this[int index] => _fontFamilies[index];

        public override int Count => _fontFamilies.Count;

        public override void Initialize(IFontManagerImpl fontManager)
        {
            _fontManager = fontManager;

            var assetLoader = AvaloniaLocator.Current.GetRequiredService<IAssetLoader>();

            var fontAssets = FontFamilyLoader.LoadFontAssets(_source);

            foreach (var fontAsset in fontAssets)
            {
                var stream = assetLoader.Open(fontAsset);

                if (fontManager.TryCreateGlyphTypeface(stream, FontSimulations.None, out var glyphTypeface))
                {
                    AddGlyphTypeface(glyphTypeface);
                }
            }
        }

        public override bool TryGetGlyphTypeface(string familyName, FontStyle style, FontWeight weight,
            FontStretch stretch, [NotNullWhen(true)] out IGlyphTypeface? glyphTypeface)
        {
            var typeface = GetImplicitTypeface(new Typeface(familyName, style, weight, stretch), out familyName);

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
                    if(_fontManager != null && FontManager.TryCreateSyntheticGlyphTypeface(_fontManager, glyphTypeface, style, weight, out var syntheticGlyphTypeface))
                    {
                        glyphTypeface = syntheticGlyphTypeface;
                    }

                    //Make sure we cache the found match
                    glyphTypefaces.TryAdd(key, glyphTypeface);

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

        public override IEnumerator<FontFamily> GetEnumerator() => _fontFamilies.GetEnumerator();

        private void AddGlyphTypeface(IGlyphTypeface glyphTypeface)
        {
            if (glyphTypeface is IGlyphTypeface2 glyphTypeface2)
            {
                //Add the TypographicFamilyName to the cache
                if (!string.IsNullOrEmpty(glyphTypeface2.TypographicFamilyName))
                {
                    AddGlyphTypefaceByFamilyName(glyphTypeface2.TypographicFamilyName, glyphTypeface);
                }

                foreach (var kvp in glyphTypeface2.FamilyNames)
                {
                    AddGlyphTypefaceByFamilyName(kvp.Value, glyphTypeface);
                }
            }
            else
            {
                AddGlyphTypefaceByFamilyName(glyphTypeface.FamilyName, glyphTypeface);
            }

            return;

            void AddGlyphTypefaceByFamilyName(string familyName, IGlyphTypeface glyphTypeface)
            {
                var typefaces = _glyphTypefaceCache.GetOrAdd(familyName,
                    x =>
                    {
                        _fontFamilies.Add(new FontFamily(_key, familyName));

                        return new ConcurrentDictionary<FontCollectionKey, IGlyphTypeface?>();
                    });

                typefaces.TryAdd(
                    new FontCollectionKey(glyphTypeface.Style, glyphTypeface.Weight, glyphTypeface.Stretch),
                    glyphTypeface);
            }
        }
    }
}
