// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections;
using System.Globalization;
using System.Linq;

namespace Perspex.Controls.Utils
{
    internal static class IEnumerableUtils
    {
        public static bool Contains(this IEnumerable items, object item)
        {
            return items.IndexOf(item) != -1;
        }

        public static int Count(this IEnumerable items)
        {
            Contract.Requires<ArgumentNullException>(items != null);

            var collection = items as ICollection;

            if (collection != null)
            {
                return collection.Count;
            }
            else
            {
                return Enumerable.Count(items.Cast<object>());
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
                    if (object.ReferenceEquals(i, item))
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
                return items.Cast<object>().ElementAt(index);
            }
        }
    }
}
