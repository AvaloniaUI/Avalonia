using System.Collections.Generic;
using Avalonia.Utilities;

namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// A collapsing properties to collapse whole line toward the end
    /// at character granularity.
    /// </summary>
    public sealed class TextTrailingCharacterEllipsis : TextCollapsingProperties
    {
        /// <summary>
        /// Construct a text trailing character ellipsis collapsing properties
        /// </summary>
        /// <param name="ellipsis">Text used as collapsing symbol.</param>
        /// <param name="width">Width in which collapsing is constrained to.</param>
        /// <param name="textRunProperties">Text run properties of ellipsis symbol.</param>
        public TextTrailingCharacterEllipsis(ReadOnlySlice<char> ellipsis, double width, TextRunProperties textRunProperties)
        {
            Width = width;
            Symbol = new TextCharacters(ellipsis, textRunProperties);
        }

        /// <inheritdoc/>
        public override double Width { get; }

        /// <inheritdoc/>
        public override TextRun Symbol { get; }

        public override List<DrawableTextRun>? Collapse(TextLine textLine)
        {
            return TextEllipsisHelper.Collapse(textLine, this, false);
        }
    }
}
