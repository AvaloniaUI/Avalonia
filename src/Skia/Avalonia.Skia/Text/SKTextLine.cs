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

        public int StartingIndex { get; }

        public int Length { get; }

        public IReadOnlyList<SKTextRun> TextRuns => _textRuns;

        public SKTextLineMetrics LineMetrics { get; }

        public void RemoveTextRun(int index)
        {
            _textRuns.RemoveAt(index);
        }

        public void InsertTextRun(int index, SKTextRun textRun)
        {
            _textRuns.Insert(index, textRun);
        }
    }
}
