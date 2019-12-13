// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Diagnostics;
using Avalonia.Media.Text.Unicode;

namespace Avalonia.Media.Text
{
    [DebuggerTypeProxy(typeof(TextRunDebuggerProxy))]
    public class TextRun
    {
        public TextRun(
            GlyphRun glyphRun,
            TextFormat textFormat,
            IBrush foreground = null)
        {
            GlyphRun = glyphRun;
            TextFormat = textFormat;
            Foreground = foreground;
        }

        /// <summary>
        ///     Gets the glyph run.
        /// </summary>
        /// <value>
        ///     The glyphs.
        /// </value>
        public GlyphRun GlyphRun { get; }

        /// <summary>
        ///     Gets the text format.
        /// </summary>
        /// <value>
        ///     The text format.
        /// </value>
        public TextFormat TextFormat { get; }

        /// <summary>
        ///     Gets the foreground.
        /// </summary>
        /// <value>
        ///     The drawing effect.
        /// </value>
        public IBrush Foreground { get; }

        internal SplitTextRunResult Split(int length)
        {
            var glyphCount = 0;

            for (var i = 0; i < length;)
            {
                CodepointReader.Read(GlyphRun.Characters, ref i);

                glyphCount++;
            }

            var firstGlyphRun = new GlyphRun(
                TextFormat.Typeface.GlyphTypeface,
                TextFormat.FontSize,
                GlyphRun.GlyphIndices.Take(glyphCount),
                GlyphRun.GlyphAdvances.Take(glyphCount),
                GlyphRun.GlyphOffsets.Take(glyphCount),
                GlyphRun.Characters.Take(length),
                GlyphRun.GlyphClusters.Take(length));

            var firstTextRun = new TextRun(firstGlyphRun, TextFormat, Foreground);

            var secondGlyphRun = new GlyphRun(
                TextFormat.Typeface.GlyphTypeface,
                TextFormat.FontSize,
                GlyphRun.GlyphIndices.Skip(glyphCount),
                GlyphRun.GlyphAdvances.Skip(glyphCount),
                GlyphRun.GlyphOffsets.Skip(glyphCount),
                GlyphRun.Characters.Skip(length),
                GlyphRun.GlyphClusters.Skip(length));

            var secondTextRun = new TextRun(secondGlyphRun, TextFormat, Foreground);

            return new SplitTextRunResult(firstTextRun, secondTextRun);
        }

        internal readonly struct SplitTextRunResult
        {
            public SplitTextRunResult(TextRun first, TextRun second)
            {
                First = first;

                Second = second;
            }

            /// <summary>
            ///     Gets the first text run.
            /// </summary>
            /// <value>
            ///     The first text run.
            /// </value>
            public TextRun First { get; }

            /// <summary>
            ///     Gets the second text run.
            /// </summary>
            /// <value>
            ///     The second text run.
            /// </value>
            public TextRun Second { get; }
        }

        private class TextRunDebuggerProxy
        {
            private readonly TextRun _textRun;

            public TextRunDebuggerProxy(TextRun textRun)
            {
                _textRun = textRun;
            }

            public string Text
            {
                get
                {
                    unsafe
                    {
                        fixed (char* charsPtr = _textRun.GlyphRun.Characters.AsSpan())
                        {
                            return new string(charsPtr, 0, _textRun.GlyphRun.Characters.Length);
                        }
                    }
                }
            }

            public GlyphRun GlyphRun => _textRun.GlyphRun;

            public TextFormat TextFormat => _textRun.TextFormat;

            public IBrush Foreground => _textRun.Foreground;
        }
    }
}
