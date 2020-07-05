using System;

namespace Avalonia.Utilities
{
#if !BUILDTASK
    public
#endif
    static class KeywordParser
    {
        public static bool CheckKeyword(this ref CharacterReader r, string keyword)
        {
            return (CheckKeywordInternal(ref r, keyword) >= 0);
        }
        
        static int CheckKeywordInternal(this ref CharacterReader r, string keyword)
        {
            var ws = r.PeekWhitespace();

            var chars = r.TryPeek(ws.Length + keyword.Length);
            if (chars.IsEmpty)
                return -1;
            if (SpanEquals(chars.Slice(ws.Length), keyword.AsSpan()))
                return chars.Length;
            return -1;
        }

        static bool SpanEquals(ReadOnlySpan<char> left, ReadOnlySpan<char> right)
        {
            if (left.Length != right.Length)
                return false;
            for(var c=0; c<left.Length;c++)
                if (left[c] != right[c])
                    return false;
            return true;
        }

        public static bool TakeIfKeyword(this ref CharacterReader r, string keyword)
        {
            var l = CheckKeywordInternal(ref r, keyword);
            if (l < 0)
                return false;
            r.Skip(l);
            return true;
        }
    }
}
