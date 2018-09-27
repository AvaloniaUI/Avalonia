// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;

namespace Avalonia.Skia
{
    public class SKTextLine
    {
        public SKTextLine(int startingIndex, int length, IReadOnlyList<SKTextRun> textRuns, SKTextLineMetrics lineMetrics)
        {
            StartingIndex = startingIndex;
            Length = length;
            TextRuns = textRuns;
            LineMetrics = lineMetrics;
        }

        public int StartingIndex { get; }

        public int Length { get; }

        public IReadOnlyList<SKTextRun> TextRuns { get; }

        public SKTextLineMetrics LineMetrics { get; }
    }
}
