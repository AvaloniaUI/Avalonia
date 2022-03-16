using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Avalonia.Controls.Utils
{
    internal static class IEnumerableUtils
    {
        public static bool Contains(this IEnumerable items, object item)
        {
            return items.IndexOf(item) != -1;
        }

        public static bool TryGetCountFast(this IEnumerable? items, out int count)
        {
            if (items != null)
            {
                if (items is ICollection collection)
                {
                    count = collection.Count;
                    return true;
                }
                else if (items is IReadOnlyCollection<object> readOnly)
                {
                    count = readOnly.Count;
                    return true;
                }
            }

            count = 0;
            return false;
        }

        public static int Count(this IEnumerable? items)
        {
            if (TryGetCountFast(items, out var count))
            {
                return count;
            }
            else if (items != null)
            {
                return Enumerable.Count(items.Cast<object>());
            }
            else
            {
                return 0;
            }
        }

        public static int IndexOf(this IEnumerable items, object item)
        {
            _ = items ?? throw new ArgumentNullException(nameof(items));

            var list = items as IList;

            if (list != null)
            {
                return list.IndexOf(item);
            }
            else
            {
                int index = 0;

                foreach (var i in items)
                {
                    if (ReferenceEquals(i, item))
                    {
                        return index;
                    }

                    ++index;
                }

                return -1;
            }
        }

        public static object? ElementAt(this IEnumerable items, int index)
        {
            _ = items ?? throw new ArgumentNullException(nameof(items));

            var list = items as IList;

            if (list != null)
            {
                return list[index];
            }
            else
            {
                return Enumerable.ElementAt(items.Cast<object>(), index);
            }
        }
    }
}
