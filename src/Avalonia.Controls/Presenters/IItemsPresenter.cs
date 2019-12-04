// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections;
using System.Collections.Specialized;

namespace Avalonia.Controls.Presenters
{
    public interface IItemsPresenter : IPresenter
    {
        IEnumerable Items { get; set; }

        IPanel Panel { get; }

        void ItemsChanged(NotifyCollectionChangedEventArgs e);

        void ScrollIntoView(object item);
    }
}
