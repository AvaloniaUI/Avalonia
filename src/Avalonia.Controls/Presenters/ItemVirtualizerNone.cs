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
    internal class ItemVirtualizerNone : ItemVirtualizer
    {
        public ItemVirtualizerNone(ItemsPresenter owner)
            : base(owner)
        {
        }

        public override bool IsLogicalScrollEnabled => false;

        public override Size Extent
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        public override Size Viewport
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        public override void Arranging(Size finalSize)
        {
            // We don't need to do anything here.
        }

        public override void ItemsChanged(IEnumerable items, NotifyCollectionChangedEventArgs e)
        {
            base.ItemsChanged(items, e);

            var generator = Owner.ItemContainerGenerator;
            var panel = Owner.Panel;

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
                        panel.Children[i++] = container.ContainerControl;
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

            Owner.InvalidateMeasure();
        }

        private IList<ItemContainerInfo> AddContainers(int index, IEnumerable items)
        {
            var generator = Owner.ItemContainerGenerator;
            var result = new List<ItemContainerInfo>();
            var panel = Owner.Panel;

            foreach (var item in items)
            {
                var i = generator.Materialize(index++, item, Owner.MemberSelector);

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
