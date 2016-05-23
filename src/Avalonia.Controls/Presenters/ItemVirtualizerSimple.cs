// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Linq;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Utils;

namespace Avalonia.Controls.Presenters
{
    internal class ItemVirtualizerSimple : ItemVirtualizer
    {
        public ItemVirtualizerSimple(ItemsPresenter owner)
            : base(owner)
        {
        }

        public override bool IsLogicalScrollEnabled => true;

        public override Size Extent
        {
            get
            {
                if (VirtualizingPanel.ScrollDirection == Orientation.Vertical)
                {
                    return new Size(0, Items.Count());
                }
                else
                {
                    return new Size(Items.Count(), 0);
                }
            }
        }

        public override Vector Offset
        {
            get
            {
                if (VirtualizingPanel.ScrollDirection == Orientation.Vertical)
                {
                    return new Vector(0, FirstIndex);
                }
                else
                {
                    return new Vector(FirstIndex, 0);
                }
            }

            set
            {
                var scroll = (VirtualizingPanel.ScrollDirection == Orientation.Vertical) ?
                    value.Y : value.X;
                var delta = (int)(scroll - FirstIndex);

                if (delta != 0)
                {
                    RecycleContainers(delta);
                    FirstIndex += delta;
                    LastIndex += delta;
                }
            }
        }

        public override Size Viewport
        {
            get
            {
                var panel = VirtualizingPanel;

                if (panel.ScrollDirection == Orientation.Vertical)
                {
                    return new Size(0, panel.Children.Count);
                }
                else
                {
                    return new Size(panel.Children.Count, 0);
                }
            }
        }

        public override void Arranging(Size finalSize)
        {
            CreateRemoveContainers();
            ((IScrollable)Owner).InvalidateScroll();
        }

        public override void ItemsChanged(IEnumerable items, NotifyCollectionChangedEventArgs e)
        {
            base.ItemsChanged(items, e);
            ((IScrollable)Owner).InvalidateScroll();
        }

        private void CreateRemoveContainers()
        {
            var generator = Owner.ItemContainerGenerator;
            var panel = VirtualizingPanel;

            if (!panel.IsFull)
            {
                var index = LastIndex + 1;
                var items = Items.Cast<object>().Skip(index);
                var memberSelector = Owner.MemberSelector;

                foreach (var item in items)
                {
                    var materialized = generator.Materialize(index++, item, memberSelector);
                    panel.Children.Add(materialized.ContainerControl);

                    if (panel.IsFull)
                    {
                        break;
                    }
                }

                LastIndex = index - 1;
            }

            if (panel.OverflowCount > 0)
            {
                var count = panel.OverflowCount;
                var index = panel.Children.Count - count;

                panel.Children.RemoveRange(index, count);
                generator.Dematerialize(index, count);

                LastIndex -= count;
            }
        }

        private void RecycleContainers(int delta)
        {
            var panel = VirtualizingPanel;
            var generator = Owner.ItemContainerGenerator;
            var selector = Owner.MemberSelector;
            var sign = delta < 0 ? -1 : 1;
            var move = delta < panel.Children.Count;
            var first = delta < 0 && move ? panel.Children.Count + delta : 0;
            var count = Math.Min(Math.Abs(delta), panel.Children.Count);
            var containers = panel.Children.GetRange(first, count).ToList();

            for (var i = 0; i < containers.Count; ++i)
            {
                var oldItemIndex = FirstIndex + first + i;
                var newItemIndex = oldItemIndex + delta + ((panel.Children.Count - count) * sign);
                var item = Items.ElementAt(newItemIndex);

                if (!generator.TryRecycle(oldItemIndex, newItemIndex, item, selector))
                {
                    throw new NotImplementedException();
                }
            }

            if (move)
            {
                panel.Children.RemoveRange(first, count);

                if (delta > 0)
                {
                    panel.Children.AddRange(containers);
                }
                else
                {
                    panel.Children.InsertRange(0, containers);
                }
            }
        }
    }
}
