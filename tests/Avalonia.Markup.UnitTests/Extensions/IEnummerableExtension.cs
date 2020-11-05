using System;
using System.Collections;

namespace Avalonia.Markup.UnitTests
{
   internal static class IEnumerableExtensions
    {
        public static object ElementAt(this IEnumerable source, int index)
        {
            var i = -1;
            var enumerator = source.GetEnumerator();

            while (enumerator.MoveNext() && ++i < index);
            if (i == index)
            {
                return enumerator.Current;
            }
            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }
}
