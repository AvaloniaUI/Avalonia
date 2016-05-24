// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Specialized;
using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;

namespace Avalonia.Controls.Presenters
{
    internal abstract class ItemVirtualizer
    {
        public ItemVirtualizer(ItemsPresenter owner)
        {
            Owner = owner;
        }

        public ItemsPresenter Owner { get; }
        public IVirtualizingPanel VirtualizingPanel => Owner.Panel as IVirtualizingPanel;
        public IEnumerable Items { get; private set; }
        public int FirstIndex { get; set; }
        public int LastIndex { get; set; } = -1;

        public abstract bool IsLogicalScrollEnabled { get; }
        public abstract Size Extent { get; }
        public abstract Vector Offset { get; set; }
        public abstract Size Viewport { get; }

        public static ItemVirtualizer Create(ItemsPresenter owner)
        {
            var virtualizingPanel = owner.Panel as IVirtualizingPanel;
            var scrollable = (ILogicalScrollable)owner;

            if (virtualizingPanel != null && scrollable.InvalidateScroll != null)
            {
                switch (owner.VirtualizationMode)
                {
                    case ItemVirtualizationMode.Simple:
                        return new ItemVirtualizerSimple(owner);
                }
            }

            return new ItemVirtualizerNone(owner);
        }

        public abstract void Arranging(Size finalSize);

        public virtual bool BringIntoView(IVisual target, Rect targetRect)
        {
            return false;
        }

        public virtual void ItemsChanged(IEnumerable items, NotifyCollectionChangedEventArgs e)
        {
            Items = items;
        }
    }
}
