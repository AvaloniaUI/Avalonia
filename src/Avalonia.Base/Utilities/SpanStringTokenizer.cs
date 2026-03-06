using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using static System.Char;

namespace Avalonia.Utilities
{
    internal ref struct SpanStringTokenizer
    {
        private const char DefaultSeparatorChar = ',';

        private readonly ReadOnlySpan<char> _s;
        private readonly int _length;
        private readonly char _separator;
        private readonly string? _exceptionMessage;
        private readonly IFormatProvider _formatProvider;
        private int _index;
        private int _tokenIndex;
        private int _tokenLength;

        public SpanStringTokenizer(string s, IFormatProvider formatProvider, string? exceptionMessage = null)
            : this(s.AsSpan(), GetSeparatorFromFormatProvider(formatProvider), exceptionMessage)
        {
            _formatProvider = formatProvider;
        }

        public SpanStringTokenizer(string s, char separator = DefaultSeparatorChar, string? exceptionMessage = null)
            : this(s.AsSpan(), separator, exceptionMessage)
        {
        }

        public SpanStringTokenizer(ReadOnlySpan<char> s, IFormatProvider formatProvider, string? exceptionMessage = null)
            : this(s, GetSeparatorFromFormatProvider(formatProvider), exceptionMessage)
        {
            _formatProvider = formatProvider;
        }

        public SpanStringTokenizer(ReadOnlySpan<char> s, char separator = DefaultSeparatorChar, string? exceptionMessage = null)
        {
            _s = s;
            _length = s.Length;
            _separator = separator;
            _exceptionMessage = exceptionMessage;
            _formatProvider = CultureInfo.InvariantCulture;
            _index = 0;
            _tokenIndex = -1;
            _tokenLength = 0;

            while (_index < _length && IsWhiteSpace(_s[_index]))
            {
                _index++;
            }
        }

        public int CurrentTokenIndex => _tokenIndex;

        public string? CurrentToken => _tokenIndex < 0 ? null : _s.Slice(_tokenIndex, _tokenLength).ToString();

        public ReadOnlySpan<char> CurrentTokenSpan => _tokenIndex < 0 ? ReadOnlySpan<char>.Empty : _s.Slice(_tokenIndex, _tokenLength);

        public void Dispose()
        {
            if (_index != _length)
            {
                throw GetFormatException();
            }
        }

        public bool TryReadInt32(out Int32 result, char? separator = null)
        {
            if (TryReadSpan(out var stringResult, separator) &&
                SpanHelpers.TryParseInt(stringResult, NumberStyles.Integer, _formatProvider, out result))
            {
                return true;
            }
            else
            {
                result = default;
                return false;
            }
        }

        public int ReadInt32(char? separator = null)
        {
            if (!TryReadInt32(out var result, separator))
            {
                throw GetFormatException();
            }

            return result;
        }

        public bool TryReadDouble(out double result, char? separator = null)
        {
            if (TryReadSpan(out var stringResult, separator) &&
                SpanHelpers.TryParseDouble(stringResult, NumberStyles.Float, _formatProvider, out result))
            {
                return true;
            }
            else
            {
                result = default;
                return false;
            }
        }

        public double ReadDouble(char? separator = null)
        {
            if (!TryReadDouble(out var result, separator))
            {
                throw GetFormatException();
            }

            return result;
        }

        public bool TryReadString([NotNull] out string result, char? separator = null)
        {
            var success = TryReadToken(separator ?? _separator);
            result = CurrentTokenSpan.ToString();
            return success;
        }

        public string ReadString(char? separator = null)
        {
            if (!TryReadString(out var result, separator))
            {
                throw GetFormatException();
            }

            return result;
        }

        public bool TryReadSpan(out ReadOnlySpan<char> result, char? separator = null)
        {
            var success = TryReadToken(separator ?? _separator);
            result = CurrentTokenSpan;
            return success;
        }

        public ReadOnlySpan<char> ReadSpan(char? separator = null)
        {
            if (!TryReadSpan(out var result, separator))
            {
                throw GetFormatException();
            }

            return result;
        }

        private bool TryReadToken(char separator)
        {
            _tokenIndex = -1;

            if (_index >= _length)
            {
                return false;
            }

            var c = _s[_index];

            var index = _index;
            var length = 0;

            while (_index < _length)
            {
                c = _s[_index];

                if (IsWhiteSpace(c) || c == separator)
                {
                    break;
                }

                _index++;
                length++;
            }

            SkipToNextToken(separator);

            _tokenIndex = index;
            _tokenLength = length;

            if (_tokenLength < 1)
            {
                throw GetFormatException();
            }

            return true;
        }

        private void SkipToNextToken(char separator)
        {
            if (_index < _length)
            {
                var c = _s[_index];

                if (c != separator && !IsWhiteSpace(c))
                {
                    throw GetFormatException();
                }

                var length = 0;

                while (_index < _length)
                {
                    c = _s[_index];

                    if (c == separator)
                    {
                        length++;
                        _index++;

                        if (length > 1)
                        {
                            throw GetFormatException();
                        }
                    }
                    else
                    {
                        if (!IsWhiteSpace(c))
                        {
                            break;
                        }

                        _index++;
                    }
                }

                if (length > 0 && _index >= _length)
                {
                    throw GetFormatException();
                }
            }
        }

        private FormatException GetFormatException() =>
            _exceptionMessage != null ? new FormatException(_exceptionMessage) : new FormatException();

        private static char GetSeparatorFromFormatProvider(IFormatProvider provider)
        {
            var c = DefaultSeparatorChar;

            var formatInfo = NumberFormatInfo.GetInstance(provider);
            if (formatInfo.NumberDecimalSeparator.Length > 0 && c == formatInfo.NumberDecimalSeparator[0])
            {
                c = ';';
            }

            return c;
        }
    }
}
