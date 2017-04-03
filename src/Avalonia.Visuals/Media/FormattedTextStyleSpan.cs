using System;

namespace Avalonia.Media
{
    public class FormattedTextStyleSpan
    {
        public FormattedTextStyleSpan(
            int startIndex,
            int length,
            IBrush foregroundBrush = null)
        {
            StartIndex = startIndex;
            Length = length;
            ForegroundBrush = foregroundBrush;
        }

        public int StartIndex { get; }
        public int Length { get; }
        public IBrush ForegroundBrush { get; }
    }
}
