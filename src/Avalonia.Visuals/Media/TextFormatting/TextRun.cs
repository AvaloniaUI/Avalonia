// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Diagnostics;
using Avalonia.Utility;

namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// Represents a portion of a <see cref="TextLine"/> object.
    /// </summary>
    [DebuggerTypeProxy(typeof(TextRunDebuggerProxy))]
    public abstract class TextRun
    {
        /// <summary>
        /// Gets the text run's text.
        /// </summary>
        public ReadOnlySlice<char> Text { get; protected set; }

        /// <summary>
        /// Gets the text run's style.
        /// </summary>
        public TextStyle Style { get; protected set; }

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
                        fixed (char* charsPtr = _textRun.Text.Buffer.Span)
                        {
                            return new string(charsPtr, 0, _textRun.Text.Length);
                        }
                    }
                }
            }

            public TextStyle Style => _textRun.Style;
        }
    }
}
