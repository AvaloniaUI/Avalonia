using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Avalonia.Platform;

namespace Avalonia.Media.Fonts
{
    internal class SystemFontCollection : FontCollectionBase
    {
        private readonly IFontManagerImpl _platformImpl;

        public SystemFontCollection(IFontManagerImpl platformImpl)
        {
            _platformImpl = platformImpl ?? throw new ArgumentNullException(nameof(platformImpl));

            var familyNames = _platformImpl.GetInstalledFontFamilyNames().Where(x => !string.IsNullOrEmpty(x));

            foreach (var familyName in familyNames)
            {
                AddFontFamily(familyName);
            }
        }

        public override Uri Key => FontManager.SystemFontsKey;

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
            if (!_platformImpl.TryCreateGlyphTypeface(familyName, style, weight, stretch, out glyphTypeface))
            {
                //Add null to cache to avoid future calls
                TryAddGlyphTypeface(familyName, key, null);

                return false;
            }

            //Add to cache
            if (!TryAddGlyphTypeface(glyphTypeface))
            {
                return false;
            }

            //Requested glyph typeface should be in cache now
            return base.TryGetGlyphTypeface(familyName, style, weight, stretch, out glyphTypeface);
        }

        public override bool TryGetFamilyTypefaces(string familyName, [NotNullWhen(true)] out IReadOnlyList<Typeface>? familyTypefaces)
        {
            familyTypefaces = null;

            if (_platformImpl is IFontManagerImpl2 fontManagerImpl2)
            {
                return fontManagerImpl2.TryGetFamilyTypefaces(familyName, out familyTypefaces);
            }

            return false;
        }

        public override bool TryMatchCharacter(int codepoint, FontStyle style, FontWeight weight, FontStretch stretch, string? familyName,
           CultureInfo? culture, out Typeface match)
        {
            var requestedKey = new FontCollectionKey { Style = style, Weight = weight, Stretch = stretch };

            //TODO12: Think about removing familyName parameter
            if (base.TryMatchCharacter(codepoint, style, weight, stretch, familyName, culture, out match))
            {
                var matchKey = new FontCollectionKey { Style = match.Style, Weight = match.Weight, Stretch = match.Stretch };

                if (requestedKey == matchKey)
                {
                    return true;
                }
            }

            if (_platformImpl is IFontManagerImpl2 fontManagerImpl2)
            {
                if (fontManagerImpl2.TryMatchCharacter(codepoint, style, weight, stretch, familyName, culture, out var glyphTypeface))
                {
                    match = new Typeface(glyphTypeface.FamilyName, glyphTypeface.Style, glyphTypeface.Weight,
                       glyphTypeface.Stretch);

                    // Add to cache if not already present
                    TryAddGlyphTypeface(glyphTypeface);

                    return true;
                }

                return false;
            }
            else
            {
                return _platformImpl.TryMatchCharacter(codepoint, style, weight, stretch, familyName, culture, out match);
            }
        }
    }
}
