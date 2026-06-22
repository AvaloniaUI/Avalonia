namespace Avalonia.Media.TextFormatting.Unicode
{
    /// <summary>
    ///     Helper for looking up unicode character class information.
    ///     This file contains only the packing-layout constants; the trie-backed
    ///     lookup helpers live in <c>UnicodeData.Lookups.cs</c>. The split lets
    ///     the trie generator tool consume the constants without needing the
    ///     generated <c>*Trie</c> classes (which it produces).
    /// </summary>
    internal static partial class UnicodeData
    {
        internal const int CATEGORY_BITS = 6;
        internal const int SCRIPT_BITS = 8;
        internal const int LINEBREAK_BITS = 6;
        internal const int WORDBREAK_BITS = 5;
        internal const int GRAPHEMEBREAK_BITS = 5;
        internal const int INDICCONJUNCTBREAK_BITS = 2;
        internal const int SCRIPTEXTENSIONS_BITS = 7;

        internal const int BIDIPAIREDBRACKED_BITS = 16;
        internal const int BIDIPAIREDBRACKEDTYPE_BITS = 2;
        internal const int BIDICLASS_BITS = 5;

        internal const int SCRIPT_SHIFT = CATEGORY_BITS;
        internal const int LINEBREAK_SHIFT = CATEGORY_BITS + SCRIPT_BITS;
        internal const int WORDBREAK_SHIFT = CATEGORY_BITS + SCRIPT_BITS + LINEBREAK_BITS;
        internal const int SCRIPTEXTENSIONS_SHIFT = CATEGORY_BITS + SCRIPT_BITS + LINEBREAK_BITS + WORDBREAK_BITS;
        internal const int INDICCONJUNCTBREAK_SHIFT = GRAPHEMEBREAK_BITS;

        internal const int BIDIPAIREDBRACKEDTYPE_SHIFT = BIDIPAIREDBRACKED_BITS;
        internal const int BIDICLASS_SHIFT = BIDIPAIREDBRACKED_BITS + BIDIPAIREDBRACKEDTYPE_BITS;

        internal const int CATEGORY_MASK = (1 << CATEGORY_BITS) - 1;
        internal const int SCRIPT_MASK = (1 << SCRIPT_BITS) - 1;
        internal const int LINEBREAK_MASK = (1 << LINEBREAK_BITS) - 1;
        internal const int WORDBREAK_MASK = (1 << WORDBREAK_BITS) - 1;
        internal const int GRAPHEMEBREAK_MASK = (1 << GRAPHEMEBREAK_BITS) - 1;
        internal const int INDICCONJUNCTBREAK_MASK = (1 << INDICCONJUNCTBREAK_BITS) - 1;
        internal const int SCRIPTEXTENSIONS_MASK = (1 << SCRIPTEXTENSIONS_BITS) - 1;

        internal const int BIDIPAIREDBRACKED_MASK = (1 << BIDIPAIREDBRACKED_BITS) - 1;
        internal const int BIDIPAIREDBRACKEDTYPE_MASK = (1 << BIDIPAIREDBRACKEDTYPE_BITS) - 1;
        internal const int BIDICLASS_MASK = (1 << BIDICLASS_BITS) - 1;
    }
}
