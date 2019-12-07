// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Media.Text;
using Avalonia.Utility;

namespace Avalonia.Platform
{
    /// <summary>
    ///     An abstraction that is used during the text layout process.
    /// </summary>
    public interface ITextFormatter
    {
        /// <summary>
        ///     Creates runs with unique properties for the specified text pointer. This may return runs with a non default typeface.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="defaultTypeface">The default typeface.</param>
        /// <param name="defaultFontSize">The default font size.</param>
        /// <returns>The text runs.</returns>
        List<TextRunProperties> CreateTextRuns(ReadOnlySlice<char> text, Typeface defaultTypeface, double defaultFontSize);

        /// <summary>
        ///     Formats a list of text runs 
        /// </summary>
        /// <param name="text">The text to format.</param>
        /// <param name="textRunProperties">The run properties to apply.
        /// This can contain properties of a greater range than the supplied text covers.
        /// Properties are removed from the list when applied.</param>
        /// <returns></returns>
        List<TextRun> FormatTextRuns(ReadOnlySlice<char> text, List<TextRunProperties> textRunProperties);

        /// <summary>
        ///     Shapes the specified region within the text and returns a resulting glyph run.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="textFormat">The text format.</param>
        /// <returns></returns>
        GlyphRun CreateShapedGlyphRun(ReadOnlySlice<char> text, TextFormat textFormat);
    }
}
