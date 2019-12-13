// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using Avalonia.Utility;

namespace Avalonia.Media.Text
{
    [DebuggerTypeProxy(typeof(TextLineDebuggerProxy))]
    public class TextLine
    {
        public TextLine(ReadOnlySlice<char> text, IReadOnlyList<TextRun> textRuns, TextLineMetrics lineMetrics)
        {
            Text = text;
            TextRuns = textRuns;
            LineMetrics = lineMetrics;
        }

        /// <summary>
        ///     Gets the text pointer.
        /// </summary>
        /// <value>
        /// The text pointer.
        /// </value>
        public ReadOnlySlice<char> Text { get; }

        /// <summary>
        ///     Gets the text runs.
        /// </summary>
        /// <value>
        ///     The text runs.
        /// </value>
        public IReadOnlyList<TextRun> TextRuns { get; }

        /// <summary>
        ///     Gets the line metrics.
        /// </summary>
        /// <value>
        ///     The line metrics.
        /// </value>
        public TextLineMetrics LineMetrics { get; }

        private class TextLineDebuggerProxy
        {
            private readonly TextLine _textLine;

            public TextLineDebuggerProxy(TextLine textLine)
            {
                _textLine = textLine;
            }

            public string Text
            {
                get
                {
                    unsafe
                    {
                        fixed (char* charsPtr = _textLine.Text.AsSpan())
                        {
                            return new string(charsPtr, 0, _textLine.Text.Length);
                        }
                    }
                }
            }

            public IReadOnlyList<TextRun> TextRuns => _textLine.TextRuns;

            public TextLineMetrics LineMetrics => _textLine.LineMetrics;
        }
    }
}
