using System.Globalization;
using Avalonia.Media.Fonts;
using Avalonia.Media.TextFormatting.Unicode;
using Xunit;

namespace Avalonia.Base.UnitTests.Media.Fonts
{
    public class FontFallbackScriptHintsTests
    {
        [Fact]
        public void RefineWithCulture_Returns_Han_For_Han_Without_Culture()
        {
            // U+4E2D 中 — Han script.
            var cp = new Codepoint(0x4E2D);

            var refined = FontFallbackScriptHints.RefineWithCulture(cp, culture: null);

            Assert.Equal(Script.Han, refined);
        }

        [Fact]
        public void RefineWithCulture_Han_With_Japanese_Culture_Maps_To_Hiragana()
        {
            var cp = new Codepoint(0x4E2D);

            var refined = FontFallbackScriptHints.RefineWithCulture(cp, CultureInfo.GetCultureInfo("ja-JP"));

            Assert.Equal(Script.Hiragana, refined);
        }

        [Fact]
        public void RefineWithCulture_Han_With_Korean_Culture_Maps_To_Hangul()
        {
            var cp = new Codepoint(0x4E2D);

            var refined = FontFallbackScriptHints.RefineWithCulture(cp, CultureInfo.GetCultureInfo("ko-KR"));

            Assert.Equal(Script.Hangul, refined);
        }

        [Fact]
        public void RefineWithCulture_Han_With_Chinese_Culture_Stays_Han()
        {
            var cp = new Codepoint(0x4E2D);

            var refinedSimplified = FontFallbackScriptHints.RefineWithCulture(cp, CultureInfo.GetCultureInfo("zh-CN"));
            var refinedTraditional = FontFallbackScriptHints.RefineWithCulture(cp, CultureInfo.GetCultureInfo("zh-TW"));

            Assert.Equal(Script.Han, refinedSimplified);
            Assert.Equal(Script.Han, refinedTraditional);
        }

        [Fact]
        public void RefineWithCulture_Common_Codepoint_Passes_Through()
        {
            // U+30FC has primary Script=Common in Avalonia's data (its Hrkt membership lives
            // in Script_Extensions and is consulted directly through Codepoint.HasScriptExtension).
            // RefineWithCulture only refines codepoints whose primary script is ambiguous.
            var cp = new Codepoint(0x30FC);

            var refined = FontFallbackScriptHints.RefineWithCulture(cp, culture: null);

            Assert.Equal(cp.Script, refined);
        }

        [Fact]
        public void RefineWithCulture_Latin_Codepoint_Returns_Latin_Unchanged()
        {
            var cp = new Codepoint('A');

            var refined = FontFallbackScriptHints.RefineWithCulture(cp, CultureInfo.GetCultureInfo("en-US"));

            Assert.Equal(Script.Latin, refined);
        }

        [Fact]
        public void IsLocaleSensitive_True_For_Han()
        {
            Assert.True(FontFallbackScriptHints.IsLocaleSensitive(Script.Han));
        }

        [Fact]
        public void IsLocaleSensitive_False_For_Latin()
        {
            Assert.False(FontFallbackScriptHints.IsLocaleSensitive(Script.Latin));
        }

        [Theory]
        [InlineData(Script.Hiragana, 49)]
        [InlineData(Script.Katakana, 50)]
        [InlineData(Script.Hangul, 56)]
        [InlineData(Script.Han, 59)]
        [InlineData(Script.Cyrillic, 9)]
        [InlineData(Script.Arabic, 13)]
        public void TryGetOS2Bit_Returns_Spec_Bit(Script script, int expected)
        {
            Assert.True(FontFallbackScriptHints.TryGetOS2Bit(script, out var bit));
            Assert.Equal(expected, bit);
        }

        [Fact]
        public void TryGetOS2Bit_Latin_Returns_False()
        {
            Assert.False(FontFallbackScriptHints.TryGetOS2Bit(Script.Latin, out var bit));
            Assert.Equal(-1, bit);
        }
    }
}
