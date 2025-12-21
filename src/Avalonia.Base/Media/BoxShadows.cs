using System;
using System.ComponentModel;
using Avalonia.Utilities;

namespace Avalonia.Media
{
    /// <summary>
    /// Represents a collection of <see cref="BoxShadow"/>s.
    /// </summary>
    public struct BoxShadows
    {
        private const char Separator = ',';
        private const char OpeningParenthesis = '(';
        private const char ClosingParenthesis = ')';

        private readonly BoxShadow _first;
        private readonly BoxShadow[]? _list;

        /// <summary>
        /// Gets the number of <see cref="BoxShadow"/>s in the collection.
        /// </summary>
        public int Count { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BoxShadows"/> struct.
        /// </summary>
        /// <param name="shadow">The first <see cref="BoxShadow"/> to add to the collection.</param>
        public BoxShadows(BoxShadow shadow)
        {
            _first = shadow;
            _list = null;
            Count = _first == default ? 0 : 1;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BoxShadows"/> struct.
        /// </summary>
        /// <param name="first">The first <see cref="BoxShadow"/> to add to the collection.</param>
        /// <param name="rest">All remaining <see cref="BoxShadow"/>s to add to the collection.</param>
        public BoxShadows(BoxShadow first, BoxShadow[] rest)
        {
            _first = first;
            _list = rest;
            Count = 1 + (rest?.Length ?? 0);
        }

        /// <summary>
        /// Gets the <see cref="BoxShadow"/> at the specified index.
        /// </summary>
        /// <param name="index">The index of the <see cref="BoxShadow"/> to return.</param>
        /// <returns>The <see cref="BoxShadow"/> at the specified index.</returns>
        /// <exception cref="IndexOutOfRangeException">
        /// Thrown when index less than 0 or index greater than or equal to <see cref="Count"/>.
        /// </exception>
        public BoxShadow this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                {
                    throw new IndexOutOfRangeException();
                }

                if (index == 0)
                {
                    return _first;
                }

                return _list![index - 1];
            }
        }

        /// <inheritdoc/>
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

        /// <summary>
        /// Parses a <see cref="BoxShadows"/> string representing one or more <see cref="BoxShadow"/>s.
        /// </summary>
        /// <param name="s">The input string to parse.</param>
        /// <returns>A new <see cref="BoxShadows"/> collection.</returns>
        public static BoxShadows Parse(string s)
        {
            var sp = StringSplitter.SplitRespectingBrackets(
                s, Separator, OpeningParenthesis, ClosingParenthesis,
                StringSplitOptions.RemoveEmptyEntries);
            if (sp.Length == 0
                || (sp.Length == 1 &&
                    (string.IsNullOrWhiteSpace(sp[0])
                     || sp[0] == "none")))
            {
                return new BoxShadows();
            }

            var first = BoxShadow.Parse(sp[0]);
            if (sp.Length == 1)
            {
                return new BoxShadows(first);
            }

            var rest = new BoxShadow[sp.Length - 1];
            for (var c = 0; c < rest.Length; c++)
            {
                rest[c] = BoxShadow.Parse(sp[c + 1]);
            }

            return new BoxShadows(first, rest);
        }

        /// <summary>
        /// Transforms the specified bounding rectangle to account for all shadow's offset, spread, and blur.
        /// </summary>
        /// <param name="rect">The original bounding <see cref="Rect"/> to transform.</param>
        /// <returns>
        /// A new <see cref="Rect"/> that includes all shadow's offset, spread, and blur in the collection.
        /// </returns>
        public Rect TransformBounds(in Rect rect)
        {
            var final = rect;
            foreach (var shadow in this)
            {
                final = final.Union(shadow.TransformBounds(rect));
            }

            return final;
        }

        /// <summary>
        /// Gets a value indicating whether any <see cref="BoxShadow"/> in the collection has
        /// <see cref="BoxShadow.IsInset"/> set to <c>true</c>.
        /// </summary>
        public bool HasInsetShadows
        {
            get
            {
                foreach(var boxShadow in this)
                {
                    if (boxShadow != default && boxShadow.IsInset)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        /// <c>true</c> if the current object is equal to the other parameter; otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(BoxShadows other)
        {
            if (other.Count != Count)
            {
                return false;
            }

            for (var c = 0; c < Count; c++)
            {
                if (!this[c].Equals(other[c]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is BoxShadows other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = 0;
                foreach (var s in this)
                {
                    hashCode = (hashCode * 397) ^ s.GetHashCode();
                }

                return hashCode;
            }
        }

        /// <summary>
        /// Determines whether two <see cref="BoxShadows"/> collections are equal.
        /// </summary>
        /// <param name="left">The first <see cref="BoxShadows"/> collection to compare.</param>
        /// <param name="right">The second <see cref="BoxShadows"/> collection to compare.</param>
        /// <returns>
        /// <c>true</c> if the two <see cref="BoxShadows"/> collections are equal; otherwise, <c>false</c>.
        /// </returns>
        public static bool operator ==(BoxShadows left, BoxShadows right) =>
            left.Equals(right);

        /// <summary>
        /// Determines whether two <see cref="BoxShadows"/> collections are not equal.
        /// </summary>
        /// <param name="left">The first <see cref="BoxShadows"/> collection to compare.</param>
        /// <param name="right">The second <see cref="BoxShadows"/> collection to compare.</param>
        /// <returns>
        /// <c>true</c> if the two <see cref="BoxShadows"/> collections are not equal; otherwise, <c>false</c>.
        /// </returns>
        public static bool operator !=(BoxShadows left, BoxShadows right) =>
            !(left == right);
    }
}
