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
                            RecycleContainers(delta);
                            FirstIndex += delta;
                            NextIndex += delta;
                        }
                    }
                    else
                    {
                        // We're moving to a partially obscured item at the end of the list.
                        var firstIndex = ItemCount - panel.Children.Count;
                        RecycleContainers(firstIndex - FirstIndex);
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

            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                // We could recycle items here if this proves to be inefficient, but
                // Reset indicates a large change and should (?) be quite rare.
                VirtualizingPanel.Children.Clear();
                Owner.ItemContainerGenerator.Clear();
                FirstIndex = NextIndex = 0;
                CreateRemoveContainers();
            }

            ((ILogicalScrollable)Owner).InvalidateScroll();
        }

        private void CreateRemoveContainers()
        {
            var generator = Owner.ItemContainerGenerator;
            var panel = VirtualizingPanel;

            if (!panel.IsFull && Items != null)
            {
                var index = NextIndex;
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

                NextIndex = index;
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

        private void RecycleContainers(int delta)
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
