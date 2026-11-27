using System;
using Avalonia.Media.TextFormatting.Unicode;

namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// A group of characters that can be shaped.
    /// </summary>
    public sealed class UnshapedTextRun : TextRun
    {
        public UnshapedTextRun(ReadOnlyMemory<char> text, TextRunProperties properties, sbyte biDiLevel)
            : this(text, properties, biDiLevel, Script.Unknown)
        {
        }

        internal UnshapedTextRun(ReadOnlyMemory<char> text, TextRunProperties properties, sbyte biDiLevel,
            Script script)
        {
            Text = text;
            Properties = properties;
            BidiLevel = biDiLevel;
            Script = script;
        }

        public override int Length
            => Text.Length;

        public override ReadOnlyMemory<char> Text { get; }

        public override TextRunProperties Properties { get; }

        public sbyte BidiLevel { get; }

        /// <summary>
        /// The script of the run's text: Common when it holds no script of its own, Unknown when it was
        /// not itemized.
        /// </summary>
        internal Script Script { get; }
    }
}
