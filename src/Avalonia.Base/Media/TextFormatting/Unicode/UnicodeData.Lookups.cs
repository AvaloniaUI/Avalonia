using System.Runtime.CompilerServices;

namespace Avalonia.Media.TextFormatting.Unicode
{
    internal static partial class UnicodeData
    {
        /// <summary>
        /// Gets the <see cref="GeneralCategory"/> for a Unicode codepoint.
        /// </summary>
        /// <param name="codepoint">The codepoint in question.</param>
        /// <returns>The code point's general category.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GeneralCategory GetGeneralCategory(uint codepoint)
        {
            return (GeneralCategory)(UnicodeDataTrie.Trie.Get(codepoint) & CATEGORY_MASK);
        }

        /// <summary>
        /// Gets the <see cref="Script"/> for a Unicode codepoint.
        /// </summary>
        /// <param name="codepoint">The codepoint in question.</param>
        /// <returns>The code point's script.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Script GetScript(uint codepoint)
        {
            return (Script)((UnicodeDataTrie.Trie.Get(codepoint) >> SCRIPT_SHIFT) & SCRIPT_MASK);
        }

        /// <summary>
        /// Gets the <see cref="BidiClass"/> for a Unicode codepoint.
        /// </summary>
        /// <param name="codepoint">The codepoint in question.</param>
        /// <returns>The code point's biDi class.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BidiClass GetBiDiClass(uint codepoint)
        {
            return (BidiClass)((BiDiTrie.Trie.Get(codepoint) >> BIDICLASS_SHIFT) & BIDICLASS_MASK);
        }

        /// <summary>
        /// Gets the <see cref="BidiPairedBracketType"/> for a Unicode codepoint.
        /// </summary>
        /// <param name="codepoint">The codepoint in question.</param>
        /// <returns>The code point's paired bracket type.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BidiPairedBracketType GetBiDiPairedBracketType(uint codepoint)
        {
            return (BidiPairedBracketType)((BiDiTrie.Trie.Get(codepoint) >> BIDIPAIREDBRACKEDTYPE_SHIFT) & BIDIPAIREDBRACKEDTYPE_MASK);
        }

        /// <summary>
        /// Gets the paired bracket for a Unicode codepoint.
        /// </summary>
        /// <param name="codepoint">The codepoint in question.</param>
        /// <returns>The code point's paired bracket.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Codepoint GetBiDiPairedBracket(uint codepoint)
        {
            return new Codepoint(BiDiTrie.Trie.Get(codepoint) & BIDIPAIREDBRACKED_MASK);
        }

        /// <summary>
        /// Gets the line break class for a Unicode codepoint.
        /// </summary>
        /// <param name="codepoint">The codepoint in question.</param>
        /// <returns>The code point's line break class.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LineBreakClass GetLineBreakClass(uint codepoint)
        {
            return (LineBreakClass)((UnicodeDataTrie.Trie.Get(codepoint) >> LINEBREAK_SHIFT) & LINEBREAK_MASK);
        }

        /// <summary>
        /// Gets the word break class for a Unicode codepoint.
        /// </summary>
        /// <param name="codepoint">The codepoint in question.</param>
        /// <returns>The code point's word break class.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static WordBreakClass GetWordBreakClass(uint codepoint)
        {
            return (WordBreakClass)((UnicodeDataTrie.Trie.Get(codepoint) >> WORDBREAK_SHIFT) & WORDBREAK_MASK);
        }

        /// <summary>
        /// Gets the grapheme break type for the Unicode codepoint.
        /// </summary>
        /// <param name="codepoint">The codepoint in question.</param>
        /// <returns>The code point's grapheme break type.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GraphemeBreakClass GetGraphemeClusterBreak(uint codepoint)
        {
            return (GraphemeBreakClass)(GraphemeBreakTrie.Trie.Get(codepoint) & GRAPHEMEBREAK_MASK);
        }

        /// <summary>
        /// Gets the Indic conjunct break class for the Unicode codepoint.
        /// </summary>
        /// <param name="codepoint">The codepoint in question.</param>
        /// <returns>The code point's Indic conjunct break class.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static IndicConjunctBreakClass GetIndicConjunctBreakClass(uint codepoint)
        {
            return (IndicConjunctBreakClass)((GraphemeBreakTrie.Trie.Get(codepoint) >> INDICCONJUNCTBREAK_SHIFT) & INDICCONJUNCTBREAK_MASK);
        }

        /// <summary>
        /// Gets the EastAsianWidth class for the Unicode codepoint.
        /// </summary>
        /// <param name="codepoint">The codepoint in question.</param>
        /// <returns>The code point's EastAsianWidth class.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EastAsianWidthClass GetEastAsianWidthClass(uint codepoint)
        {
            return (EastAsianWidthClass)EastAsianWidthTrie.Trie.Get(codepoint);
        }

        /// <summary>
        /// Determines whether the given codepoint's Script_Extensions property (UAX #24) contains
        /// the supplied script. When the codepoint has no explicit Script_Extensions entry the
        /// extensions set is taken to be the singleton of the codepoint's primary
        /// <see cref="Script"/> property.
        /// </summary>
        /// <param name="codepoint">The codepoint in question.</param>
        /// <param name="script">The script being tested.</param>
        public static bool HasScriptExtension(uint codepoint, Script script)
        {
            if (script == Script.Unknown)
            {
                return false;
            }

            var packed = UnicodeDataTrie.Trie.Get(codepoint);
            var setIndex = (int)((packed >> SCRIPTEXTENSIONS_SHIFT) & SCRIPTEXTENSIONS_MASK);

            if (setIndex == 0)
            {
                var primary = (Script)((packed >> SCRIPT_SHIFT) & SCRIPT_MASK);
                return primary == script;
            }

            var sets = s_scriptExtensionSets;

            if ((uint)setIndex >= (uint)sets.Length)
            {
                return false;
            }

            var target = (byte)script;
            var set = sets[setIndex];

            for (var i = 0; i < set.Length; i++)
            {
                if (set[i] == target)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
