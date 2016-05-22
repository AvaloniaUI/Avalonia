// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Avalonia.Controls.Generators;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Utils;
using Avalonia.Input;

namespace Avalonia.Controls.Presenters
{
    /// <summary>
    /// Displays items inside an <see cref="ItemsControl"/>.
    /// </summary>
    public class ItemsPresenter : ItemsPresenterBase, IScrollable
    {
        /// <summary>
        /// Defines the <see cref="VirtualizationMode"/> property.
        /// </summary>
        public static readonly StyledProperty<ItemVirtualizationMode> VirtualizationModeProperty =
            AvaloniaProperty.Register<ItemsPresenter, ItemVirtualizationMode>(
                nameof(VirtualizationMode),
                defaultValue: ItemVirtualizationMode.Simple);

        private VirtualizationInfo _virt;

        /// <summary>
        /// Initializes static members of the <see cref="ItemsPresenter"/> class.
        /// </summary>
        static ItemsPresenter()
        {
            KeyboardNavigation.TabNavigationProperty.OverrideDefaultValue(
                typeof(ItemsPresenter),
                KeyboardNavigationMode.Once);
        }

        /// <summary>
        /// Gets or sets the virtualization mode for the items.
        /// </summary>
        public ItemVirtualizationMode VirtualizationMode
        {
            get { return GetValue(VirtualizationModeProperty); }
            set { SetValue(VirtualizationModeProperty, value); }
        }

        /// <inheritdoc/>
        bool IScrollable.IsLogicalScrollEnabled
        {
            get { return _virt != null && VirtualizationMode != ItemVirtualizationMode.None; }
        }

        /// <inheritdoc/>
        Action IScrollable.InvalidateScroll { get; set; }

        /// <inheritdoc/>
        Size IScrollable.Extent
        {
            get
            {
                switch (VirtualizationMode)
                {
                    case ItemVirtualizationMode.Simple:
                        return new Size(0, Items?.Count() ?? 0);
                    default:
                        return default(Size);
                }
            }
        }

        /// <inheritdoc/>
        Vector IScrollable.Offset { get; set; }

        /// <inheritdoc/>
        Size IScrollable.Viewport
        {
            get
            {
                switch (VirtualizationMode)
                {
                    case ItemVirtualizationMode.Simple:
                        return new Size(0, (_virt.LastIndex - _virt.FirstIndex) + 1);
                    default:
                        return default(Size);
                }
            }
        }

        /// <inheritdoc/>
        Size IScrollable.ScrollSize => new Size(0, 1);

        /// <inheritdoc/>
        Size IScrollable.PageScrollSize => new Size(0, 1);

        /// <inheritdoc/>
        protected override Size ArrangeOverride(Size finalSize)
        {
            var result = base.ArrangeOverride(finalSize);

            if (_virt != null)
            {
                CreateRemoveVirtualizedContainers();
                ((IScrollable)this).InvalidateScroll();

            }

            return result;
        }

        /// <inheritdoc/>
        protected override void PanelCreated(IPanel panel)
        {
            if (((IScrollable)this).InvalidateScroll != null)
            {
                var virtualizingPanel = Panel as IVirtualizingPanel;
                _virt = virtualizingPanel != null ? new VirtualizationInfo(virtualizingPanel) : null;
            }

            if (!Panel.IsSet(KeyboardNavigation.DirectionalNavigationProperty))
            {
                KeyboardNavigation.SetDirectionalNavigation(
                    (InputElement)Panel,
                    KeyboardNavigationMode.Contained);
            }

            KeyboardNavigation.SetTabNavigation(
                (InputElement)Panel,
                KeyboardNavigation.GetTabNavigation(this));
        }

        protected override void ItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            if (_virt == null)
            {
                ItemsChangedNonVirtualized(e);
            }
            else
            {
                ItemsChangedVirtualized(e);
            }
        }

        private void ItemsChangedNonVirtualized(NotifyCollectionChangedEventArgs e)
        {
            var generator = ItemContainerGenerator;

            // TODO: Handle Move and Replace etc.
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (e.NewStartingIndex + e.NewItems.Count < Items.Count())
                    {
                        generator.InsertSpace(e.NewStartingIndex, e.NewItems.Count);
                    }

                    AddContainersNonVirtualized(e.NewStartingIndex, e.NewItems);
                    break;

                case NotifyCollectionChangedAction.Remove:
                    RemoveContainers(generator.RemoveRange(e.OldStartingIndex, e.OldItems.Count));
                    break;

                case NotifyCollectionChangedAction.Replace:
                    RemoveContainers(generator.Dematerialize(e.OldStartingIndex, e.OldItems.Count));
                    var containers = AddContainersNonVirtualized(e.NewStartingIndex, e.NewItems);

                    var i = e.NewStartingIndex;

                    foreach (var container in containers)
                    {
                        Panel.Children[i++] = container.ContainerControl;
                    }

                    break;

                case NotifyCollectionChangedAction.Move:
                // TODO: Implement Move in a more efficient manner.
                case NotifyCollectionChangedAction.Reset:
                    RemoveContainers(generator.Clear());

                    if (Items != null)
                    {
                        AddContainersNonVirtualized(0, Items);
                    }

                    break;
            }

            InvalidateMeasure();
        }

        private void ItemsChangedVirtualized(NotifyCollectionChangedEventArgs e)
        {
        }

        private IList<ItemContainerInfo> AddContainersNonVirtualized(int index, IEnumerable items)
        {
            var generator = ItemContainerGenerator;
            var result = new List<ItemContainerInfo>();

            foreach (var item in items)
            {
                var i = generator.Materialize(index++, item, MemberSelector);

                if (i.ContainerControl != null)
                {
                    if (i.Index < this.Panel.Children.Count)
                    {
                        // TODO: This will insert at the wrong place when there are null items.
                        this.Panel.Children.Insert(i.Index, i.ContainerControl);
                    }
                    else
                    {
                        this.Panel.Children.Add(i.ContainerControl);
                    }
                }

                result.Add(i);
            }

            return result;
        }

        private void CreateRemoveVirtualizedContainers()
        {
            var generator = ItemContainerGenerator;
            var panel = _virt.Panel;

            if (!panel.IsFull)
            {
                var index = _virt.LastIndex + 1;
                var items = Items.Cast<object>().Skip(index);
                var memberSelector = MemberSelector;

                foreach (var item in items)
                {
                    var materialized = generator.Materialize(index++, item, memberSelector);
                    panel.Children.Add(materialized.ContainerControl);

                    if (panel.IsFull)
                    {
                        break;
                    }
                }

                _virt.LastIndex = index - 1;
            }

            if (panel.OverflowCount > 0)
            {
                var remove = panel.OverflowCount;

                panel.Children.RemoveRange(
                    panel.Children.Count - remove,
                    panel.OverflowCount);
                _virt.LastIndex -= remove;
            }
        }

        private void RemoveContainers(IEnumerable<ItemContainerInfo> items)
        {
            foreach (var i in items)
            {
                if (i.ContainerControl != null)
                {
                    this.Panel.Children.Remove(i.ContainerControl);
                }
            }
        }

        private class VirtualizationInfo
        {
            public VirtualizationInfo(IVirtualizingPanel panel)
            {
                Panel = panel;
            }

            public IVirtualizingPanel Panel { get; }
            public int FirstIndex { get; set; }
            public int LastIndex { get; set; } = -1;
        }
    }
}