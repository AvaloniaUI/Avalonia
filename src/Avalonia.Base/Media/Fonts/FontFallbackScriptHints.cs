using System.Globalization;
using Avalonia.Media.TextFormatting.Unicode;

namespace Avalonia.Media.Fonts
{
    /// <summary>
    /// Script-aware hints used by <see cref="FontCollectionBase.TryMatchCharacter"/> to
    /// disambiguate locale-sensitive scripts (e.g. CJK), to provide deterministic probe
    /// codepoints for scoring candidate fonts, and to skip cache pollution for scripts that
    /// don't benefit from per-culture fallback caching (e.g. Latin / Common).
    /// </summary>
    internal static class FontFallbackScriptHints
    {
        /// <summary>
        /// Returns true if the script's preferred font typically depends on the user's
        /// culture (CJK Han is the canonical example: same codepoints render with different
        /// fonts in zh-CN vs. ja-JP vs. ko-KR).
        /// </summary>
        public static bool IsLocaleSensitive(Script script)
            => GetProbeCodepoint(script) != 0;

        /// <summary>
        /// Refines a script using the supplied culture. Pure-Han codepoints will be remapped
        /// to <see cref="Script.Hiragana"/> for Japanese cultures and <see cref="Script.Hangul"/>
        /// for Korean cultures so the candidate font is scored against a probe in the user's
        /// preferred script. CJK ambiguous (<see cref="Script.KatakanaOrHiragana"/>) is
        /// normalised to <see cref="Script.Hiragana"/>.
        /// </summary>
        public static Script RefineWithCulture(Script script, CultureInfo? culture)
        {
            if (script == Script.KatakanaOrHiragana)
            {
                return Script.Hiragana;
            }

            if (culture == null || script != Script.Han)
            {
                return script;
            }

            var name = culture.Name;

            if (StartsWithIgnoreCase(name, "ja"))
            {
                return Script.Hiragana;
            }

            if (StartsWithIgnoreCase(name, "ko"))
            {
                return Script.Hangul;
            }

            return script;
        }

        /// <summary>
        /// Returns a representative codepoint for the script that can be used to probe a
        /// candidate font's character-to-glyph map. Returns 0 for scripts without a probe
        /// (in which case the script is considered locale-insensitive).
        /// </summary>
        public static int GetProbeCodepoint(Script script) => script switch
        {
            Script.Han => 0x4E2D,        // 中
            Script.Hiragana => 0x3042,   // あ
            Script.Katakana => 0x30A2,   // ア
            Script.KatakanaOrHiragana => 0x3042,
            Script.Hangul => 0xAC00,     // 가
            Script.Bopomofo => 0x3105,   // ㄅ
            Script.Arabic => 0x0627,     // ا
            Script.Hebrew => 0x05D0,     // א
            Script.Devanagari => 0x0915, // क
            Script.Bengali => 0x0995,    // ক
            Script.Thai => 0x0E01,       // ก
            Script.Tibetan => 0x0F40,    // ཀ
            Script.Cyrillic => 0x0410,   // А
            Script.Greek => 0x0391,      // Α
            _ => 0,
        };

        /// <summary>
        /// Returns the bit index into the OS/2 ulUnicodeRange bitfield (UnicodeRange1..4) that the
        /// OpenType specification assigns to the supplied script, or -1 if the script has no
        /// canonical OS/2 bit. Bits 0..31 belong to UnicodeRange1, 32..63 to UnicodeRange2, etc.
        /// </summary>
        public static bool TryGetOS2Bit(Script script, out int bit)
        {
            bit = script switch
            {
                Script.Greek => 7,
                Script.Cyrillic => 9,
                Script.Hebrew => 11,
                Script.Arabic => 13,
                Script.Devanagari => 15,
                Script.Bengali => 16,
                Script.Thai => 24,
                Script.Hiragana => 49,
                Script.Katakana => 50,
                Script.KatakanaOrHiragana => 49,
                Script.Bopomofo => 51,
                Script.Hangul => 56,
                Script.Han => 59,
                Script.Tibetan => 70,
                _ => -1,
            };

            return bit >= 0;
        }

        private static bool StartsWithIgnoreCase(string value, string prefix)
        {
            if (value.Length < prefix.Length)
            {
                return false;
            }

            for (var i = 0; i < prefix.Length; i++)
            {
                var a = value[i];
                var b = prefix[i];

                if (a >= 'A' && a <= 'Z')
                {
                    a = (char)(a + 32);
                }

                if (b >= 'A' && b <= 'Z')
                {
                    b = (char)(b + 32);
                }

                if (a != b)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
