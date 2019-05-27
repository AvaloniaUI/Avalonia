// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Avalonia.Controls
{
    /// <summary>
    /// Helper class for representing a key for a span in hashtable.
    /// </summary>
    internal class GridSpanKey
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="start">Starting index of the span.</param>
        /// <param name="count">Span count.</param>
        /// <param name="u"><c>true</c> for columns; <c>false</c> for rows.</param>
        internal GridSpanKey(int start, int count, bool u)
        {
            _start = start;
            _count = count;
            _u = u;
        }

        /// <summary>
        /// <see cref="object.GetHashCode"/>
        /// </summary>
        public override int GetHashCode()
        {
            int hash = (_start ^ (_count << 2));

            if (_u) hash &= 0x7ffffff;
            else hash |= 0x8000000;

            return (hash);
        }

        /// <summary>
        /// <see cref="object.Equals(object)"/>
        /// </summary>
        public override bool Equals(object obj)
        {
            GridSpanKey sk = obj as GridSpanKey;
            return (sk != null
                    && sk._start == _start
                    && sk._count == _count
                    && sk._u == _u);
        }

        /// <summary>
        /// Returns start index of the span.
        /// </summary>
        internal int Start { get { return (_start); } }

        /// <summary>
        /// Returns span count.
        /// </summary>
        internal int Count { get { return (_count); } }

        /// <summary>
        /// Returns <c>true</c> if this is a column span.
        /// <c>false</c> if this is a row span.
        /// </summary>
        internal bool U { get { return (_u); } }

        private int _start;
        private int _count;
        private bool _u;
    }
}