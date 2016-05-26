// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Specialized;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Utils;
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
        public int ItemCount { get; private set; }
        public int FirstIndex { get; set; }
        public int NextIndex { get; set; }
        public bool Vertical => VirtualizingPanel.ScrollDirection == Orientation.Vertical;

        public abstract bool IsLogicalScrollEnabled { get; }
        public abstract double ExtentValue { get; }
        public abstract double OffsetValue { get; set; }
        public abstract double ViewportValue { get; }

        public Size Extent => Vertical ? new Size(0, ExtentValue) : new Size(ExtentValue, 0);
        public Size Viewport => Vertical ? new Size(0, ViewportValue) : new Size(ViewportValue, 0);

        public Vector Offset
        {
            get
            {
                return Vertical ? new Vector(0, OffsetValue) : new Vector(OffsetValue, 0);
            }

            set
            {
                OffsetValue = Vertical ? value.Y : value.X;
            }
        }
        
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
            ItemCount = items.Count();
        }
    }
}
