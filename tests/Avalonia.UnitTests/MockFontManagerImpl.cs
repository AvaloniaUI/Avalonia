using System.Collections.Generic;
using System.Globalization;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Avalonia.Platform;

namespace Avalonia.UnitTests
{
    public class MockFontManagerImpl : IFontManagerImpl
    {
        public string GetDefaultFontFamilyName()
        {
            return "Default";
        }

        public IEnumerable<string> GetInstalledFontFamilyNames(bool checkForUpdates = false)
        {
            return new[] { "Default" };
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
