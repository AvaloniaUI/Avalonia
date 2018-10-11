// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;

namespace Avalonia.Skia
{
    public class SKTextLine
    {
        private readonly List<SKTextRun> _textRuns;

        public SKTextLine(int startingIndex, int length, List<SKTextRun> textRuns, SKTextLineMetrics lineMetrics)
        {
            StartingIndex = startingIndex;
            Length = length;
            _textRuns = textRuns;
            LineMetrics = lineMetrics;
        }

        /// <summary>
        /// Gets the starting index.
        /// </summary>
        /// <value>
        /// The starting index.
        /// </value>
        public int StartingIndex { get; }

        /// <summary>
        /// Gets the text line length.
        /// </summary>
        /// <value>
        /// The text line length.
        /// </value>
        public int Length { get; }

        /// <summary>
        /// Gets the text runs.
        /// </summary>
        /// <value>
        /// The text runs.
        /// </value>
        public IReadOnlyList<SKTextRun> TextRuns => _textRuns;

        /// <summary>
        /// Gets the line metrics.
        /// </summary>
        /// <value>
        /// The line metrics.
        /// </value>
        public SKTextLineMetrics LineMetrics { get; }
    }
}
