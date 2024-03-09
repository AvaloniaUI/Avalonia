using System;
using System.Collections.Generic;

namespace Avalonia.LinuxFramebuffer;

internal static class EnumerableExtesions
{
    public static IEnumerable<TSource> Where<TSource, TArg>(this IEnumerable<TSource> source, Func<TSource, TArg, bool> predicate, TArg arg)
    {
        foreach (var item in source)
        {
            if (predicate(item, arg))
            {
                yield return item;
            }
        }
    }
}
