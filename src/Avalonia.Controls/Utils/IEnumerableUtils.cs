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

        public static int Count(this IEnumerable items)
        {
            if (items != null)
            {
                if (items is ICollection collection)
                {
                    return collection.Count;
                }
                else if (items is IReadOnlyCollection<object> readOnly)
                {
                    return readOnly.Count;
                }
                else
                {
                    return Enumerable.Count(items.Cast<object>());
                }
            }
            else
            {
                return 0;
            }
        }

        public static int IndexOf(this IEnumerable items, object item)
        {
            Contract.Requires<ArgumentNullException>(items != null);

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

        public static object ElementAt(this IEnumerable items, int index)
        {
            Contract.Requires<ArgumentNullException>(items != null);

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
