using Avalonia.Media.TextFormatting.Unicode;
using Xunit;

namespace Avalonia.Base.UnitTests.Media.TextFormatting.Unicode
{
    public class CodepointHasScriptExtensionTests
    {
        [Fact]
        public void Returns_True_When_Script_Matches_Primary_Script()
        {
            var latinA = new Codepoint('A');

            Assert.True(latinA.HasScriptExtension(Script.Latin));
        }

        [Fact]
        public void Returns_False_When_Script_Does_Not_Match()
        {
            var latinA = new Codepoint('A');

            Assert.False(latinA.HasScriptExtension(Script.Hiragana));
        }

        [Fact]
        public void Returns_False_For_Unknown_Script()
        {
            var latinA = new Codepoint('A');

            Assert.False(latinA.HasScriptExtension(Script.Unknown));
        }

        [Theory]
        // U+30FC Katakana-Hiragana Prolonged Sound Mark: scx={Hira, Kana}.
        [InlineData(0x30FC)]
        // U+3031 Vertical Kana Repeat Mark: scx={Hira, Kana}.
        [InlineData(0x3031)]
        // U+30A0 Katakana-Hiragana Double Hyphen: scx={Hira, Kana}.
        [InlineData(0x30A0)]
        public void Hrkt_Shared_Codepoints_Match_Both_Hiragana_And_Katakana(int value)
        {
            var cp = new Codepoint((uint)value);

            Assert.True(cp.HasScriptExtension(Script.Hiragana));
            Assert.True(cp.HasScriptExtension(Script.Katakana));
        }

        [Fact]
        public void Hrkt_Codepoint_Does_Not_Claim_Latin_Extension()
        {
            var cp = new Codepoint(0x30FC);

            Assert.False(cp.HasScriptExtension(Script.Latin));
        }

        [Fact]
        public void Arabic_Tatweel_Reports_All_Listed_Scripts()
        {
            // U+0640 ARABIC TATWEEL has primary Script=Common with scx covering several
            // Arabic-derived scripts (incl. Arabic, Syriac, Mandaic, ...). Primary Common is
            // *not* part of the extensions set.
            var cp = new Codepoint(0x0640);

            Assert.True(cp.HasScriptExtension(Script.Arabic));
            Assert.True(cp.HasScriptExtension(Script.Syriac));
            Assert.False(cp.HasScriptExtension(Script.Common));
            Assert.False(cp.HasScriptExtension(Script.Latin));
        }

        [Fact]
        public void Codepoint_Without_Extensions_Falls_Back_To_Primary_Script()
        {
            // U+05D0 HEBREW LETTER ALEF has primary Script=Hebrew and no Script_Extensions entry.
            var cp = new Codepoint(0x05D0);

            Assert.Equal(Script.Hebrew, cp.Script);
            Assert.True(cp.HasScriptExtension(Script.Hebrew));
            Assert.False(cp.HasScriptExtension(Script.Arabic));
        }
    }
}

