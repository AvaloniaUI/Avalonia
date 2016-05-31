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

        public override double ExtentValue => ItemCount;

        public override double OffsetValue
        {
            get
            {
                var offset = VirtualizingPanel.PixelOffset > 0 ? 1 : 0;
                return FirstIndex + offset;
            }

            set
            {
                var panel = VirtualizingPanel;
                var offset = VirtualizingPanel.PixelOffset > 0 ? 1 : 0;
                var delta = (int)(value - (FirstIndex + offset));

                if (delta != 0)
                {
                    if ((NextIndex - 1) + delta < ItemCount)
                    {
                        if (panel.PixelOffset > 0)
                        {
                            panel.PixelOffset = 0;
                            delta += 1;                           
                        }

                        if (delta != 0)
                        {
                            RecycleMoveContainers(delta);
                            FirstIndex += delta;
                            NextIndex += delta;
                        }
                    }
                    else
                    {
                        // We're moving to a partially obscured item at the end of the list.
                        var firstIndex = ItemCount - panel.Children.Count;
                        RecycleMoveContainers(firstIndex - FirstIndex);
                        NextIndex = ItemCount;
                        FirstIndex = NextIndex - panel.Children.Count;
                        panel.PixelOffset = VirtualizingPanel.PixelOverflow;
                    }
                }
            }
        }

        public override double ViewportValue
        {
            get
            {
                // If we can't fit the last item in the panel fully, subtract 1 from the viewport.
                var overflow = VirtualizingPanel.PixelOverflow > 0 ? 1 : 0;
                return VirtualizingPanel.Children.Count - overflow;
            }
        }

        public override void Arranging(Size finalSize)
        {
            CreateRemoveContainers();
            ((ILogicalScrollable)Owner).InvalidateScroll();
        }

        public override void ItemsChanged(IEnumerable items, NotifyCollectionChangedEventArgs e)
        {
            base.ItemsChanged(items, e);

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (e.NewStartingIndex >= FirstIndex &&
                        e.NewStartingIndex + e.NewItems.Count < NextIndex)
                    {
                        CreateRemoveContainers();
                        RecycleContainers();
                    }

                    break;

                case NotifyCollectionChangedAction.Reset:
                    // We could recycle items here if this proves to be inefficient, but
                    // Reset indicates a large change and should (?) be quite rare.
                    VirtualizingPanel.Children.Clear();
                    Owner.ItemContainerGenerator.Clear();
                    FirstIndex = NextIndex = 0;
                    CreateRemoveContainers();
                    break;
            }

            ((ILogicalScrollable)Owner).InvalidateScroll();
        }

        private void CreateRemoveContainers()
        {
            var generator = Owner.ItemContainerGenerator;
            var panel = VirtualizingPanel;

            if (!panel.IsFull && Items != null)
            {
                var memberSelector = Owner.MemberSelector;
                var index = NextIndex;
                var step = 1;

                while (!panel.IsFull)
                {
                    if (index == ItemCount)
                    {
                        if (FirstIndex == 0)
                        {
                            break;
                        }
                        else
                        {
                            index = FirstIndex - 1;
                            step = -1;
                        }
                    }

                    var materialized = generator.Materialize(index, Items.ElementAt(index), memberSelector);

                    if (step == 1)
                    {
                        panel.Children.Add(materialized.ContainerControl);
                    }
                    else
                    {
                        panel.Children.Insert(0, materialized.ContainerControl);
                    }

                    index += step;
                }

                if (step == 1)
                {
                    NextIndex = index;
                }
                else
                {
                    NextIndex = ItemCount;
                    FirstIndex = index + 1;
                }
            }

            if (panel.OverflowCount > 0)
            {
                var count = panel.OverflowCount;
                var index = panel.Children.Count - count;

                panel.Children.RemoveRange(index, count);
                generator.Dematerialize(FirstIndex + index, count);

                NextIndex -= count;
            }
        }

        private void RecycleContainers()
        {
            var panel = VirtualizingPanel;
            var generator = Owner.ItemContainerGenerator;
            var selector = Owner.MemberSelector;
            var containers = generator.Containers.ToList();
            var itemIndex = FirstIndex;

            foreach (var container in containers)
            {
                var item = Items.ElementAt(itemIndex);

                if (!object.Equals(container.Item, item))
                {
                    if (!generator.TryRecycle(itemIndex, itemIndex, item, selector))
                    {
                        throw new NotImplementedException();
                    }
                }

                ++itemIndex;
            }
        }

        private void RecycleMoveContainers(int delta)
        {
            var panel = VirtualizingPanel;
            var generator = Owner.ItemContainerGenerator;
            var selector = Owner.MemberSelector;
            var sign = delta < 0 ? -1 : 1;
            var count = Math.Min(Math.Abs(delta), panel.Children.Count);
            var move = count < panel.Children.Count;
            var first = delta < 0 && move ? panel.Children.Count + delta : 0;
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
