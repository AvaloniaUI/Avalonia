namespace Avalonia.Media.TextFormatting.Unicode
{
    /// <summary>
    /// Sentence_Break property values, as defined by UAX #29.
    /// </summary>
    /// <remarks>
    /// The trailing comment on each member is the property value alias used by the Unicode
    /// Character Database. <see cref="SentenceBreakEnumerator"/> applies rules SB1 to SB11 and
    /// SB998 to these classes to place sentence boundaries.
    /// </remarks>
    public enum SentenceBreakClass
    {
        /// <summary>
        /// A code point in none of the other classes, carrying no meaning of its own for the
        /// sentence rules.
        /// </summary>
        Other, //XX

        /// <summary>
        /// U+000D CARRIAGE RETURN. Ends a sentence, and joins to a following
        /// <see cref="LineFeed"/> rather than breaking between the two.
        /// </summary>
        CarriageReturn, //CR

        /// <summary>
        /// U+000A LINE FEED. Ends a sentence.
        /// </summary>
        LineFeed, //LF

        /// <summary>
        /// A combining mark or other code point that attaches to the one before it. Transparent
        /// to the sentence rules, which read through it to the code point it attaches to.
        /// </summary>
        Extend, //EX

        /// <summary>
        /// A paragraph or line separator, such as U+2029 PARAGRAPH SEPARATOR. Ends a sentence.
        /// </summary>
        Sep, //SE

        /// <summary>
        /// A format code point. Transparent to the sentence rules, in the same way as
        /// <see cref="Extend"/>.
        /// </summary>
        Format, //FO

        /// <summary>
        /// Whitespace that is not a paragraph separator. A run of it following a terminator
        /// belongs to the sentence that terminator ends, so the boundary falls after the run.
        /// </summary>
        Sp, //SP

        /// <summary>
        /// A lowercase letter. Reached after a full stop, it marks that stop as an abbreviation
        /// rather than the end of a sentence.
        /// </summary>
        Lower, //LO

        /// <summary>
        /// An uppercase or titlecase letter.
        /// </summary>
        Upper, //UP

        /// <summary>
        /// A letter that is neither uppercase nor lowercase, as in scripts without case such as
        /// Han, Hebrew or Devanagari.
        /// </summary>
        OLetter, //OL

        /// <summary>
        /// A numeric code point. Keeps a full stop between digits from ending a sentence, so that
        /// decimal numbers stay whole.
        /// </summary>
        Numeric, //NU

        /// <summary>
        /// A full stop or equivalent, which may end a sentence or may mark an abbreviation or a
        /// decimal point. Which one it is depends on what surrounds it.
        /// </summary>
        ATerm, //AT

        /// <summary>
        /// A code point that carries a sentence on past a terminator, such as a comma, instead of
        /// allowing a boundary there.
        /// </summary>
        SContinue, //SC

        /// <summary>
        /// A sentence terminator that is unambiguous, such as an exclamation or question mark.
        /// </summary>
        STerm, //ST

        /// <summary>
        /// Closing punctuation, such as a bracket or quotation mark, which may sit between a
        /// terminator and the sentence boundary.
        /// </summary>
        Close, //CL
    }
}
