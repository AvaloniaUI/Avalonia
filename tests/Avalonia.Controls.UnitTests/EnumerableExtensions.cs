using System;
using System.Collections.Generic;
using System.Linq;

namespace Avalonia.Controls.UnitTests
{
    internal static class EnumerableExtensions
    {
        public static IEnumerable<T> Do<T>(this IEnumerable<T> items, Action<T> action)
        {
            foreach (var i in items)
            {
                action(i);
                yield return i;
            }
        }

        public static IEnumerable<T[]> Permutations<T>(this IEnumerable<T> source)
        {
            var sourceArray = source.ToArray();
            var results = new List<T[]>();
            Permute(sourceArray, 0, sourceArray.Length - 1);
            return results;

            void Permute(T[] elements, int depth, int maxDepth)
            {
                if (depth == maxDepth)
                {
                    results.Add(elements.ToArray());
                    return;
                }

                for (var i = depth; i <= maxDepth; i++)
                {
                    (elements[depth], elements[i]) = (elements[i], elements[depth]);
                    Permute(elements, depth + 1, maxDepth);
                    (elements[depth], elements[i]) = (elements[i], elements[depth]);
                }
            }
        }
    }
}
