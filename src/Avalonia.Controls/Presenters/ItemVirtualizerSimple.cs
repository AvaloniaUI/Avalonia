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
    /// <summary>
    /// Handles virtualization in an <see cref="ItemsPresenter"/> for
    /// <see cref="ItemVirtualizationMode.Simple"/>.
    /// </summary>
    internal class ItemVirtualizerSimple : ItemVirtualizer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ItemVirtualizerSimple"/> class.
        /// </summary>
        /// <param name="owner"></param>
        public ItemVirtualizerSimple(ItemsPresenter owner)
            : base(owner)
        {
        }

        /// <inheritdoc/>
        public override bool IsLogicalScrollEnabled => true;

        /// <inheritdoc/>
        public override double ExtentValue => ItemCount;

        /// <inheritdoc/>
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
                            RecycleContainersForMove(delta);
                        }
                    }
                    else
                    {
                        // We're moving to a partially obscured item at the end of the list.
                        var firstIndex = ItemCount - panel.Children.Count;
                        RecycleContainersForMove(firstIndex - FirstIndex);
                        panel.PixelOffset = VirtualizingPanel.PixelOverflow;
                    }
                }
            }
        }

        /// <inheritdoc/>
        public override double ViewportValue
        {
            get
            {
                // If we can't fit the last item in the panel fully, subtract 1 from the viewport.
                var overflow = VirtualizingPanel.PixelOverflow > 0 ? 1 : 0;
                return VirtualizingPanel.Children.Count - overflow;
            }
        }

        /// <inheritdoc/>
        public override void Arranging(Size finalSize)
        {
            CreateAndRemoveContainers();
            ((ILogicalScrollable)Owner).InvalidateScroll();
        }

        /// <inheritdoc/>
        public override void ItemsChanged(IEnumerable items, NotifyCollectionChangedEventArgs e)
        {
            base.ItemsChanged(items, e);

            if (items != null)
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        if (e.NewStartingIndex >= FirstIndex &&
                            e.NewStartingIndex + e.NewItems.Count <= NextIndex)
                        {
                            CreateAndRemoveContainers();
                            RecycleContainers();
                        }

                        break;

                    case NotifyCollectionChangedAction.Remove:
                        if (e.OldStartingIndex >= FirstIndex &&
                            e.OldStartingIndex + e.OldItems.Count <= NextIndex)
                        {
                            RecycleContainersOnRemove();
                        }

                        break;

                    case NotifyCollectionChangedAction.Move:
                    case NotifyCollectionChangedAction.Replace:
                        RecycleContainers();
                        break;

                    case NotifyCollectionChangedAction.Reset:
                        RecycleContainersOnRemove();
                        break;
                }
            }
            else
            {
                Owner.ItemContainerGenerator.Clear();
                VirtualizingPanel.Children.Clear();
            }

            ((ILogicalScrollable)Owner).InvalidateScroll();
        }

        /// <summary>
        /// Creates and removes containers such that we have at most enough containers to fill
        /// the panel.
        /// </summary>
        private void CreateAndRemoveContainers()
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
                    if (index >= ItemCount)
                    {
                        // We can fit more containers in the panel, but we're at the end of the
                        // items. If we're scrolled to the top (FirstIndex == 0), then there are
                        // no more items to create. Otherwise, go backwards adding containers to
                        // the beginning of the panel.
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
                RemoveContainers(panel.OverflowCount);
            }
        }

        /// <summary>
        /// Updates the containers in the panel to make sure they are displaying the correct item
        /// based on <see cref="ItemVirtualizer.FirstIndex"/>.
        /// </summary>
        /// <remarks>
        /// This method requires that <see cref="ItemVirtualizer.FirstIndex"/> + the number of
        /// materialized containers is not more than <see cref="ItemVirtualizer.ItemCount"/>.
        /// </remarks>
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

        /// <summary>
        /// Recycles containers when a move occurs.
        /// </summary>
        /// <param name="delta">The delta of the move.</param>
        /// <remarks>
        /// If the move is less than a page, then this method moves the containers for the items
        /// that are still visible to the correct place, and recyles and moves the others. For
        /// example: if there are 20 items and 10 containers visible and the user scrolls 5
        /// items down, then the bottom 5 containers will be moved to the top and the top 5 will
        /// be moved to the bottom and recycled to display the newly visible item. Updates 
        /// <see cref="ItemVirtualizer.FirstIndex"/> and <see cref="ItemVirtualizer.NextIndex"/>
        /// with their new values.
        /// </remarks>
        private void RecycleContainersForMove(int delta)
        {
            var panel = VirtualizingPanel;
            var generator = Owner.ItemContainerGenerator;
            var selector = Owner.MemberSelector;
            var sign = delta < 0 ? -1 : 1;
            var count = Math.Min(Math.Abs(delta), panel.Children.Count);
            var move = count < panel.Children.Count;
            var first = delta < 0 && move ? panel.Children.Count + delta : 0;
            var containers = panel.Children.GetRange(first, count).ToList();

            for (var i = 0; i < count; ++i)
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
                if (delta > 0)
                {
                    panel.Children.MoveRange(first, count, panel.Children.Count);
                }
                else
                {
                    panel.Children.MoveRange(first, count, 0);
                }
            }

            FirstIndex += delta;
            NextIndex += delta;
        }

        /// <summary>
        /// Recycles containers due to items being removed.
        /// </summary>
        private void RecycleContainersOnRemove()
        {
            var panel = VirtualizingPanel;

            if (NextIndex <= ItemCount)
            {
                // Items have been removed but FirstIndex..NextIndex is still a valid range in the
                // items, so just recycle the containers to adapt to the new state.
                RecycleContainers();
            }
            else
            {
                // Items have been removed and now the range FirstIndex..NextIndex goes out of 
                // the item bounds. Remove any excess containers, try to scroll up and then recycle
                // the containers to make sure they point to the correct item.
                var newFirstIndex = Math.Max(0, FirstIndex - (NextIndex - ItemCount));
                var delta = newFirstIndex - FirstIndex;
                var newNextIndex = NextIndex + delta;

                if (newNextIndex > ItemCount)
                {
                    RemoveContainers(newNextIndex - ItemCount);
                }

                if (delta != 0)
                {
                    RecycleContainersForMove(delta);
                }

                RecycleContainers();
            }
        }

        /// <summary>
        /// Removes the specified number of containers from the end of the panel and updates
        /// <see cref="ItemVirtualizer.NextIndex"/>.
        /// </summary>
        /// <param name="count">The number of containers to remove.</param>
        private void RemoveContainers(int count)
        {
            var index = VirtualizingPanel.Children.Count - count;

            VirtualizingPanel.Children.RemoveRange(index, count);
            Owner.ItemContainerGenerator.Dematerialize(FirstIndex + index, count);
            NextIndex -= count;
        }
    }
}
