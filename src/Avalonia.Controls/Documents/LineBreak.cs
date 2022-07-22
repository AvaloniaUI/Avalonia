using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Media.TextFormatting;
using Avalonia.Metadata;

namespace Avalonia.Controls.Documents 
{
    /// <summary>
    /// LineBreak element that forces a line breaking. 
    /// </summary>
    [TrimSurroundingWhitespace]
    public class LineBreak : Inline
    {
        /// <summary>
        /// Creates a new LineBreak instance.
        /// </summary>
        public LineBreak()
        {
        }

        internal override void BuildTextRun(IList<TextRun> textRuns)
        {
            textRuns.Add(new TextEndOfLine(Environment.NewLine.Length));
        }

        internal override void AppendText(StringBuilder stringBuilder)
        {
            stringBuilder.Append(Environment.NewLine);
        }
    }
}

