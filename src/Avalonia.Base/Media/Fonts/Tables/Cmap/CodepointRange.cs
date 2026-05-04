using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Avalonia.Media.Fonts.Tables.Cmap
{
    /// <summary>
    /// Represents a range of Unicode code points, defined by inclusive start and end values.
    /// </summary>
    public readonly struct CodepointRange
    {
        /// <summary>
        /// Gets the start of the range.
        /// </summary>
        public readonly int Start;

        /// <summary>
        /// Gets the end of the range.
        /// </summary>
        public readonly int End;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CodepointRange(int start, int end)
        {
            Start = start;
            End = end;
        }

        public override bool Equals(object? obj)
        {
            return obj is CodepointRange range &&
                   Start == range.Start &&
                   End == range.End;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Start, End);
        }

        public static bool operator ==(CodepointRange left, CodepointRange right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CodepointRange left, CodepointRange right)
        {
            return !(left == right);
        }
    }
}
