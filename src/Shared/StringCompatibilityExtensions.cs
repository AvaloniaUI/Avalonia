using System.Runtime.CompilerServices;

namespace System;

#if !NET6_0_OR_GREATER
internal static class StringCompatibilityExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Contains(this string str, char search) =>
        str.Contains(search.ToString());
}
#endif
