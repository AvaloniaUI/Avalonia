// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Utility;

namespace Avalonia.Platform
{
    /// <summary>
    /// An abstraction that is used produce shaped text.
    /// </summary>
    public interface ITextShaperImpl
    {
        /// <summary>
        /// Shapes the specified region within the text and returns a resulting glyph run.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="textFormat">The text format.</param>
        /// <returns>A shaped glyph run.</returns>
        GlyphRun ShapeText(ReadOnlySlice<char> text, TextFormat textFormat);
    }
}
