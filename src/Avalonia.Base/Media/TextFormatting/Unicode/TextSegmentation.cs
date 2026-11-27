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
        /// <item>BS1: CR LF is deleted as one unit.</item>
        /// <item>BS2: A variation selector is deleted with the codepoint it modifies.</item>
        /// <item>BS3: A regional indicator that completes a flag is deleted with its partner.</item>
        /// </list>
        /// Then, unless the deletion is a line break:
        /// <list type="bullet">
        /// <item>BS4: A zero width joiner before the deletion is deleted with it.</item>
        /// </list>
        /// Deleting only part of those sequences leaves text nobody typed on purpose: a text-style glyph, a
        /// lone regional indicator, or an invisible joiner that takes another press.
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
            if (deleted.Value == LineFeed && start > 0 && text[start - 1] == CarriageReturn)
            {
                return start - 1;
            }

            if (deleted.Value == CarriageReturn)
            {
                return start;
            }

            // BS2
            if (IsVariationSelector(deleted) && start > 0)
            {
                start = PreviousCodepointStart(start, text, out _);
            }
            // BS3
            else if (IsRegionalIndicator(deleted) && CountRegionalIndicatorsBefore(start, text) % 2 == 1)
            {
                start = PreviousCodepointStart(start, text, out _);
            }

            // BS4
            if (start > 0 && text[start - 1] == ZeroWidthJoiner)
            {
                start--;
            }

            return start;
        }

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

        private static bool IsRegionalIndicator(Codepoint codepoint)
            => codepoint.GraphemeBreakClass == GraphemeBreakClass.RegionalIndicator;

        // The Unicode Variation_Selector property: Mongolian free variation selectors, the standardized
        // and emoji presentation selectors, and the ideographic variation selectors.
        private static bool IsVariationSelector(Codepoint codepoint)
            => codepoint.Value is >= 0x180B and <= 0x180D or 0x180F or >= 0xFE00 and <= 0xFE0F
                or >= 0xE0100 and <= 0xE01EF;
    }
}
