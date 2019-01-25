// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;

namespace Avalonia.Skia.Text
{
    public class SKTextLine
    {
        public SKTextLine(SKTextPointer textPointer, IReadOnlyList<SKTextRun> textRuns, SKTextLineMetrics lineMetrics)
        {
            TextPointer = textPointer;
            TextRuns = textRuns;
            LineMetrics = lineMetrics;
        }

        /// <summary>
        /// Gets the text pointer.
        /// </summary>
        /// <value>
        /// The text pointer.
        /// </value>
        public SKTextPointer TextPointer { get; }

        /// <summary>
        /// Gets the text runs.
        /// </summary>
        /// <value>
        /// The text runs.
        /// </value>
        public IReadOnlyList<SKTextRun> TextRuns { get; }

        /// <summary>
        /// Gets the line metrics.
        /// </summary>
        /// <value>
        /// The line metrics.
        /// </value>
        public SKTextLineMetrics LineMetrics { get; }

        public override string ToString()
        {
            return $"{TextPointer.StartingIndex}:{TextPointer.Length}";
        }
    }
}
