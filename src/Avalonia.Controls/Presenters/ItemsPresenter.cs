// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
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

        Vector IScrollable.Offset { get; set; }

        Size IScrollable.Viewport
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        Size IScrollable.ScrollSize
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        Size IScrollable.PageScrollSize
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <inheritdoc/>
        protected override void CreatePanel()
        {
            base.CreatePanel();

            var virtualizingPanel = Panel as IVirtualizingPanel;
            _virt = virtualizingPanel != null ? new VirtualizationInfo(virtualizingPanel) : null;

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

        /// <inheritdoc/>
        protected override void ItemsChanged(NotifyCollectionChangedEventArgs e)
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

                    AddContainers(e.NewStartingIndex, e.NewItems);
                    break;

                case NotifyCollectionChangedAction.Remove:
                    RemoveContainers(generator.RemoveRange(e.OldStartingIndex, e.OldItems.Count));
                    break;

                case NotifyCollectionChangedAction.Replace:
                    RemoveContainers(generator.Dematerialize(e.OldStartingIndex, e.OldItems.Count));
                    var containers = AddContainers(e.NewStartingIndex, e.NewItems);

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
                        AddContainers(0, Items);
                    }

                    break;
            }

            InvalidateMeasure();
        }

        private IList<ItemContainerInfo> AddContainers(int index, IEnumerable items)
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
            public int LastIndex { get; set; }
        }
    }
}