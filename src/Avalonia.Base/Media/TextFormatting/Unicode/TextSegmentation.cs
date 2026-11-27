using System;

namespace Avalonia.Media.TextFormatting.Unicode
{
    /// <summary>
    /// Pure text segmentation over a <see cref="ReadOnlySpan{T}"/> of characters, with offsets in code units.
    /// </summary>
    internal static class TextSegmentation
    {
        private const uint CarriageReturn = 0x000D;
        private const uint LineFeed = 0x000A;
        private const uint ZeroWidthJoiner = 0x200D;

        /// <summary>
        /// Gets the offset that backspace at <paramref name="offset"/> deletes back to.
        /// </summary>
        /// <remarks>
        /// Backspace deletes less than a grapheme cluster, so a cluster such as a consonant with vowel signs
        /// or an emoji with a skin tone modifier can be corrected one piece at a time. It deletes the
        /// codepoint before the offset, widened by the first matching rule:
        /// <list type="bullet">
        /// <item>BS1: CR LF is deleted as one unit, and no other rule extends a line break.</item>
        /// <item>BS2: A variation selector is deleted with the codepoint it modifies. A selector after a
        /// line break, a control, or a mark has nothing to modify and is deleted alone.</item>
        /// <item>BS3: A regional indicator that completes a flag is deleted with its partner.</item>
        /// <item>BS4: A tag character is deleted with the tag characters before it, which leaves the base
        /// of a tag sequence such as the black flag of a subdivision flag.</item>
        /// </list>
        /// Then:
        /// <list type="bullet">
        /// <item>BS5: A zero width joiner between two pictographs of an emoji zwj sequence is deleted with
        /// the pictograph after it. Elsewhere the joiner stays: in Indic scripts it selects a conjunct form,
        /// such as the half form in U+0915 U+094D U+200D U+0937, which must survive retyping the last
        /// consonant.</item>
        /// </list>
        /// Deleting only part of those sequences leaves text nobody typed on purpose: a text-style glyph, a
        /// lone regional indicator, or invisible tags and joiners that each take another press.
        /// </remarks>
        /// <param name="offset">The caret offset.</param>
        /// <param name="text">The text the caret is in.</param>
        /// <returns>The offset where the deletion starts.</returns>
        public static int BackspaceStart(int offset, ReadOnlySpan<char> text)
        {
            offset = Math.Min(offset, text.Length);

            if (offset <= 0)
            {
                return 0;
            }

            var start = PreviousCodepointStart(offset, text, out var deleted);

            // BS1
            if (deleted.Value == LineFeed)
            {
                return start > 0 && text[start - 1] == CarriageReturn ? start - 1 : start;
            }

            if (deleted.Value == CarriageReturn)
            {
                return start;
            }

            // The first codepoint of the deletion, which decides whether BS5 applies.
            var first = deleted;

            // BS2
            if (IsVariationSelector(deleted))
            {
                if (start > 0)
                {
                    var baseStart = PreviousCodepointStart(start, text, out var baseCodepoint);

                    if (CanTakeVariationSelector(baseCodepoint))
                    {
                        start = baseStart;
                        first = baseCodepoint;
                    }
                }
            }
            // BS3
            else if (IsRegionalIndicator(deleted) && CountRegionalIndicatorsBefore(start, text) % 2 == 1)
            {
                start = PreviousCodepointStart(start, text, out first);
            }
            // BS4
            else if (IsTag(deleted))
            {
                while (start > 0)
                {
                    var previousStart = PreviousCodepointStart(start, text, out var previous);

                    if (!IsTag(previous))
                    {
                        break;
                    }

                    start = previousStart;
                }
            }

            // BS5
            if (IsExtendedPictographic(first) && start > 0 && text[start - 1] == ZeroWidthJoiner &&
                EndsWithEmojiZwjElement(start - 1, text))
            {
                start--;
            }

            return start;
        }

        /// <summary>
        /// Gets whether the text before <paramref name="offset"/> is a pictograph followed by any number of
        /// extending codepoints such as variation selectors, skin tone modifiers, or tags.
        /// </summary>
        private static bool EndsWithEmojiZwjElement(int offset, ReadOnlySpan<char> text)
        {
            while (offset > 0)
            {
                offset = PreviousCodepointStart(offset, text, out var codepoint);

                if (codepoint.GraphemeBreakClass != GraphemeBreakClass.Extend)
                {
                    return IsExtendedPictographic(codepoint);
                }
            }

            return false;
        }

        private static bool CanTakeVariationSelector(Codepoint codepoint)
            => codepoint.GraphemeBreakClass is not (GraphemeBreakClass.Control or GraphemeBreakClass.CR or
                GraphemeBreakClass.LF or GraphemeBreakClass.Extend or GraphemeBreakClass.ZWJ);

        private static bool IsExtendedPictographic(Codepoint codepoint)
            => codepoint.GraphemeBreakClass == GraphemeBreakClass.ExtendedPictographic;

        /// <summary>
        /// Gets the start of the codepoint that ends at <paramref name="offset"/>.
        /// </summary>
        private static int PreviousCodepointStart(int offset, ReadOnlySpan<char> text, out Codepoint codepoint)
        {
            // Reading at a low surrogate decodes the pair it closes; any other unit starts its own codepoint.
            codepoint = Codepoint.ReadAt(text, offset - 1, out var count);

            return char.IsLowSurrogate(text[offset - 1]) ? offset - count : offset - 1;
        }

        private static int CountRegionalIndicatorsBefore(int offset, ReadOnlySpan<char> text)
        {
            var count = 0;

            while (offset > 0)
            {
                offset = PreviousCodepointStart(offset, text, out var codepoint);

                if (!IsRegionalIndicator(codepoint))
                {
                    break;
                }

                count++;
            }

            return count;
        }

        // The Tags block: tag spec characters and the cancel tag.
        private static bool IsTag(Codepoint codepoint)
            => codepoint.Value is >= 0xE0020 and <= 0xE007F;

        private static bool IsRegionalIndicator(Codepoint codepoint)
            => codepoint.GraphemeBreakClass == GraphemeBreakClass.RegionalIndicator;

        // The Unicode Variation_Selector property: Mongolian free variation selectors, the standardized
        // and emoji presentation selectors, and the ideographic variation selectors.
        private static bool IsVariationSelector(Codepoint codepoint)
            => codepoint.Value is >= 0x180B and <= 0x180D or 0x180F or >= 0xFE00 and <= 0xFE0F
                or >= 0xE0100 and <= 0xE01EF;
    }
}
