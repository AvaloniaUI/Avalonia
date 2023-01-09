using System.Diagnostics;

namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// Represents a portion of a <see cref="TextLine"/> object.
    /// </summary>
    [DebuggerTypeProxy(typeof(TextRunDebuggerProxy))]
    public abstract class TextRun
    {
        public const int DefaultTextSourceLength = 1;

        /// <summary>
        ///  Gets the text source length.
        /// </summary>
        public virtual int Length => DefaultTextSourceLength;

        /// <summary>
        /// Gets the text run's text.
        /// </summary>
        public virtual CharacterBufferReference CharacterBufferReference => default;

        /// <summary>
        /// A set of properties shared by every characters in the run
        /// </summary>
        public virtual TextRunProperties? Properties => null;

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
                        var characterBuffer = new CharacterBufferRange(_textRun.CharacterBufferReference, _textRun.Length);

                        fixed (char* charsPtr = characterBuffer.Span)
                        {
                            return new string(charsPtr, _textRun.CharacterBufferReference.OffsetToFirstChar, _textRun.Length);
                        }
                    }
                }
            }

            public TextRunProperties? Properties => _textRun.Properties;
        }
    }
}
