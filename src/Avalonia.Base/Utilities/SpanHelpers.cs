using System;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Avalonia.Utilities
{
#if !BUILDTASK
    public
#endif
    static class SpanHelpers
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryParseUInt(this ReadOnlySpan<char> span, NumberStyles style, IFormatProvider provider, out uint value)
        {
#if NETSTANDARD2_0
            return uint.TryParse(span.ToString(), style, provider, out value);
#else
            return uint.TryParse(span, style, provider, out value);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryParseInt(this ReadOnlySpan<char> span, out int value)
        {
#if NETSTANDARD2_0
            return int.TryParse(span.ToString(), out value);
#else
            return int.TryParse(span, out value);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryParseDouble(this ReadOnlySpan<char> span, NumberStyles style, IFormatProvider provider, out double value)
        {
#if NETSTANDARD2_0
            return double.TryParse(span.ToString(), style, provider, out value);
#else
            return double.TryParse(span, style, provider, out value);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryParseByte(this ReadOnlySpan<char> span, NumberStyles style, IFormatProvider provider, out byte value)
        {
#if NETSTANDARD2_0
            return byte.TryParse(span.ToString(), style, provider, out value);
#else
            return byte.TryParse(span, style, provider, out value);
#endif
        }
    }
}
