﻿namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// a collapsing properties to collapse whole line toward the end
    /// at word granularity.
    /// </summary>
    public sealed class TextTrailingWordEllipsis : TextCollapsingProperties
    {
        /// <summary>
        /// Construct a text trailing word ellipsis collapsing properties.
        /// </summary>
        /// <param name="ellipsis">Text used as collapsing symbol.</param>
        /// <param name="width">width in which collapsing is constrained to.</param>
        /// <param name="textRunProperties">text run properties of ellipsis symbol.</param>
        public TextTrailingWordEllipsis(
            string ellipsis,
            double width,
            TextRunProperties textRunProperties
        )
        {
            Width = width;
            Symbol = new TextCharacters(ellipsis, textRunProperties);
        }

        /// <inheritdoc/>
        public override double Width { get; }

        /// <inheritdoc/>
        public override TextRun Symbol { get; }

        /// <inheritdoc />
        public override TextRun[]? Collapse(TextLine textLine)
        {
            return TextEllipsisHelper.Collapse(textLine, this, true);
        }
    }
}
