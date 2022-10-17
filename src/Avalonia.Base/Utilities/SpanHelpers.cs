using System;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Avalonia.Utilities
{
    public static class SpanHelpers
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryParseFromHexToUInt(this ReadOnlySpan<char> span, out uint value)
        {
#if NETSTANDARD2_0
            return uint.TryParse(span.ToString(), NumberStyles.HexNumber, CultureInfo.InvariantCulture,
                    out value);
#else
            return uint.TryParse(span, NumberStyles.HexNumber, CultureInfo.InvariantCulture,
                    out value);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryParseToInt(this ReadOnlySpan<char> span, out int value)
        {
#if NETSTANDARD2_0
            return int.TryParse(span.ToString(), out value);
#else
            return int.TryParse(span, out value);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryParseNumberToDouble(this ReadOnlySpan<char> span, out double value)
        {
#if NETSTANDARD2_0
            return double.TryParse(span.ToString(), NumberStyles.Number, CultureInfo.InvariantCulture,
                    out value);
#else
            return double.TryParse(span, NumberStyles.Number, CultureInfo.InvariantCulture,
                    out value);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryParseNumberToByte(this ReadOnlySpan<char> span, out byte value)
        {
#if NETSTANDARD2_0
            return byte.TryParse(span.ToString(), NumberStyles.Number, CultureInfo.InvariantCulture,
                    out value);
#else
            return byte.TryParse(span, NumberStyles.Number, CultureInfo.InvariantCulture,
                    out value);
#endif
        }
    }
}
