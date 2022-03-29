﻿using System.Collections.Generic;
using Avalonia.Utilities;

namespace Avalonia.Media.TextFormatting
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
        /// <param name="textRunProperties">text run properties of ellispis symbol.</param>
        public TextTrailingWordEllipsis(
            ReadOnlySlice<char> ellipsis,
            double width,
            TextRunProperties textRunProperties
        )
        {
            Width = width;
            Symbol = new TextCharacters(ellipsis, textRunProperties);
        }

        /// <inheritdoc/>
        public sealed override double Width { get; }

        /// <inheritdoc/>
        public sealed override TextRun Symbol { get; }

        public override IReadOnlyList<TextRun>? Collapse(TextLine textLine)
        {
            return TextEllipsisHelper.Collapse(textLine, this, true);
        }
    }
}
