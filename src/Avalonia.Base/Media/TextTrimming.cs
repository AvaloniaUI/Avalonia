using System;
using Avalonia.Media.TextFormatting;

namespace Avalonia.Media
{
    /// <summary>
    /// Describes how text is trimmed when it overflows.
    /// </summary>
    public abstract class TextTrimming
    {
        internal const string DefaultEllipsisChar = "\u2026";

        /// <summary>
        /// Text is not trimmed.
        /// </summary>
        public static TextTrimming None { get; } = new TextNoneTrimming();

        /// <summary>
        /// Text is trimmed at a character boundary. An ellipsis (...) is drawn in place of remaining text.
        /// </summary>
        public static TextTrimming CharacterEllipsis { get; } = new TextTrailingTrimming(DefaultEllipsisChar, false);

        /// <summary>
        /// Text is trimmed at a word boundary. An ellipsis (...) is drawn in place of remaining text.
        /// </summary>
        public static TextTrimming WordEllipsis { get; } = new TextTrailingTrimming(DefaultEllipsisChar, true);

        /// <summary>
        /// Text is trimmed after a given prefix length. An ellipsis (...) is drawn in between prefix and suffix and represents remaining text.
        /// </summary>
        public static TextTrimming PrefixCharacterEllipsis { get; } = new TextLeadingPrefixTrimming(DefaultEllipsisChar, 8);

        /// <summary>
        /// Text is trimmed at a character boundary starting from the beginning. An ellipsis (...) is drawn in place of remaining text.
        /// </summary>
        public static TextTrimming LeadingCharacterEllipsis { get; } = new TextLeadingPrefixTrimming(DefaultEllipsisChar, 0);

        /// <summary>
        /// Creates properties that will be used for collapsing lines of text.
        /// </summary>
        /// <param name="createInfo">Contextual info about text that will be collapsed.</param>
        public abstract TextCollapsingProperties CreateCollapsingProperties(TextCollapsingCreateInfo createInfo);

        /// <summary>
        /// Parses a text trimming string. Names must match static properties defined in this class.
        /// </summary>
        /// <param name="s">The text trimming string.</param>
        /// <returns>The <see cref="TextTrimming"/>.</returns>
        public static TextTrimming Parse(string s)
        {
            bool Matches(string name)
            {
                return name.Equals(s, StringComparison.OrdinalIgnoreCase);
            }

            if (Matches(nameof(None)))
            {
                return None;
            }
            if (Matches(nameof(CharacterEllipsis)))
            {
                return CharacterEllipsis;
            }
            else if (Matches(nameof(WordEllipsis)))
            {
                return WordEllipsis;
            }
            else if (Matches(nameof(PrefixCharacterEllipsis)))
            {
                return PrefixCharacterEllipsis;
            }

            throw new FormatException($"Invalid text trimming string: '{s}'.");
        }
    }
}
