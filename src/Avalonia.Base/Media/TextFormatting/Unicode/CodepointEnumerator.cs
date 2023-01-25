using System;

namespace Avalonia.Media.TextFormatting.Unicode
{
    public ref struct CodepointEnumerator
    {
        private readonly ReadOnlySpan<char> _text;
        private int _offset;

        public CodepointEnumerator(ReadOnlySpan<char> text)
            => _text = text;

        /// <summary>
        /// Moves to the next <see cref="Codepoint"/>.
        /// </summary>
        /// <returns></returns>
        public bool MoveNext(out Codepoint codepoint)
        {
            if ((uint)_offset >= (uint)_text.Length)
            {
                codepoint = Codepoint.ReplacementCodepoint;
                return false;
            }

            codepoint = Codepoint.ReadAt(_text, _offset, out var count);

            _offset += count;

            return true;
        }
    }
}
