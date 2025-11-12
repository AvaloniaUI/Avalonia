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
            FontStretch fontStretch, CultureInfo culture, out IPlatformTypeface? platformTypeface)
        {
            foreach (var customTypeface in _customTypefaces)
            {
                var glyphTypeface = customTypeface.GlyphTypeface;

                if (!glyphTypeface.CharacterToGlyphMap.TryGetValue(codepoint, out _))
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

            return false;
        }

        public bool TryGetFamilyTypefaces(string familyName, [NotNullWhen(true)] out IReadOnlyList<Typeface> familyTypefaces)
        {
            throw new NotImplementedException();
        }
    }
}
