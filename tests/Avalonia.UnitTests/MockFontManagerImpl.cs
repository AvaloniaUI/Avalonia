using System.Collections.Generic;
using System.Globalization;
using Avalonia.Media;
using Avalonia.Platform;

namespace Avalonia.UnitTests
{
    public class MockFontManagerImpl : IFontManagerImpl
    {
        public string DefaultFontFamilyName => "Default";

        public IEnumerable<string> GetInstalledFontFamilyNames(bool checkForUpdates = false) =>
            new[] { DefaultFontFamilyName };

        public Typeface GetTypeface(FontFamily fontFamily, FontWeight fontWeight, FontStyle fontStyle)
        {
            return new Typeface(fontFamily, fontWeight, fontStyle);
        }

        public Typeface MatchCharacter(int codepoint, FontWeight fontWeight = default, FontStyle fontStyle = default,
            FontFamily fontFamily = null, CultureInfo culture = null)
        {
            return null;
        }
    }
}
