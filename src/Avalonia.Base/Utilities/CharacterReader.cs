using System;

namespace Avalonia.Utilities
{
#if !BUILDTASK
    public
#endif
    ref struct CharacterReader
    {
        private ReadOnlySpan<char> _s;

        public CharacterReader(ReadOnlySpan<char> s)
            :this()
        {
            _s = s;
        }

        public bool End => _s.IsEmpty;
        public char Peek => _s[0];
        public int Position { get; private set; }
        public char Take()
        {
            Position++;
            char taken = _s[0];
            _s = _s.Slice(1);
            return taken;
        }

        public void SkipWhitespace()
        {
            var trimmed = _s.TrimStart();
            Position += _s.Length - trimmed.Length;
            _s = trimmed;
        }

        public bool TakeIf(char c)
        {
            if (Peek == c)
            {
                Take();
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool TakeIf(Func<char, bool> condition)
        {
            if (condition(Peek))
            {
                Take();
                return true;
            }
            return false;
        }

        public ReadOnlySpan<char> TakeUntil(char c)
        {
            int len;
            for (len = 0; len < _s.Length && _s[len] != c; len++)
            {
            }
            var span = _s.Slice(0, len);
            _s = _s.Slice(len);
            Position += len;
            return span;
        }

        public ReadOnlySpan<char> TakeWhile(Func<char, bool> condition)
        {
            int len;
            for (len = 0; len < _s.Length && condition(_s[len]); len++)
            {
            }
            var span = _s.Slice(0, len);
            _s = _s.Slice(len);
            Position += len;
            return span;
        }

        public ReadOnlySpan<char> TryPeek(int count)
        {
            if (_s.Length < count)
                return ReadOnlySpan<char>.Empty;
            return _s.Slice(0, count);
        }

        public ReadOnlySpan<char> PeekWhitespace()
        {
            var trimmed = _s.TrimStart();
            return _s.Slice(0, _s.Length - trimmed.Length);
        }

        public void Skip(int count)
        {
            if (_s.Length < count)
                throw new IndexOutOfRangeException();
            _s = _s.Slice(count);
        }
    }
}
