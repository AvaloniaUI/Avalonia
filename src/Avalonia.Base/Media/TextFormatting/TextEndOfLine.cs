﻿namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// A text run that indicates the end of a line.
    /// </summary>
    public class TextEndOfLine : TextRun
    {
        public TextEndOfLine(int textSourceLength = DefaultTextSourceLength)
        {
            Length = textSourceLength;
        }

        public override int Length { get; }
    }
}
