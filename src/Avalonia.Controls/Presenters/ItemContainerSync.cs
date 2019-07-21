using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia.Controls.Generators;
using Avalonia.Controls.Utils;

namespace Avalonia.Controls.Presenters
{
    internal static class ItemContainerSync
    {
        public static void ItemsChanged(
            ItemsPresenterBase owner,
            IEnumerable items,
            NotifyCollectionChangedEventArgs e)
        {
            var generator = owner.ItemContainerGenerator;
            var panel = owner.Panel;

            if (panel == null)
            {
                return;
            }

            void Add()
            {
                if (e.NewStartingIndex + e.NewItems.Count < items.Count())
                {
                    generator.InsertSpace(e.NewStartingIndex, e.NewItems.Count);
                }

                AddContainers(owner, e.NewStartingIndex, e.NewItems);
            }

            void Remove()
            {
                RemoveContainers(panel, generator.RemoveRange(e.OldStartingIndex, e.OldItems.Count));
            }

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    Add();
                    break;

                case NotifyCollectionChangedAction.Remove:
                    Remove();
                    break;

                case NotifyCollectionChangedAction.Replace:
                    RemoveContainers(panel, generator.Dematerialize(e.OldStartingIndex, e.OldItems.Count));
                    var containers = AddContainers(owner, e.NewStartingIndex, e.NewItems);

                    var i = e.NewStartingIndex;

                    foreach (var container in containers)
                    {
                        panel.Children[i++] = container.ContainerControl;
                    }

                    break;

                case NotifyCollectionChangedAction.Move:
                    Remove();
                    Add();
                    break;

                case NotifyCollectionChangedAction.Reset:
                    RemoveContainers(panel, generator.Clear());

                    if (items != null)
                    {
                        AddContainers(owner, 0, items);
                    }

                    break;
            }
        }

        private static IList<ItemContainerInfo> AddContainers(
            ItemsPresenterBase owner,
            int index,
            IEnumerable items)
        {
            var generator = owner.ItemContainerGenerator;
            var result = new List<ItemContainerInfo>();
            var panel = owner.Panel;

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

        private static void RemoveContainers(
            IPanel panel,
            IEnumerable<ItemContainerInfo> items)
        {
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
