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

        public UnshapedTextRun(ReadOnlyMemory<char> text, TextRunProperties properties, sbyte biDiLevel, Script script)
        {
            Text = text;
            Properties = properties;
            BidiLevel = biDiLevel;
            Script = script != Script.Unknown ? script : GetScript(text.Span);
        }

        public override int Length
            => Text.Length;

        public override ReadOnlyMemory<char> Text { get; }

        public override TextRunProperties Properties { get; }

        public sbyte BidiLevel { get; }

        public Script Script { get; }

        private static Script GetScript(ReadOnlySpan<char> text)
        {
            var enumerator = new CodepointEnumerator(text);
            while (enumerator.MoveNext(out var cp))
            {
                var s = cp.Script;
                if (s != Script.Common && s != Script.Inherited && s != Script.Unknown)
                {
                    return s;
                }
            }
            return Script.Common;
        }
    }
}
