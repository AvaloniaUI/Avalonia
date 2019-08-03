// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia.Controls.Generators;
using Avalonia.Controls.Utils;

namespace Avalonia.Controls.Presenters
{
    /// <summary>
    /// Represents an item virtualizer for an <see cref="ItemsPresenter"/> that doesn't actually
    /// virtualize items - it just creates a container for every item.
    /// </summary>
    internal class ItemVirtualizerNone : ItemVirtualizer
    {
        public ItemVirtualizerNone(ItemsPresenter owner)
            : base(owner)
        {
            if (Items != null && owner.Panel != null)
            {
                AddContainers(0, Items);
            }
        }

        /// <inheritdoc/>
        public override bool IsLogicalScrollEnabled => false;

        /// <summary>
        /// This property should never be accessed because <see cref="IsLogicalScrollEnabled"/> is
        /// false.
        /// </summary>
        public override double ExtentValue
        {
            get { throw new NotSupportedException(); }
        }

        /// <summary>
        /// This property should never be accessed because <see cref="IsLogicalScrollEnabled"/> is
        /// false.
        /// </summary>
        public override double OffsetValue
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        /// <summary>
        /// This property should never be accessed because <see cref="IsLogicalScrollEnabled"/> is
        /// false.
        /// </summary>
        public override double ViewportValue
        {
            get { throw new NotSupportedException(); }
        }

        /// <inheritdoc/>
        public override void ItemsChanged(IEnumerable items, NotifyCollectionChangedEventArgs e)
        {
            base.ItemsChanged(items, e);
            ItemContainerSync.ItemsChanged(Owner, items, e);
            Owner.InvalidateMeasure();
        }

        /// <summary>
        /// Scrolls the specified item into view.
        /// </summary>
        /// <param name="item">The item.</param>
        public override void ScrollIntoView(object item)
        {
            if (Items != null)
            {
                var index = Items.IndexOf(item);

                if (index != -1)
                {
                    var container = Owner.ItemContainerGenerator.ContainerFromIndex(index);
                    container?.BringIntoView();
                }
            }
        }

        private IList<ItemContainerInfo> AddContainers(int index, IEnumerable items)
        {
            var generator = Owner.ItemContainerGenerator;
            var result = new List<ItemContainerInfo>();
            var panel = Owner.Panel;

            foreach (var item in items)
            {
                var i = generator.Materialize(index++, item);

                if (i.ContainerControl != null)
                {
                    if (i.Index < panel.Children.Count)
                    {
                        // TODO: This will insert at the wrong place when there are null items.
                        panel.Children.Insert(i.Index, i.ContainerControl);
                    }
                    else
                    {
                        panel.Children.Add(i.ContainerControl);
                    }
                }

                result.Add(i);
            }

            return result;
        }

        private void RemoveContainers(IEnumerable<ItemContainerInfo> items)
        {
            var panel = Owner.Panel;

            foreach (var i in items)
            {
                if (i.ContainerControl != null)
                {
                    panel.Children.Remove(i.ContainerControl);
                }
            }
        }
    }
}
