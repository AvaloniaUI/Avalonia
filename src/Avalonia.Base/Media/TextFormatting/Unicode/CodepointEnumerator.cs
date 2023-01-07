using System;

namespace Avalonia.Media.TextFormatting.Unicode
{
    public ref struct CodepointEnumerator
    {
        private CharacterBufferRange _text;

        public CodepointEnumerator(CharacterBufferRange text)
        {
            _text = text;
            Current = Codepoint.ReplacementCodepoint;
        }

        /// <summary>
        /// Gets the current <see cref="Codepoint"/>.
        /// </summary>
        public Codepoint Current { get; private set; }

        /// <summary>
        /// Moves to the next <see cref="Codepoint"/>.
        /// </summary>
        /// <returns></returns>
        public bool MoveNext()
        {
            if (_text.IsEmpty)
            {
                Current = Codepoint.ReplacementCodepoint;

                return false;
            }

            Current = Codepoint.ReadAt(_text.Span, 0, out var count);

            _text = _text.Skip(count);

            return true;
        }
    }
}
