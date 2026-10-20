using System;
using Avalonia.Media.TextFormatting.Unicode;

namespace Avalonia.Input.TextInput;

/// <summary>
/// Keeps addresses and identifiers intact when splitting spell-check text.
/// </summary>
internal static class SpellCheckTokenization
{
    public static bool IsBoundary(char c) =>
        char.IsWhiteSpace(c) || c is ',' or ';' or '(' or ')' or '[' or ']' or '{' or '}' or '<' or '>' or '"';

    public static bool IsInsideToken(string text, int index) =>
        index > 0 && index < text.Length && !IsBoundary(text[index - 1]) && !IsBoundary(text[index]);

    public static int Start(string text, int index, int minimum = 0)
    {
        index = Math.Clamp(index, minimum, text.Length);
        while (index > minimum && !IsBoundary(text[index - 1]))
            index--;
        return index;
    }

    public static int End(string text, int index, int maximum = int.MaxValue)
    {
        maximum = Math.Min(maximum, text.Length);
        index = Math.Clamp(index, 0, maximum);
        while (index < maximum && !IsBoundary(text[index]))
            index++;
        return index;
    }

    public static bool RequiresWholeToken(ReadOnlySpan<char> token)
    {
        for (var i = 0; i < token.Length; i++)
        {
            var c = token[i];

            if (char.IsNumber(c) || c is '@' or '/' or '\\' or '_')
                return true;

            if (c == '.' && i > 0 && i < token.Length - 1 &&
                char.IsLetterOrDigit(token[i - 1]) && char.IsLetterOrDigit(token[i + 1]))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Finds word boundaries without rescanning previous chunks or splitting addresses and identifiers.
    /// </summary>
    internal sealed class WordBoundaryFinder(string text)
    {
        private int _tokenStart;
        private int _tokenEnd = -1;
        private bool _requiresWholeToken;
        private int _wordStart;
        private int _wordEnd;

        public int Start(int index) => IsInsideWord(index) ? _wordStart : index;

        public int End(int index) => IsInsideWord(index) ? _wordEnd : index;

        private bool IsInsideWord(int index)
        {
            if (!IsInsideToken(text, index))
                return false;

            if (index < _tokenStart || index > _tokenEnd)
            {
                _tokenStart = SpellCheckTokenization.Start(text, index);
                _tokenEnd = SpellCheckTokenization.End(text, index);
                _requiresWholeToken = RequiresWholeToken(text.AsSpan(_tokenStart, _tokenEnd - _tokenStart));
                _wordStart = _wordEnd = _tokenStart;
            }

            if (_requiresWholeToken)
            {
                _wordStart = _tokenStart;
                _wordEnd = _tokenEnd;
            }
            else
            {
                if (index < _wordStart)
                    _wordStart = _wordEnd = _tokenStart;

                var offset = _wordEnd;
                var words = new WordBreakEnumerator(text.AsSpan(offset, _tokenEnd - offset));

                while (_wordEnd < index && words.MoveNext(out var word))
                {
                    _wordStart = offset + word.Offset;
                    _wordEnd = _wordStart + word.Length;
                }
            }

            return _wordStart < index && index < _wordEnd;
        }
    }
}
