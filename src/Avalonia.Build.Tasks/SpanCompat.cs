#if !NETCOREAPP3_1_OR_GREATER
namespace System
{
    // This is a hack to enable our span code to work inside MSBuild task without referencing System.Memory
    struct ReadOnlySpan<T>
    {
        private string _s;
        private int _start;
        private int _length;
        public int Length => _length;

        public ReadOnlySpan(string s) : this(s, 0, s.Length)
        {
            
        }
        public ReadOnlySpan(string s, int start, int len)
        {
            _s = s;
            _length = len;
            _start = start;
            if (_start > s.Length)
                _length = 0;
            else if (_start + _length > s.Length)
                _length = s.Length - _start;
        }

        public char this[int c] => _s[_start + c];

        public bool IsEmpty => _length == 0;
        
        public ReadOnlySpan<char> Slice(int start, int len)
        {
            return new ReadOnlySpan<char>(_s, _start + start, len);
        }

        public static ReadOnlySpan<char> Empty => default;
        
        public ReadOnlySpan<char> Slice(int start)
        {
            return new ReadOnlySpan<char>(_s, _start + start, _length - start);
        }

        public bool SequenceEqual(ReadOnlySpan<char> other)
        {
            if (_length != other.Length)
                return false;
            for(var c=0; c<_length;c++)
                if (this[c] != other[c])
                    return false;
            return true;
        }
        
        public ReadOnlySpan<char> TrimStart()
        {
            int start = 0;
            for (; start < Length; start++)
            {
                if (!char.IsWhiteSpace(this[start]))
                {
                    break;
                }
            }
            return Slice(start);
        }

        public override string ToString() => _length == 0 ? string.Empty : _s.Substring(_start, _length);

        public static implicit operator ReadOnlySpan<T>(char[] arr) => new ReadOnlySpan<T>(new string(arr));
    }

    static class SpanCompatExtensions
    {
        public static ReadOnlySpan<char> AsSpan(this string s) => new ReadOnlySpan<char>(s);
    }

}
#endif
