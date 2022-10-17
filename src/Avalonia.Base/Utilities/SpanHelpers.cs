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
    }
}
