using System.Runtime.CompilerServices;

namespace Avalonia.Media.TextFormatting.Unicode
{
    /// <summary>
    ///     Helper for looking up unicode character class information
    /// </summary>
    internal static class UnicodeData
    {
        internal const int CATEGORY_BITS = 6;
        internal const int SCRIPT_BITS = 8;
        internal const int LINEBREAK_BITS = 6;

        internal const int BIDIPAIREDBRACKED_BITS = 16;
        internal const int BIDIPAIREDBRACKEDTYPE_BITS = 2;
        internal const int BIDICLASS_BITS = 5;

        internal const int SCRIPT_SHIFT = CATEGORY_BITS;
        internal const int LINEBREAK_SHIFT = CATEGORY_BITS + SCRIPT_BITS;
        
        internal const int BIDIPAIREDBRACKEDTYPE_SHIFT = BIDIPAIREDBRACKED_BITS;
        internal const int BIDICLASS_SHIFT = BIDIPAIREDBRACKED_BITS + BIDIPAIREDBRACKEDTYPE_BITS;
        
        internal const int CATEGORY_MASK = (1 << CATEGORY_BITS) - 1;
        internal const int SCRIPT_MASK = (1 << SCRIPT_BITS) - 1;
        internal const int LINEBREAK_MASK = (1 << LINEBREAK_BITS) - 1;
        
        internal const int BIDIPAIREDBRACKED_MASK = (1 << BIDIPAIREDBRACKED_BITS) - 1;
        internal const int BIDIPAIREDBRACKEDTYPE_MASK = (1 << BIDIPAIREDBRACKEDTYPE_BITS) - 1;
        internal const int BIDICLASS_MASK = (1 << BIDICLASS_BITS) - 1;

        private static readonly UnicodeTrie s_unicodeDataTrie;
        private static readonly UnicodeTrie s_graphemeBreakTrie;
        private static readonly UnicodeTrie s_biDiTrie;

        static UnicodeData()
        {
            s_unicodeDataTrie = new UnicodeTrie(typeof(UnicodeData).Assembly.GetManifestResourceStream("Avalonia.Assets.UnicodeData.trie"));
            s_graphemeBreakTrie = new UnicodeTrie(typeof(UnicodeData).Assembly.GetManifestResourceStream("Avalonia.Assets.GraphemeBreak.trie"));
            s_biDiTrie = new UnicodeTrie(typeof(UnicodeData).Assembly.GetManifestResourceStream("Avalonia.Assets.BiDi.trie"));
        }

        /// <summary>
        /// Gets the <see cref="GeneralCategory"/> for a Unicode codepoint.
        /// </summary>
        /// <param name="codepoint">The codepoint in question.</param>
        /// <returns>The code point's general category.</returns>
        public static GeneralCategory GetGeneralCategory(int codepoint)
        {
            var value = s_unicodeDataTrie.Get(codepoint);

            return (GeneralCategory)(value & CATEGORY_MASK);
        }

        /// <summary>
        /// Gets the <see cref="Script"/> for a Unicode codepoint.
        /// </summary>
        /// <param name="codepoint">The codepoint in question.</param>
        /// <returns>The code point's script.</returns>
        public static Script GetScript(int codepoint)
        {
            var value = s_unicodeDataTrie.Get(codepoint);

            return (Script)((value >> SCRIPT_SHIFT) & SCRIPT_MASK);
        }

        /// <summary>
        /// Gets the <see cref="BiDiClass"/> for a Unicode codepoint.
        /// </summary>
        /// <param name="codepoint">The codepoint in question.</param>
        /// <returns>The code point's biDi class.</returns>
        public static BiDiClass GetBiDiClass(int codepoint)
        {
            var value = s_biDiTrie.Get(codepoint);

            return (BiDiClass)((value >> BIDICLASS_SHIFT) & BIDICLASS_MASK);
        }
        
        /// <summary>
        /// Gets the <see cref="BiDiPairedBracketType"/> for a Unicode codepoint.
        /// </summary>
        /// <param name="codepoint">The codepoint in question.</param>
        /// <returns>The code point's paired bracket type.</returns>
        public static BiDiPairedBracketType GetBiDiPairedBracketType(int codepoint)
        {
            var value = s_biDiTrie.Get(codepoint);

            return (BiDiPairedBracketType)((value >> BIDIPAIREDBRACKEDTYPE_SHIFT) & BIDIPAIREDBRACKEDTYPE_MASK);
        }
        
        /// <summary>
        /// Gets the paired bracket for a Unicode codepoint.
        /// </summary>
        /// <param name="codepoint">The codepoint in question.</param>
        /// <returns>The code point's paired bracket.</returns>
        public static Codepoint GetBiDiPairedBracket(int codepoint)
        {
            var value = s_biDiTrie.Get(codepoint);

            return new Codepoint((int)(value & BIDIPAIREDBRACKED_MASK));
        }

        /// <summary>
        /// Gets the line break class for a Unicode codepoint.
        /// </summary>
        /// <param name="codepoint">The codepoint in question.</param>
        /// <returns>The code point's line break class.</returns>
        public static LineBreakClass GetLineBreakClass(int codepoint)
        {
            var value = s_unicodeDataTrie.Get(codepoint);

            return (LineBreakClass)((value >> LINEBREAK_SHIFT) & LINEBREAK_MASK);
        }

        /// <summary>
        /// Gets the grapheme break type for the Unicode codepoint.
        /// </summary>
        /// <param name="codepoint">The codepoint in question.</param>
        /// <returns>The code point's grapheme break type.</returns>
        public static GraphemeBreakClass GetGraphemeClusterBreak(int codepoint)
        {
            return (GraphemeBreakClass)s_graphemeBreakTrie.Get(codepoint);
        }
    }
}
