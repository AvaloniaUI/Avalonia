using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.Platform;

namespace Avalonia.UnitTests
{
    public class HeadlessFontManagerImpl : IFontManagerImpl
    {
        private readonly Typeface[] _customTypefaces;
        private readonly string _defaultFamilyName;

        private static readonly Typeface _defaultTypeface =
            new Typeface(new FontFamily("resm:Avalonia.UnitTests.Assets?assembly=Avalonia.UnitTests#Noto Mono"));
        private static readonly Typeface _italicTypeface =
            new Typeface(new FontFamily("resm:Avalonia.UnitTests.Assets?assembly=Avalonia.UnitTests#Noto Sans"));
        private static readonly Typeface _emojiTypeface =
            new Typeface(new FontFamily("resm:Avalonia.UnitTests.Assets?assembly=Avalonia.UnitTests#Twitter Color Emoji"));

        public HeadlessFontManagerImpl(string defaultFamilyName = "Noto Mono")
        {
            _customTypefaces = new[] { _emojiTypeface, _italicTypeface, _defaultTypeface };
            _defaultFamilyName = defaultFamilyName;
        }

        public string GetDefaultFontFamilyName()
        {
            return _defaultFamilyName;
        }

        string[] IFontManagerImpl.GetInstalledFontFamilyNames(bool checkForUpdates)
        {
            return _customTypefaces.Select(x => x.FontFamily!.Name).ToArray();
        }

        public bool TryMatchCharacter(int codepoint, FontStyle fontStyle, FontWeight fontWeight,
            FontStretch fontStretch, string? familyName, CultureInfo? culture, [NotNullWhen(true)] out IPlatformTypeface? platformTypeface)
        {
            foreach (var customTypeface in _customTypefaces)
            {
                var glyphTypeface = customTypeface.GlyphTypeface;

                if (!glyphTypeface.CharacterToGlyphMap.TryGetGlyph(codepoint, out _))
                {
                    continue;
                }

                platformTypeface = glyphTypeface.PlatformTypeface;

                return true;
            }

            platformTypeface = null;

            return false;
        }

        public bool TryCreateGlyphTypeface(Stream stream, FontSimulations fontSimulations, [NotNullWhen(true)] out IPlatformTypeface? platformTypeface)
        {
            platformTypeface = new HeadlessPlatformTypeface(stream);

            return true;
        }

        public bool TryCreateGlyphTypeface(string familyName, FontStyle style, FontWeight weight,
            FontStretch stretch, [NotNullWhen(true)] out IPlatformTypeface? platformTypeface)
        {
            platformTypeface = null;

            // Search through custom typefaces for matching family name and style
            foreach (var customTypeface in _customTypefaces)
            {
                var glyphTypeface = customTypeface.GlyphTypeface;

                // Check if family name matches
                if (!string.Equals(glyphTypeface.FamilyName, familyName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Check if style properties match (exact match preferred, but any match is acceptable)
                var styleMatches = glyphTypeface.Style == style;
                var weightMatches = glyphTypeface.Weight == weight;
                var stretchMatches = glyphTypeface.Stretch == stretch;

                // Exact match - return immediately
                if (styleMatches && weightMatches && stretchMatches)
                {
                    platformTypeface = glyphTypeface.PlatformTypeface;
                    return true;
                }

                // If family matches but style doesn't, keep searching
                // but remember first family match as fallback
                if (platformTypeface == null)
                {
                    platformTypeface = glyphTypeface.PlatformTypeface;
                }
            }

            // Return true if we found at least a family match (even if style doesn't match exactly)
            return platformTypeface != null;
        }

        public bool TryGetFamilyTypefaces(string familyName, [NotNullWhen(true)] out IReadOnlyList<Typeface>? familyTypefaces)
        {
            // Find all typefaces that belong to the specified family
            var typefaces = new List<Typeface>();

            foreach (var customTypeface in _customTypefaces)
            {
                var glyphTypeface = customTypeface.GlyphTypeface;

                if (string.Equals(glyphTypeface.FamilyName, familyName, StringComparison.OrdinalIgnoreCase))
                {
                    typefaces.Add(customTypeface);
                }
            }

            if (typefaces.Count > 0)
            {
                familyTypefaces = typefaces;
                return true;
            }

            familyTypefaces = null;
            return false;
        }
    }
}
