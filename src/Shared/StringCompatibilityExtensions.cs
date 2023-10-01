using System.Runtime.CompilerServices;

namespace System;

#if !NET6_0_OR_GREATER
internal static class StringCompatibilityExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Contains(this string str, char search) =>
        str.Contains(search.ToString());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool EndsWith(this string str, char search) =>
        str?.Length switch
        {
            int len when len > 0 => str[len - 1] == search,
            _ => false
        };
}
#endif
