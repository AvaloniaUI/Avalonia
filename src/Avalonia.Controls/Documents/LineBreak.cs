using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Media.TextFormatting;
using Avalonia.Metadata;
using Avalonia.Utilities;

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

        internal override int BuildRun(StringBuilder stringBuilder,
            IList<ValueSpan<TextRunProperties>> textStyleOverrides, int firstCharacterIndex)
        {
            var length = AppendText(stringBuilder);

            textStyleOverrides.Add(new ValueSpan<TextRunProperties>(firstCharacterIndex, length,
                CreateTextRunProperties()));

            return length;
        }

        internal override int AppendText(StringBuilder stringBuilder)
        {
            var text = Environment.NewLine;

            stringBuilder.Append(text);

            return text.Length;
        }
    }
}

