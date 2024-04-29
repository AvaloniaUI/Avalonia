using System;
using System.ComponentModel;
using Avalonia.Animation.Animators;
using Avalonia.Utilities;

namespace Avalonia.Media
{
    public struct BoxShadows
    {
        private readonly BoxShadow _first;
        private readonly BoxShadow[]? _list;
        public int Count { get; }
        
        public BoxShadows(BoxShadow shadow)
        {
            _first = shadow;
            _list = null;
            Count = _first == default ? 0 : 1;
        }

        public BoxShadows(BoxShadow first, BoxShadow[] rest)
        {
            _first = first;
            _list = rest;
            Count = 1 + (rest?.Length ?? 0);
        }

        public BoxShadow this[int c]
        {
            get
            {
                if (c< 0 || c >= Count)
                    throw new IndexOutOfRangeException();
                if (c == 0)
                    return _first;
                return _list![c - 1];
            }
        }

        public override string ToString()
        {
            if (Count == 0)
            {
                return "none";
            }

            var sb = StringBuilderCache.Acquire();
            foreach (var boxShadow in this)
            {
                boxShadow.ToString(sb);
                sb.Append(',');
                sb.Append(' ');
            }
            sb.Remove(sb.Length - 2, 2);
            return StringBuilderCache.GetStringAndRelease(sb);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
#pragma warning disable CA1815 // Override equals and operator equals on value types
        public struct BoxShadowsEnumerator
#pragma warning restore CA1815 // Override equals and operator equals on value types
        {
            private int _index;
            private readonly BoxShadows _shadows;

            public BoxShadowsEnumerator(BoxShadows shadows)
            {
                _shadows = shadows;
                _index = -1;
            }

            public BoxShadow Current => _shadows[_index];

            public bool MoveNext()
            {
                _index++;
                return _index < _shadows.Count;
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public BoxShadowsEnumerator GetEnumerator() => new BoxShadowsEnumerator(this);

        private static readonly char[] s_Separators = new[] { ',' };
        public static BoxShadows Parse(string s)
        {
            var sp = s.Split(s_Separators, StringSplitOptions.RemoveEmptyEntries);
            if (sp.Length == 0
                || (sp.Length == 1 &&
                    (string.IsNullOrWhiteSpace(sp[0])
                     || sp[0] == "none")))
                return new BoxShadows();

            var first = BoxShadow.Parse(sp[0]);
            if (sp.Length == 1)
                return new BoxShadows(first);

            var rest = new BoxShadow[sp.Length - 1];
            for (var c = 0; c < rest.Length; c++)
                rest[c] = BoxShadow.Parse(sp[c + 1]);
            return new BoxShadows(first, rest);
        }

        public Rect TransformBounds(in Rect rect)
        {
            var final = rect;
            foreach (var shadow in this)
                final = final.Union(shadow.TransformBounds(rect));
            return final;
        }
        
        public bool HasInsetShadows
        {
            get
            {
                foreach(var boxShadow in this)
                    if (boxShadow != default && boxShadow.IsInset)
                        return true;
                return false;
            }
        }
        
        public bool Equals(BoxShadows other)
        {
            if (other.Count != Count)
                return false;
            for(var c=0; c<Count ; c++)
                if (!this[c].Equals(other[c]))
                    return false;
            return true;
        }

        public override bool Equals(object? obj)
        {
            return obj is BoxShadows other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = 0;
                foreach (var s in this)
                    hashCode = (hashCode * 397) ^ s.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(BoxShadows left, BoxShadows right) => 
            left.Equals(right);

        public static bool operator !=(BoxShadows left, BoxShadows right) =>
            !(left == right);
    }
}
