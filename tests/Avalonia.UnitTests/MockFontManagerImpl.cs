using System.Collections.Generic;
using System.Globalization;
using Avalonia.Media;
using Avalonia.Media.Fonts;
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

        public IEnumerable<string> GetInstalledFontFamilyNames(bool checkForUpdates = false)
        {
            return new[] { _defaultFamilyName };
        }

        public bool TryMatchCharacter(int codepoint, FontWeight fontWeight, FontStyle fontStyle, FontFamily fontFamily,
            CultureInfo culture, out FontKey fontKey)
        {
            fontKey = default;

            return false;
        }

        public IGlyphTypefaceImpl CreateGlyphTypeface(Typeface typeface)
        {
            return new MockGlyphTypeface();
        }
    }
}
