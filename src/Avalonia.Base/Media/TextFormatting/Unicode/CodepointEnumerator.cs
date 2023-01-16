using System;

namespace Avalonia.Media.TextFormatting.Unicode
{
    public ref struct CodepointEnumerator
    {
        private ReadOnlySpan<char> _text;

        public CodepointEnumerator(ReadOnlySpan<char> text)
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

            Current = Codepoint.ReadAt(_text, 0, out var count);

            _text = _text.Slice(count);

            return true;
        }
    }
}
