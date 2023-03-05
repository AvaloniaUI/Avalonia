using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using Avalonia.Media;
using Avalonia.Platform;

namespace Avalonia.UnitTests
{
    public class MockFontManagerImpl : IFontManagerImpl
    {
        private readonly string _defaultFamilyName;

        public MockFontManagerImpl(string defaultFamilyName = "Default")
        {
            _defaultFamilyName = defaultFamilyName;
        }

        public string GetDefaultFontFamilyName()
        {
            return _defaultFamilyName;
        }

        string[] IFontManagerImpl.GetInstalledFontFamilyNames(bool checkForUpdates)
        {
            return new[] { _defaultFamilyName };
        }

        public bool TryMatchCharacter(int codepoint, FontStyle fontStyle, FontWeight fontWeight,
            FontStretch fontStretch, FontFamily fontFamily,
            CultureInfo culture, out Typeface fontKey)
        {
            fontKey = new Typeface(_defaultFamilyName);

            return false;
        }

        public bool TryCreateGlyphTypeface(string familyName, FontStyle style, FontWeight weight, FontStretch stretch, [NotNullWhen(true)] out IGlyphTypeface glyphTypeface)
        {
            glyphTypeface = new MockGlyphTypeface();

            return true;
        }

        public bool TryCreateGlyphTypeface(Stream stream, out IGlyphTypeface glyphTypeface)
        {
            glyphTypeface = new MockGlyphTypeface();

            return true;
        }
    }
}
