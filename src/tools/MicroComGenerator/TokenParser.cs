using System;
using System.Globalization;
using System.IO;

namespace MicroComGenerator
{
    internal ref struct TokenParser
    {
        private ReadOnlySpan<char> _s;
        public int Position { get; private set; }
        public int Line { get; private set; }
        public TokenParser(ReadOnlySpan<char> s)
        {
            _s = s;
            Position = 0;
            Line = 0;
        }

        public void SkipWhitespace()
        {
            while (true)
            {
                if(_s.Length == 0)
                    return;
                if (char.IsWhiteSpace(_s[0]))
                    Advance(1);
                else if (_s[0] == '/' && _s.Length>1)
                {
                    if (_s[1] == '/')
                        SkipOneLineComment();
                    else if (_s[1] == '*')
                        SkipMultiLineComment();
                    else
                        return;
                }
                else
                    return;
            }
        }

        void SkipOneLineComment()
        {
            while (true)
            {
                if (_s.Length > 0 && _s[0] != '\n' && _s[0] != '\r')
                    Advance(1);
                else
                    return;
            }
        }
        
        void SkipMultiLineComment()
        {
            var l = Line;
            var p = Position;
            while (true)
            {
                if (_s.Length == 0)
                    throw new ParseException("No matched */ found for /*", l, p);

                if (_s[0] == '*' && _s.Length > 1 && _s[1] == '/')
                {
                    Advance(2);
                    return;
                }

                Advance(1);
            }
        }

        static bool IsAlphaNumeric(char ch) => (ch >= '0' && ch <= '9') || (ch >= 'a' && ch <= 'z') ||
                                               (ch >= 'A' && ch <= 'Z');

        public void Consume(char c)
        {
            if (!TryConsume(c))
                throw new ParseException("Expected " + c, Line, Position);
        }
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
            while (c > 0)
            {
                if (_s[0] == '\n')
                {
                    Line++;
                    Position = 0;
                }
                else
                    Position++;

                _s = _s.Slice(1);
                c--;
            }
        }

        public int Length => _s.Length;
        public bool Eof
        {
            get
            {
                SkipWhitespace();
                return Length == 0;
            }
        }

        public char Peek
        {
            get
            {
                if (_s.Length == 0)
                    throw new ParseException("Unexpected EOF", Line, Position);
                return _s[0];
            }
        }

        public string ParseIdentifier(ReadOnlySpan<char> extraValidChars)
        {
            if (!TryParseIdentifier(extraValidChars, out var ident))
                throw new ParseException("Identifier expected", Line, Position);
            return ident.ToString();
        }
        
        public string ParseIdentifier()
        {
            if (!TryParseIdentifier(out var ident))
                throw new ParseException("Identifier expected", Line, Position);
            return ident.ToString();
        }
        
        public bool TryParseIdentifier(ReadOnlySpan<char> extraValidChars, out ReadOnlySpan<char> res)
        {
            res = ReadOnlySpan<char>.Empty;
            SkipWhitespace();
            if (_s.Length == 0)
                return false;
            var first = _s[0];
            if (!((first >= 'a' && first <= 'z') || (first >= 'A' && first <= 'Z') || first == '_'))
                return false;
            int len = 1;
            for (var c = 1; c < _s.Length; c++)
            {
                var ch = _s[c];
                if (IsAlphaNumeric(ch) || ch == '_')
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
            if (!((first >= 'a' && first <= 'z') || (first >= 'A' && first <= 'Z') || first == '_'))
                return false;
            int len = 1;
            for (var c = 1; c < _s.Length; c++)
            {
                var ch = _s[c];
                if (IsAlphaNumeric(ch) || ch == '_')
                    len++;
                else
                    break;
            }

            res = _s.Slice(0, len);
            Advance(len);
            return true;
        }

        public string ReadToEol()
        {
            var initial = _s;
            var len = 0;
            while (true)
            {
                if (_s.Length > 0 && _s[0] != '\n' && _s[0] != '\r')
                {
                    len++;
                    Advance(1);
                }
                else
                    return initial.Slice(0, len).ToString();
            }
        }
        
        public string ReadTo(char c)
        {
            var initial = _s;
            var len = 0;
            var l = Line;
            var p = Position;
            while (true)
            {
                if (_s.Length == 0)
                    throw new ParseException("Expected " + c + " before EOF", l, p);
                
                if (_s[0] != c)
                {
                    len++;
                    Advance(1);
                }
                else
                    return initial.Slice(0, len).ToString();
            }
        }
        
        public string ReadToAny(ReadOnlySpan<char> chars)
        {
            var initial = _s;
            var len = 0;
            var l = Line;
            var p = Position;
            while (true)
            {
                if (_s.Length == 0)
                    throw new ParseException("Expected any of '" + chars.ToString() + "' before EOF", l, p);

                var foundTerminator = false;
                foreach (var term in chars)
                {
                    if (_s[0] == term)
                    {
                        foundTerminator = true;
                        break;
                    }
                }
                
                if (!foundTerminator)
                {
                    len++;
                    Advance(1);
                }
                else
                    return initial.Slice(0, len).ToString();
            }
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

        public override string ToString() => _s.ToString();

    }
}
