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
            FontStretch stretch, [NotNullWhen(true)] out GlyphTypeface? glyphTypeface)
        {
            var typeface = new Typeface(familyName, style, weight, stretch).Normalize(out familyName);
            var key = typeface.ToFontCollectionKey();

            // Find an exact match first
            if (TryGetGlyphTypeface(familyName, key, allowNearestMatch: false, out glyphTypeface))
            {
                return true;
            }

            //Check cache first to avoid unnecessary calls to the font manager
            if (_glyphTypefaceCache.TryGetValue(familyName, out var glyphTypefaces) && glyphTypefaces.TryGetValue(key, out glyphTypeface))
            {
                return glyphTypeface != null;
            }

            //Try to create the glyph typeface via system font manager
            if (!_platformImpl.TryCreateGlyphTypeface(familyName, style, weight, stretch, out var platformTypeface))
            {
                //Add null to cache to avoid future calls
                TryAddGlyphTypeface(familyName, key, null);

                return false;
            }

            // The platform matched the request against every installed face of the family, while the
            // cache only holds earlier requests, so its answer is the match even when it is not the
            // requested key. A cached nearest match could only pick a face the platform judged worse,
            // such as an upright Medium for a SemiBold Italic request when the family has a Bold.
            var platformKey = platformTypeface.ToFontCollectionKey();
            var platformFamilyName = platformTypeface.FamilyName;

            if (!TryGetOrAddPlatformGlyphTypeface(platformTypeface, platformFamilyName, platformKey,
                    out var platformGlyphTypeface))
            {
                return false;
            }

            glyphTypeface = platformGlyphTypeface;

            if (platformKey != key &&
                TryCreateSyntheticGlyphTypeface(platformGlyphTypeface, style, weight, stretch, out var synthetic))
            {
                glyphTypeface = synthetic;
            }

            if (!TryAddGlyphTypeface(familyName, key, glyphTypeface) &&
                _glyphTypefaceCache.TryGetValue(familyName, out var existingMap) &&
                existingMap.TryGetValue(key, out var existing) && existing is not null)
            {
                // Another thread registered a face for this request first.
                glyphTypeface = existing;
            }

            if (!string.Equals(familyName, platformFamilyName, StringComparison.OrdinalIgnoreCase))
            {
                TryAddGlyphTypeface(platformFamilyName, key, glyphTypeface);
            }

            return true;
        }

        /// <summary>
        /// Gets the glyph typeface cached for a platform typeface under the key it reports, or creates
        /// and caches one.
        /// </summary>
        /// <remarks>
        /// The collection owns the platform typeface the font manager returned, so one it does not keep
        /// is disposed, unless it is the very instance the cached glyph typeface holds.
        /// </remarks>
        private bool TryGetOrAddPlatformGlyphTypeface(IPlatformTypeface platformTypeface, string platformFamilyName,
            FontCollectionKey platformKey,
            [NotNullWhen(true)] out GlyphTypeface? glyphTypeface)
        {
            if (TryGetCached(platformFamilyName, platformKey, out glyphTypeface))
            {
                if (!ReferenceEquals(platformTypeface, glyphTypeface.PlatformTypeface))
                {
                    platformTypeface.Dispose();
                }

                return true;
            }

            glyphTypeface = GlyphTypeface.TryCreate(platformTypeface, platformTypeface.FontSimulations);

            if (glyphTypeface is null)
            {
                return false;
            }

            if (!TryAddGlyphTypeface(platformFamilyName, platformKey, glyphTypeface) &&
                TryGetCached(platformFamilyName, platformKey, out var existing))
            {
                // Another thread cached a face under this key first. The glyph typeface created here
                // owns the platform typeface, so disposing it releases both.
                glyphTypeface.Dispose();

                glyphTypeface = existing;

                return true;
            }

            // The face's own family names, under which a synthetic variant of it is looked up.
            TryAddGlyphTypeface(glyphTypeface, platformKey);

            return true;
        }

        private bool TryGetCached(string familyName, FontCollectionKey key,
            [NotNullWhen(true)] out GlyphTypeface? glyphTypeface)
        {
            glyphTypeface = null;

            return _glyphTypefaceCache.TryGetValue(familyName, out var glyphTypefaces) &&
                   glyphTypefaces.TryGetValue(key, out glyphTypeface) && glyphTypeface is not null;
        }

        public override bool TryGetFamilyTypefaces(string familyName, [NotNullWhen(true)] out IReadOnlyList<Typeface>? familyTypefaces)
        {
            return _platformImpl.TryGetFamilyTypefaces(familyName, out familyTypefaces);
        }

        public override bool TryMatchCharacter(int codepoint, FontStyle style, FontWeight weight, FontStretch stretch, string? familyName,
           CultureInfo? culture, out Typeface match)
        {
            // Delegate to the base algorithm. The platform call is exposed through
            // TryMatchCharacterFromPlatform and invoked at most once per (script-bucket, culture)
            // pair by the base implementation, after every cached candidate has been considered.
            return base.TryMatchCharacter(codepoint, style, weight, stretch, familyName, culture, out match);
        }

        protected override bool TryMatchCharacterFromPlatform(
            int codepoint,
            FontCollectionKey key,
            string? familyName,
            CultureInfo? culture,
            [NotNullWhen(true)] out GlyphTypeface? glyphTypeface)
        {
            glyphTypeface = null;

            if (!_platformImpl.TryMatchCharacter(codepoint, key.Style, key.Weight, key.Stretch, familyName, culture, out var platformTypeface))
            {
                return false;
            }

            var platformKey = new FontCollectionKey(platformTypeface.Style, platformTypeface.Weight, platformTypeface.Stretch);

            // Registered under the platform's key, so future lookups can short-circuit through
            // TryMatchCharacter's Tier C without re-invoking the platform.
            return TryGetOrAddPlatformGlyphTypeface(platformTypeface, platformTypeface.FamilyName, platformKey,
                out glyphTypeface);
        }
    }
}
