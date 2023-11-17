using System;
using System.Globalization;

namespace Avalonia.Rendering.Composition.Expressions
{
    /// <summary>
    /// Helper class for composition expression parser
    /// </summary>
    internal ref struct TokenParser
    {
        private ReadOnlySpan<char> _s;
        public int Position { get; private set; }
        public TokenParser(ReadOnlySpan<char> s)
        {
            _s = s;
            Position = 0;
        }

        public void SkipWhitespace()
        {
            while (true)
            {
                if (_s.Length > 0 && char.IsWhiteSpace(_s[0]))
                    Advance(1);
                else
                    return;
            }
        }

        public bool NextIsWhitespace() => _s.Length > 0 && char.IsWhiteSpace(_s[0]);

        static bool IsAlphaNumeric(char ch) => (ch >= '0' && ch <= '9') || (ch >= 'a' && ch <= 'z') ||
                                               (ch >= 'A' && ch <= 'Z');

        public bool TryConsume(char c)
        {
            SkipWhitespace();
            if (_s.Length == 0 || _s[0] != c)
                return false;

            Advance(1);
            return true;
        }
        public bool TryConsume(string s)
        {
            SkipWhitespace();
            if (_s.Length < s.Length)
                return false;
            for (var c = 0; c < s.Length; c++)
            {
                if (_s[c] != s[c])
                    return false;
            }

            Advance(s.Length);
            return true;
        }
        
        public bool TryConsumeAny(ReadOnlySpan<char> chars, out char token)
        {
            SkipWhitespace();
            token = default;
            if (_s.Length == 0)
                return false;

            foreach (var c in chars)
            {
                if (c == _s[0])
                {
                    token = c;
                    Advance(1);
                    return true;
                }
            }

            return false;
        }

        
        public bool TryParseKeyword(string keyword)
        {
            SkipWhitespace();
            if (keyword.Length > _s.Length)
                return false;
            for(var c=0; c<keyword.Length;c++)
                if (keyword[c] != _s[c])
                    return false;

            if (_s.Length > keyword.Length && IsAlphaNumeric(_s[keyword.Length]))
                return false;

            Advance(keyword.Length);
            return true;
        }
        
        public bool TryParseKeywordLowerCase(string keywordInLowerCase)
        {
            SkipWhitespace();
            if (keywordInLowerCase.Length > _s.Length)
                return false;
            for(var c=0; c<keywordInLowerCase.Length;c++)
                if (keywordInLowerCase[c] != char.ToLowerInvariant(_s[c]))
                    return false;
            
            if (_s.Length > keywordInLowerCase.Length && IsAlphaNumeric(_s[keywordInLowerCase.Length]))
                return false;
            
            Advance(keywordInLowerCase.Length);
            return true;
        }

        public void Advance(int c)
        {
            _s = _s.Slice(c);
            Position += c;
        }

        public int Length => _s.Length;

        public bool TryParseIdentifier(ReadOnlySpan<char> extraValidChars, out ReadOnlySpan<char> res)
        {
            res = ReadOnlySpan<char>.Empty;
            SkipWhitespace();
            if (_s.Length == 0)
                return false;
            var first = _s[0];
            if (!((first >= 'a' && first <= 'z') || (first >= 'A' && first <= 'Z')))
                return false;
            int len = 1;
            for (var c = 1; c < _s.Length; c++)
            {
                var ch = _s[c];
                if (IsAlphaNumeric(ch))
                    len++;
                else
                {
                    var found = false;
                    foreach(var vc in extraValidChars)
                        if (vc == ch)
                        {
                            found = true;
                            break;
                        }

                    if (found)
                        len++;
                    else
                        break;
                }
            }

            res = _s.Slice(0, len);
            Advance(len);
            return true;
        }
        
        public bool TryParseIdentifier(out ReadOnlySpan<char> res)
        {
            res = ReadOnlySpan<char>.Empty;
            SkipWhitespace();
            if (_s.Length == 0)
                return false;
            var first = _s[0];
            if (!((first >= 'a' && first <= 'z') || (first >= 'A' && first <= 'Z')))
                return false;
            int len = 1;
            for (var c = 1; c < _s.Length; c++)
            {
                var ch = _s[c];
                if (IsAlphaNumeric(ch))
                    len++;
                else
                    break;
            }

            res = _s.Slice(0, len);
            Advance(len);
            return true;
        }
        
        public bool TryParseCall(out ReadOnlySpan<char> res)
        {
            res = ReadOnlySpan<char>.Empty;
            SkipWhitespace();
            if (_s.Length == 0)
                return false;
            var first = _s[0];
            if (!((first >= 'a' && first <= 'z') || (first >= 'A' && first <= 'Z')))
                return false;
            int len = 1;
            for (var c = 1; c < _s.Length; c++)
            {
                var ch = _s[c];
                if ((ch >= '0' && ch <= '9') || (ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch<= 'Z') || ch == '.')
                    len++;
                else
                    break;
            }
            
            res = _s.Slice(0, len);

            // Find '('
            for (var c = len; c < _s.Length; c++)
            {
                if(char.IsWhiteSpace(_s[c]))
                    continue;
                if(_s[c]=='(')
                {
                    Advance(c + 1);
                    return true;
                }

                return false;

            }

            return false;

        }
        
        
        public bool TryParseFloat(out float res)
        {
            res = 0;
            SkipWhitespace();
            if (_s.Length == 0)
                return false;
            
            var len = 0;
            var dotCount = 0;
            for (var c = 0; c < _s.Length; c++)
            {
                var ch = _s[c];
                if (ch >= '0' && ch <= '9')
                    len = c + 1;
                else if (ch == '.' && dotCount == 0)
                {
                    len = c + 1;
                    dotCount++;
                }
                else if (ch == '-')
                {
                    if (len != 0)
                        break;
                    len = c + 1;
                }
                else
                    break;
            }

            var span = _s.Slice(0, len);

#if NETSTANDARD2_0
            if (!float.TryParse(span.ToString(), NumberStyles.Number, CultureInfo.InvariantCulture, out res))
                return false;
#else
            if (!float.TryParse(span, NumberStyles.Number, CultureInfo.InvariantCulture, out res))
                return false;
#endif
            Advance(len);
            return true;
        }
        
        public bool TryParseDouble(out double res)
        {
            res = 0;
            SkipWhitespace();
            if (_s.Length == 0)
                return false;
            
            var len = 0;
            var dotCount = 0;
            for (var c = 0; c < _s.Length; c++)
            {
                var ch = _s[c];
                if (ch >= '0' && ch <= '9')
                    len = c + 1;
                else if (ch == '.' && dotCount == 0)
                {
                    len = c + 1;
                    dotCount++;
                }
                else if (ch == '-')
                {
                    if (len != 0)
                        return false;
                    len = c + 1;
                }
                else
                    break;
            }

            var span = _s.Slice(0, len);

#if NETSTANDARD2_0
            if (!double.TryParse(span.ToString(), NumberStyles.Number, CultureInfo.InvariantCulture, out res))
                return false;
#else
            if (!double.TryParse(span, NumberStyles.Number, CultureInfo.InvariantCulture, out res))
                return false;
#endif
            Advance(len);
            return true;
        }

        public bool IsEofWithWhitespace()
        {
            SkipWhitespace();
            return Length == 0;
        }
        
        public override string ToString() => _s.ToString();

    }
}
