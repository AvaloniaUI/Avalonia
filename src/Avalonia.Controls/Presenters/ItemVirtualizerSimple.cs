// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Linq;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Utils;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Utilities;
using Avalonia.VisualTree;

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
            // Don't need to add children here as UpdateControls should be called by the panel
            // measure/arrange.
        }

        /// <inheritdoc/>
        public override bool IsLogicalScrollEnabled => true;

        /// <inheritdoc/>
        public override double ExtentValue => Math.Ceiling((double)ItemCount/VirtualizingPanel.ScrollQuantum);

        /// <inheritdoc/>
        public override double OffsetValue
        {
            get
            {
                var offset = VirtualizingPanel.PixelOffset > 0 ? 1 : 0;
                return FirstIndex / VirtualizingPanel.ScrollQuantum + offset;
            }

            set
            {
                var panel = VirtualizingPanel;
                var offset = panel.PixelOffset > 0 ? 1 : 0;
                var current = FirstIndex / panel.ScrollQuantum + offset;
                var delta = ((int)value - current);

                delta *= panel.ScrollQuantum;

                if (delta != 0)
                {
                    var newLastIndex = (NextIndex - 1) + delta;

                    if (newLastIndex < ItemCount)
                    {
                        if (panel.PixelOffset > 0)
                        {
                            panel.PixelOffset = 0;
                            delta += panel.ScrollQuantum;
                        }

                        if (delta != 0)
                        {
                            RecycleContainersForMove(delta);
                        }
                    }
                    else
                    {
                        // We're moving to a partially obscured item at the end of the list so
                        // offset the panel by the height of the first item.
                        var firstIndex = ItemCount - panel.Children.Count;
                        firstIndex = (int)Math.Ceiling((double)firstIndex / (double)panel.ScrollQuantum) * panel.ScrollQuantum;
                        RecycleContainersForMove(firstIndex - FirstIndex);

                        double pixelOffset;
                        var child = panel.Children[0];

                        if (child.IsArrangeValid)
                        {
                            pixelOffset = VirtualizingPanel.ScrollDirection == Orientation.Vertical ?
                                                    child.Bounds.Height :
                                                    child.Bounds.Width;
                        }
                        else
                        {
                            pixelOffset = VirtualizingPanel.ScrollDirection == Orientation.Vertical ?
                                                    child.DesiredSize.Height :
                                                    child.DesiredSize.Width;
                        }

                        panel.PixelOffset = pixelOffset;
                    }
                }
            }
        }

        /// <inheritdoc/>
        public override double ViewportValue
        {
            get
            {
                // If we can't fit the last items in the panel fully, subtract 1 line from the viewport.
                var overflow = VirtualizingPanel.PixelOverflow > 0 ? 1 : 0;
                return Math.Ceiling((double)VirtualizingPanel.Children.Count / VirtualizingPanel.ScrollQuantum) - overflow;
            }
        }

        /// <inheritdoc/>
        public override Size MeasureOverride(Size availableSize)
        {
            var scrollable = (ILogicalScrollable)Owner;
            var visualRoot = Owner.GetVisualRoot();
            var maxAvailableSize = (visualRoot as WindowBase)?.PlatformImpl?.MaxClientSize
                 ?? (visualRoot as TopLevel)?.ClientSize;

            // If infinity is passed as the available size and we're virtualized then we need to
            // fill the available space, but to do that we *don't* want to materialize all our
            // items! Take a look at the root of the tree for a MaxClientSize and use that as
            // the available size.
            if (VirtualizingPanel.ScrollDirection == Orientation.Vertical)
            {
                if (availableSize.Height == double.PositiveInfinity)
                {
                    if (maxAvailableSize.HasValue)
                    {
                        availableSize = availableSize.WithHeight(maxAvailableSize.Value.Height);
                    }
                }

                if (scrollable.CanHorizontallyScroll)
                {
                    availableSize = availableSize.WithWidth(double.PositiveInfinity);
                }
            }
            else
            {
                if (availableSize.Width == double.PositiveInfinity)
                {
                    if (maxAvailableSize.HasValue)
                    {
                        availableSize = availableSize.WithWidth(maxAvailableSize.Value.Width);
                    }
                }

                if (scrollable.CanVerticallyScroll)
                {
                    availableSize = availableSize.WithHeight(double.PositiveInfinity);
                }
            }

            Owner.Panel.Measure(availableSize);
            return Owner.Panel.DesiredSize;
        }

        /// <inheritdoc/>
        public override void UpdateControls()
        {
            CreateAndRemoveContainers();
            InvalidateScroll();
        }

        /// <inheritdoc/>
        public override void ItemsChanged(IEnumerable items, NotifyCollectionChangedEventArgs e)
        {
            base.ItemsChanged(items, e);

            var panel = VirtualizingPanel;

            if (items != null)
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        CreateAndRemoveContainers();

                        if (e.NewStartingIndex < NextIndex)
                        {
                            RecycleContainers();
                        }

                        panel.ForceInvalidateMeasure();
                        break;

                    case NotifyCollectionChangedAction.Remove:
                        if (e.OldStartingIndex >= FirstIndex &&
                            e.OldStartingIndex < NextIndex)
                        {
                            RecycleContainersOnRemove();
                        }

                        panel.ForceInvalidateMeasure();
                        break;

                    case NotifyCollectionChangedAction.Move:
                    case NotifyCollectionChangedAction.Replace:
                        RecycleContainers();
                        break;

                    case NotifyCollectionChangedAction.Reset:
                        RecycleContainersOnRemove();
                        CreateAndRemoveContainers();
                        panel.ForceInvalidateMeasure();
                        break;
                }
            }
            else
            {
                Owner.ItemContainerGenerator.Clear();
                VirtualizingPanel.Children.Clear();
                FirstIndex = NextIndex = 0;
            }

            // If we are scrolled to view a partially visible last item but controls were added
            // then we need to return to a non-offset scroll position.
            if (panel.PixelOffset != 0 && FirstIndex + panel.Children.Count < ItemCount)
            {
                panel.PixelOffset = 0;
                RecycleContainersForMove(VirtualizingPanel.ScrollQuantum);
            }

            InvalidateScroll();
        }

        public override IControl GetControlInDirection(NavigationDirection direction, IControl from)
        {
            var generator = Owner.ItemContainerGenerator;
            var panel = VirtualizingPanel;
            var itemIndex = generator.IndexFromContainer(from);
            var vertical = VirtualizingPanel.ScrollDirection == Orientation.Vertical;

            if (itemIndex == -1)
            {
                return null;
            }

            var newItemIndex = -1;

            switch (direction)
            {
                case NavigationDirection.First:
                    newItemIndex = 0;
                    break;

                case NavigationDirection.Last:
                    newItemIndex = ItemCount - 1;
                    break;

                case NavigationDirection.Up:
                    if (vertical)
                    {
                        newItemIndex = itemIndex - panel.ScrollQuantum;
                    }
                    else if (panel.ScrollQuantum > 1)
                    {
                        newItemIndex = itemIndex - 1;
                    }

                    break;
                case NavigationDirection.Down:
                    if (vertical)
                    {
                        newItemIndex = itemIndex + panel.ScrollQuantum;
                    }
                    else if(panel.ScrollQuantum>1)
                    {
                        newItemIndex = itemIndex + 1;
                    }

                    break;

                case NavigationDirection.Left:
                    if (!vertical)
                    {
                        newItemIndex = itemIndex - panel.ScrollQuantum;
                    }
                    else if (panel.ScrollQuantum > 1)
                    {
                        newItemIndex = itemIndex - 1;
                    }
                    break;

                case NavigationDirection.Right:
                    if (!vertical)
                    {
                        newItemIndex = itemIndex + panel.ScrollQuantum;
                    }
                    else if (panel.ScrollQuantum > 1)
                    {
                        newItemIndex = itemIndex + 1;
                    }
                    break;

                case NavigationDirection.PageUp:
                    newItemIndex = Math.Max(0, itemIndex - (int)ViewportValue);
                    break;

                case NavigationDirection.PageDown:
                    newItemIndex = Math.Min(ItemCount - 1, itemIndex + (int)ViewportValue);
                    break;
            }
            return ScrollIntoView(newItemIndex);
        }

        /// <inheritdoc/>
        public override void ScrollIntoView(object item)
        {
            var index = Items.IndexOf(item);

            if (index != -1)
            {
                ScrollIntoView(index);
            }
        }

        /// <summary>
        /// Creates and removes containers such that we have at most enough containers to fill
        /// the panel.
        /// </summary>
        private void CreateAndRemoveContainers()
        {
            var generator = Owner.ItemContainerGenerator;
            var panel = VirtualizingPanel;

            if (Items != null && panel.IsAttachedToVisualTree)
            {
                var memberSelector = Owner.MemberSelector;
                var index = FirstIndex - 1;
                var step = 1;

                // check scroll alignment - add to start until aligned
                var toAdd = FirstIndex % panel.ScrollQuantum;
                
                // add to start of panel
                for (int i = 0; i < toAdd; i++)
                {
                    var materialized = generator.Materialize(index, Items.ElementAt(index), memberSelector);
                    panel.Children.Insert(0, materialized.ContainerControl);
                    --index;
                    --FirstIndex;
                }
                index = NextIndex;
                    
                if (!panel.IsFull)
                {
                    while (!panel.IsFull && index >= 0)
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
                        //make sure first is scroll quantum
                        //allow panel to be partially empty on last line
                        toAdd = 1;
                        if (step < 0)
                        {
                            if (panel.PixelOverflow > 0)
                            {
                                break;
                            }
                            toAdd = (index + 1) % panel.ScrollQuantum == 0 ? panel.ScrollQuantum : (index + 1) % panel.ScrollQuantum;
                        }

                        for (int i = 0; i < toAdd; i++)
                        {

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
        /// that are still visible to the correct place, and recycles and moves the others. For
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

            // validate delta it should never overflow last index or generate index < 0 
            var clampedDelta = MathUtilities.Clamp(delta, -FirstIndex, ItemCount - FirstIndex - panel.Children.Count);
            if (clampedDelta == 0)
                return;

            var sign = delta < 0 ? -1 : 1;
            var count = Math.Min(Math.Abs(delta), panel.Children.Count);
            var move = count < panel.Children.Count;
            var first = delta < 0 && move ? panel.Children.Count + delta : 0;
            //adjust recycle count if not enough
            int toAdd = 0;
            if (delta < 0)
            {
                toAdd = panel.Children.Count - panel.Children.Count / panel.ScrollQuantum * panel.ScrollQuantum;
                toAdd = (panel.ScrollQuantum - toAdd) % panel.ScrollQuantum;
                first += toAdd;
                count -= toAdd;
            }
            
            var oldItemIndex = FirstIndex + first;
            var newItemIndex = oldItemIndex + delta + ((panel.Children.Count - count) * sign);

            for (var i = 0; i < count- (delta - clampedDelta); ++i)
            {        
                var item = Items.ElementAt(newItemIndex);

                if (!generator.TryRecycle(oldItemIndex, newItemIndex, item, selector))
                {
                    throw new NotImplementedException();
                }
                
                oldItemIndex++;
                newItemIndex++;
            }
            for (var i = 0; i < toAdd; i++)
            {
                var materialized = generator.Materialize(newItemIndex, Items.ElementAt(newItemIndex), selector);
                panel.Children.Add(materialized.ContainerControl);
                newItemIndex++;
            }

            if (move)
            {
                if (delta > 0)
                {
                    panel.Children.MoveRange(first, count, panel.Children.Count);
                }
                else
                {
                    panel.Children.MoveRange(first, count + toAdd, 0);
                }
            }

            if (clampedDelta < delta)
            {
                panel.Children.RemoveRange(panel.Children.Count - (delta - clampedDelta), (delta - clampedDelta));
                Owner.ItemContainerGenerator.Dematerialize(oldItemIndex, (delta - clampedDelta));
            }

            FirstIndex += delta;
            NextIndex = FirstIndex+ panel.Children.Count;
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
                newFirstIndex += newFirstIndex % panel.ScrollQuantum;
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

        /// <summary>
        /// Scrolls the item with the specified index into view.
        /// </summary>
        /// <param name="index">The item index.</param>
        /// <returns>The container that was brought into view.</returns>
        private IControl ScrollIntoView(int index)
        {
            var panel = VirtualizingPanel;
            var generator = Owner.ItemContainerGenerator;
            var newOffset = -1.0;

            if (index >= 0 && index < ItemCount)
            {
                var offset = panel.PixelOffset > 0 ? VirtualizingPanel.ScrollQuantum : 0;

                if (index <= FirstIndex+offset)
                {
                    newOffset = index;
                }
                else if (index >= NextIndex)
                {
                    newOffset = index - Math.Ceiling(ViewportValue - 1);
                }
                
                if (newOffset != -1)
                {
                    OffsetValue = newOffset/panel.ScrollQuantum;
                }

                var container = generator.ContainerFromIndex(index);
                var layoutManager = (Owner.GetVisualRoot() as ILayoutRoot)?.LayoutManager;

                // We need to do a layout here because it's possible that the container we moved to
                // is only partially visible due to differing item sizes. If the container is only 
                // partially visible, scroll again. Don't do this if there's no layout manager:
                // it means we're running a unit test.
                if (container != null && layoutManager != null)
                {
                    layoutManager.ExecuteLayoutPass();

                    if (panel.ScrollDirection == Orientation.Vertical)
                    {
                        if (container.Bounds.Y < panel.Bounds.Y || container.Bounds.Bottom > panel.Bounds.Bottom)
                        {
                            OffsetValue += 1;
                        }
                    }
                    else
                    {
                        if (container.Bounds.X < panel.Bounds.X || container.Bounds.Right > panel.Bounds.Right)
                        {
                            OffsetValue += 1;
                        }
                    }
                }

                return container;
            }

            return null;
        }

        /// <summary>
        /// Ensures an offset value is within the value range.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The coerced value.</returns>
        private double CoerceOffset(double value)
        {
            var max = Math.Max(ExtentValue - ViewportValue, 0);           
            return MathUtilities.Clamp(value, 0, max);
        }
    }
}
