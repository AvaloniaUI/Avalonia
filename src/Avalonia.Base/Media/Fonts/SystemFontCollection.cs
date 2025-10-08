using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Avalonia.Platform;

namespace Avalonia.Media.Fonts
{
    internal class SystemFontCollection : FontCollectionBase
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
            var typeface = new Typeface(familyName, style, weight, stretch).Normalize(out familyName);

            if (base.TryGetGlyphTypeface(familyName, style, weight, stretch, out glyphTypeface))
            {
                return true;
            }

            style = typeface.Style;

            weight = typeface.Weight;

            stretch = typeface.Stretch;

            var key = new FontCollectionKey(style, weight, stretch);

            //Check cache first to avoid unnecessary calls to the font manager
            if (_glyphTypefaceCache.TryGetValue(familyName, out var glyphTypefaces) && glyphTypefaces.TryGetValue(key, out glyphTypeface))
            {
                return glyphTypeface != null;
            }

            //Try to create the glyph typeface via system font manager
            if (!_fontManager.PlatformImpl.TryCreateGlyphTypeface(familyName, style, weight, stretch, out glyphTypeface))
            {
                //Add null to cache to avoid future calls
                TryAddGlyphTypeface(familyName, key, null);

                return false;
            }

            //Add to cache
            if(!TryAddGlyphTypeface(glyphTypeface))
            {
                return false;
            }

            //Requested glyph typeface should be in cache now
            return base.TryGetGlyphTypeface(familyName, style, weight, stretch, out glyphTypeface);
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

                var key = new FontCollectionKey(glyphTypeface.Style, glyphTypeface.Weight, glyphTypeface.Stretch);

                //Add TypographicFamilyName to the cache
                if (glyphTypeface is IGlyphTypeface2 glyphTypeface2 && !string.IsNullOrEmpty(glyphTypeface2.TypographicFamilyName))
                {
                    TryAddGlyphTypeface(glyphTypeface2.TypographicFamilyName, key, glyphTypeface);
                }

                TryAddGlyphTypeface(glyphTypeface.FamilyName, key, glyphTypeface);
            }
        }

        public override bool TryGetFamilyTypefaces(string familyName, [NotNullWhen(true)] out IReadOnlyList<Typeface>? familyTypefaces)
        {
            familyTypefaces = null;

            if (_fontManager.PlatformImpl is IFontManagerImpl2 fontManagerImpl2)
            {
                return fontManagerImpl2.TryGetFamilyTypefaces(familyName, out familyTypefaces);
            }

            return false;
        }
    }
}
