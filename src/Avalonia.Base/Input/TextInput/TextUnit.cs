using Avalonia.Metadata;

namespace Avalonia.Input.TextInput
{
    /// <summary>
    /// A text boundary granularity for navigation and enclosing-range queries.
    /// </summary>
    /// <remarks>
    /// Units do not all strictly nest; each has a precise definition in the <see cref="ITextNavigation"/> contract.
    /// </remarks>
    [Unstable]
    public enum TextUnit
    {
        /// <summary>A user-perceived character (Unicode grapheme cluster), not a UTF-16 code unit.</summary>
        Character,

        /// <summary>A maximal run over which all text formatting/attributes are constant.</summary>
        Format,

        /// <summary>A word per Unicode UAX-29, locale-aware.</summary>
        Word,

        /// <summary>A sentence per Unicode UAX-29.</summary>
        Sentence,

        /// <summary>A visual (laid-out, wrapped) line.</summary>
        Line,

        /// <summary>A logical paragraph (block) delimited by hard breaks.</summary>
        Paragraph,

        /// <summary>A viewport-sized page.</summary>
        Page,

        /// <summary>The whole document.</summary>
        Document
    }
}
