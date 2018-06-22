using System;
using Avalonia.Metadata;

namespace Avalonia.Documents
{
    [TrimSurroundingWhitespace]
    public class LineBreak : Inline
    {
        public override void BuildFormattedText(FormattedTextBuilder builder)
        {
            builder.Add(Environment.NewLine, null);
        }
    }
}
