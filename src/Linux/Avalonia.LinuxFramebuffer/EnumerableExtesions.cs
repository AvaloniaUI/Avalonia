using System;
using System.Collections.Generic;

namespace Avalonia.LinuxFramebuffer;

internal static class EnumerableExtesions
{
    public static IEnumerable<TSource> Where<TSource, TArg>(this IEnumerable<TSource> source, Func<TSource, TArg, bool> predicate, TArg arg)
    {
        var enumerator = source.GetEnumerator();
        while (enumerator.MoveNext())
        {
            var current = enumerator.Current;
            if (predicate(current, arg))
            {
                yield return current;
            }
        }
    }
}
