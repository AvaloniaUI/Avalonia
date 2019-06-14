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
            if (chars.Slice(ws.Length).Equals(keyword.AsSpan(), StringComparison.Ordinal))
                return chars.Length;
            return -1;
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
