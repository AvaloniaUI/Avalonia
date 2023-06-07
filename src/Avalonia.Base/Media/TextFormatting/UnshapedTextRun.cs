using System;

namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// A group of characters that can be shaped.
    /// </summary>
    public sealed class UnshapedTextRun : TextRun
    {
        public UnshapedTextRun(ReadOnlyMemory<char> text, TextRunProperties properties, sbyte biDiLevel)
        {
            Text = text;
            Properties = properties;
            BidiLevel = biDiLevel;
        }

        public override int Length
            => Text.Length;

        public override ReadOnlyMemory<char> Text { get; }

        public override TextRunProperties Properties { get; }

        public sbyte BidiLevel { get; }
    }
}
