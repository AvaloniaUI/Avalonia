using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Avalonia.Platform;

namespace Avalonia.Media.Fonts
{
    internal class SystemFontCollection : FontCollectionBase, IFontCollection2
    {
        private readonly FontManager _fontManager;
        private readonly List<string> _familyNames;

        public SystemFontCollection(FontManager fontManager)
        {
            _fontManager = fontManager;
            _familyNames = fontManager.PlatformImpl.GetInstalledFontFamilyNames().Where(x => !string.IsNullOrEmpty(x)).ToList();
        }

        public override Uri Key => FontManager.SystemFontsKey;

        public override FontFamily this[int index]
        {
            get
            {
                var familyName = _familyNames[index];

                return new FontFamily(familyName);
            }
        }

        public override int Count => _familyNames.Count;

        public override IEnumerator<FontFamily> GetEnumerator()
        {
            foreach (var familyName in _familyNames)
            {
                yield return new FontFamily(familyName);
            }
        }

        public override bool TryGetGlyphTypeface(string familyName, FontStyle style, FontWeight weight,
            FontStretch stretch, [NotNullWhen(true)] out IGlyphTypeface? glyphTypeface)
        {
            glyphTypeface = null;

            var typeface = GetImplicitTypeface(new Typeface(familyName, style, weight, stretch), out familyName);

            style = typeface.Style;

            weight = typeface.Weight;

            stretch = typeface.Stretch;

            var key = new FontCollectionKey(style, weight, stretch);

            if (_glyphTypefaceCache.TryGetValue(familyName, out var glyphTypefaces))
            {
                if (glyphTypefaces.TryGetValue(key, out glyphTypeface))
                {
                    return glyphTypeface != null;
                }
            }

            glyphTypefaces ??= _glyphTypefaceCache.GetOrAdd(familyName,
                (_) => new ConcurrentDictionary<FontCollectionKey, IGlyphTypeface?>());

            //Try to create the glyph typeface via system font manager
            if (!_fontManager.PlatformImpl.TryCreateGlyphTypeface(familyName, style, weight, stretch,
                    out glyphTypeface))
            {
                glyphTypefaces.TryAdd(key, null);

                return false;
            }

            var createdKey =
                new FontCollectionKey(glyphTypeface.Style, glyphTypeface.Weight, glyphTypeface.Stretch);

            //No exact match
            if (createdKey != key)
            {
                //Add the created glyph typeface to the cache so we can match it.
                glyphTypefaces.TryAdd(createdKey, glyphTypeface);

                //Try to find nearest match if possible
                if (TryGetNearestMatch(glyphTypefaces, key, out var nearestMatch))
                {
                    glyphTypeface = nearestMatch;
                }

                //Try to create a synthetic glyph typeface
                if (TryCreateSyntheticGlyphTypeface(glyphTypeface, style, weight, stretch, out var syntheticGlyphTypeface))
                {
                    glyphTypeface = syntheticGlyphTypeface;

                    return true;
                }
            }

            glyphTypefaces.TryAdd(key, glyphTypeface);

            return glyphTypeface != null;
        }

        public override void Initialize(IFontManagerImpl fontManager)
        {
            //We initialize the system font collection during construction.
        }

        public void AddCustomFontSource(Uri source)
        {
            if (source is null)
            {
                return;
            }

            LoadGlyphTypefaces(_fontManager.PlatformImpl, source);
        }

        private void LoadGlyphTypefaces(IFontManagerImpl fontManager, Uri source)
        {
            var assetLoader = AvaloniaLocator.Current.GetRequiredService<IAssetLoader>();

            var fontAssets = FontFamilyLoader.LoadFontAssets(source);

            foreach (var fontAsset in fontAssets)
            {
                var stream = assetLoader.Open(fontAsset);

                if (!fontManager.TryCreateGlyphTypeface(stream, FontSimulations.None, out var glyphTypeface))
                {
                    continue;
                }

                //Add TypographicFamilyName to the cache
                if (glyphTypeface is IGlyphTypeface2 glyphTypeface2 && !string.IsNullOrEmpty(glyphTypeface2.TypographicFamilyName))
                {
                    AddGlyphTypefaceByFamilyName(glyphTypeface2.TypographicFamilyName, glyphTypeface);
                }

                AddGlyphTypefaceByFamilyName(glyphTypeface.FamilyName, glyphTypeface);
            }

            return;
        }

        public bool TryGetFamilyTypefaces(string familyName, [NotNullWhen(true)] out IReadOnlyList<Typeface>? familyTypefaces)
        {
            familyTypefaces = null;

            if (_fontManager.PlatformImpl is IFontManagerImpl2 fontManagerImpl2)
            {
                return fontManagerImpl2.TryGetFamilyTypefaces(familyName, out familyTypefaces);
            }

            return false;
        }

        public override bool TryMatchCharacter(int codepoint, FontStyle style, FontWeight weight, FontStretch stretch, string? familyName,
            CultureInfo? culture, out Typeface match)
        {
            //TODO12: Think about removing familyName parameter
            match = default;

            if (_fontManager.PlatformImpl is IFontManagerImpl2 fontManagerImpl2)
            {
                if (fontManagerImpl2.TryMatchCharacter(codepoint, style, weight, stretch, culture, out var glyphTypeface))
                {
                    AddGlyphTypefaceByFamilyName(glyphTypeface.FamilyName, glyphTypeface);

                    match = new Typeface(glyphTypeface.FamilyName, glyphTypeface.Style, glyphTypeface.Weight,
                        glyphTypeface.Stretch);

                    return true;
                }

                return false;
            }
            else
            {
                return _fontManager.PlatformImpl.TryMatchCharacter(codepoint, style, weight, stretch, culture, out match);
            }
        }

        private void AddGlyphTypefaceByFamilyName(string familyName, IGlyphTypeface glyphTypeface)
        {
            // Add family name to the collection if not exists
            if (!_familyNames.Contains(familyName))
            {
                _familyNames.Add(familyName);
            }

            // Get or create the typefaces dictionary for the family name
            if (!_glyphTypefaceCache.TryGetValue(familyName, out var typefaces))
            {
                _glyphTypefaceCache[familyName] = typefaces = new ConcurrentDictionary<FontCollectionKey, IGlyphTypeface?>();
            }

            // Add the glyph typeface to the cache
            typefaces.TryAdd(new FontCollectionKey(glyphTypeface.Style, glyphTypeface.Weight, glyphTypeface.Stretch), glyphTypeface);
        }
    }
}
