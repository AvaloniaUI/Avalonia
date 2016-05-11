// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia.Controls.Generators;
using Avalonia.Controls.Utils;
using Avalonia.Input;

namespace Avalonia.Controls.Presenters
{
    /// <summary>
    /// Displays items inside an <see cref="ItemsControl"/>.
    /// </summary>
    public class ItemsPresenter : ItemsPresenterBase
    {
        /// <summary>
        /// Initializes static members of the <see cref="ItemsPresenter"/> class.
        /// </summary>
        static ItemsPresenter()
        {
            KeyboardNavigation.TabNavigationProperty.OverrideDefaultValue(
                typeof(ItemsPresenter),
                KeyboardNavigationMode.Once);
        }

        /// <inheritdoc/>
        protected override void CreatePanel()
        {
            base.CreatePanel();

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

                    AddContainers(generator.Materialize(e.NewStartingIndex, e.NewItems, MemberSelector));
                    break;

                case NotifyCollectionChangedAction.Remove:
                    RemoveContainers(generator.RemoveRange(e.OldStartingIndex, e.OldItems.Count));
                    break;

                case NotifyCollectionChangedAction.Replace:
                    RemoveContainers(generator.Dematerialize(e.OldStartingIndex, e.OldItems.Count));
                    var containers = generator.Materialize(e.NewStartingIndex, e.NewItems, MemberSelector);
                    AddContainers(containers);

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
                        AddContainers(generator.Materialize(0, Items, MemberSelector));
                    }

                    break;
            }

            InvalidateMeasure();
        }

        private void AddContainersToPanel(IEnumerable<ItemContainer> items)
        {
            foreach (var i in items)
            {
                if (i.ContainerControl != null)
                {
                    if (i.Index < this.Panel.Children.Count)
                    {
                        // HACK: This will insert at the wrong place when there are null items,
                        // but all of this will need to be rewritten when we implement 
                        // virtualization so hope no-one notices until then :)
                        this.Panel.Children.Insert(i.Index, i.ContainerControl);
                    }
                    else
                    {
                        this.Panel.Children.Add(i.ContainerControl);
                    }
                }
            }
        }

        private void AddContainers(IEnumerable<ItemContainer> items)
        {
            foreach (var i in items)
            {
                if (i.ContainerControl != null)
                {
                    if (i.Index < this.Panel.Children.Count)
                    {
                        // HACK: This will insert at the wrong place when there are null items,
                        // but all of this will need to be rewritten when we implement 
                        // virtualization so hope no-one notices until then :)
                        this.Panel.Children.Insert(i.Index, i.ContainerControl);
                    }
                    else
                    {
                        this.Panel.Children.Add(i.ContainerControl);
                    }
                }
            }
        }

        private void RemoveContainers(IEnumerable<ItemContainer> items)
        {
            foreach (var i in items)
            {
                if (i.ContainerControl != null)
                {
                    this.Panel.Children.Remove(i.ContainerControl);
                }
            }
        }
    }
}