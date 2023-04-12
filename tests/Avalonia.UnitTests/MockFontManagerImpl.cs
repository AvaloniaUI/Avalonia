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

        public int TryCreateGlyphTypefaceCount { get; private set; }

        public string GetDefaultFontFamilyName()
        {
            return _defaultFamilyName;
        }

        string[] IFontManagerImpl.GetInstalledFontFamilyNames(bool checkForUpdates)
        {
            return new[] { _defaultFamilyName };
        }

        public bool TryMatchCharacter(int codepoint, FontStyle fontStyle, FontWeight fontWeight,
            FontStretch fontStretch,
            CultureInfo culture, out Typeface fontKey)
        {
            fontKey = new Typeface(_defaultFamilyName);

            return false;
        }

        public virtual bool TryCreateGlyphTypeface(string familyName, FontStyle style, FontWeight weight, 
            FontStretch stretch, [NotNullWhen(true)] out IGlyphTypeface glyphTypeface)
        {
            glyphTypeface = null;

            TryCreateGlyphTypefaceCount++;

            if (familyName == "Unknown")
            {
                return false;
            }

            glyphTypeface = new MockGlyphTypeface();

            return true;
        }

        public virtual bool TryCreateGlyphTypeface(Stream stream, out IGlyphTypeface glyphTypeface)
        {
            glyphTypeface = new MockGlyphTypeface();

            return true;
        }
    }
}
