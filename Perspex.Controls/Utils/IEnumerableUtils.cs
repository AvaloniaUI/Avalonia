// -----------------------------------------------------------------------
// <copyright file="IEnumerableUtils.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls.Utils
{
    using System;
    using System.Collections;
    using System.Globalization;
    using System.Linq;

    internal static class IEnumerableUtils
    {
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
                return items.Cast<object>().Count();
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
