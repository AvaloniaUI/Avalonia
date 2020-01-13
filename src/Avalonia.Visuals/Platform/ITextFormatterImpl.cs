// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Media;
using Avalonia.Media.Text;
using Avalonia.Utility;

namespace Avalonia.Platform
{
    /// <summary>
    ///     An abstraction that is used during the text layout process.
    /// </summary>
    public interface ITextFormatterImpl
    {
        /// <summary>
        ///     Creates a shapable text style run with unique properties.
        /// </summary>
        /// <param name="text">The text to create text runs from.</param>
        /// <param name="defaultStyle"></param>
        /// <returns>A list of text runs.</returns>
        TextStyleRun CreateShapableTextStyleRun(ReadOnlySlice<char> text, TextStyle defaultStyle);

        /// <summary>
        ///     Shapes the specified region within the text and returns a resulting glyph run.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="textFormat">The text format.</param>
        /// <returns></returns>
        GlyphRun CreateShapedGlyphRun(ReadOnlySlice<char> text, TextFormat textFormat);
    }
}
