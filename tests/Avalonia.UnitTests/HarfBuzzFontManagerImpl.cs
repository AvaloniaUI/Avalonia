using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using Avalonia.Media;
using Avalonia.Platform;

namespace Avalonia.UnitTests
{
    public class HarfBuzzFontManagerImpl : IFontManagerImpl
    {
        private readonly Typeface[] _customTypefaces;
        private readonly string _defaultFamilyName;

        private static readonly Typeface _defaultTypeface =
            new Typeface("resm:Avalonia.UnitTests.Assets?assembly=Avalonia.UnitTests#Noto Mono");
        private  static readonly Typeface _italicTypeface =
            new Typeface("resm:Avalonia.UnitTests.Assets?assembly=Avalonia.UnitTests#Noto Sans");
        private  static readonly Typeface _emojiTypeface =
            new Typeface("resm:Avalonia.UnitTests.Assets?assembly=Avalonia.UnitTests#Twitter Color Emoji");

        public HarfBuzzFontManagerImpl(string defaultFamilyName = "resm:Avalonia.UnitTests.Assets?assembly=Avalonia.UnitTests#Noto Mono")
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
            FontStretch fontStretch, CultureInfo culture, out Typeface fontKey)
        {
            foreach (var customTypeface in _customTypefaces)
            {
                var glyphTypeface = customTypeface.GlyphTypeface;

                if (!glyphTypeface.TryGetGlyph((uint)codepoint, out _))
                {
                    continue;
                }
                
                fontKey = customTypeface;
                    
                return true;
            }

            fontKey = default;

            return false;
        }

        public bool TryCreateGlyphTypeface(Stream stream, FontSimulations fontSimulations, out IGlyphTypeface glyphTypeface)
        {
            glyphTypeface = new HarfBuzzGlyphTypefaceImpl(stream);

            return true;
        }

        public bool TryCreateGlyphTypeface(string familyName, FontStyle style, FontWeight weight, 
            FontStretch stretch, [NotNullWhen(true)] out IGlyphTypeface glyphTypeface)
        {
            glyphTypeface = null;

            return false;
        }
    }
}
