using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace DataGridAsyncDemoMVVM
{
    using System.Linq;

    /// <summary>
    ///     Emulate a remote data repository (list of item + sort & filter values)
    /// </summary>
    public class RemoteOrDbDataSourceEmulation
    {
        public RemoteOrDbDataSourceEmulation(int itemsCount)
        {
            for (var i = 0; i < itemsCount; i++)
                _items.Add(new RemoteOrDbDataItem(itemsCount - i, "Name_" + i.ToString("00000000000"), "Str1_" + i, "Str1_" + i, i, i));
        }

        private readonly List<RemoteOrDbDataItem> _items = new();

        public IList<RemoteOrDbDataItem> Items => _items;
    }
}
