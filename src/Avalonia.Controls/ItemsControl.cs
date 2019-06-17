// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Controls.Generators;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Controls.Utils;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Metadata;
using Avalonia.VisualTree;

namespace Avalonia.Controls
{
    /// <summary>
    /// Displays a collection of items.
    /// </summary>
    public class ItemsControl : TemplatedControl, IItemsPresenterHost
    {
        /// <summary>
        /// The default value for the <see cref="ItemsPanel"/> property.
        /// </summary>
        private static readonly FuncTemplate<IPanel> DefaultPanel =
            new FuncTemplate<IPanel>(() => new StackPanel());

        /// <summary>
        /// Defines the <see cref="Items"/> property.
        /// </summary>
        public static readonly DirectProperty<ItemsControl, IEnumerable> ItemsProperty =
            AvaloniaProperty.RegisterDirect<ItemsControl, IEnumerable>(nameof(Items), o => o.Items, (o, v) => o.Items = v);

        /// <summary>
        /// Defines the <see cref="ItemsPanel"/> property.
        /// </summary>
        public static readonly StyledProperty<ITemplate<IPanel>> ItemsPanelProperty =
            AvaloniaProperty.Register<ItemsControl, ITemplate<IPanel>>(nameof(ItemsPanel), DefaultPanel);

        /// <summary>
        /// Defines the <see cref="ItemTemplate"/> property.
        /// </summary>
        public static readonly StyledProperty<IDataTemplate> ItemTemplateProperty =
            AvaloniaProperty.Register<ItemsControl, IDataTemplate>(nameof(ItemTemplate));

        /// <summary>
        /// Defines the <see cref="MemberSelector"/> property.
        /// </summary>
        public static readonly StyledProperty<IMemberSelector> MemberSelectorProperty =
            AvaloniaProperty.Register<ItemsControl, IMemberSelector>(nameof(MemberSelector));

        private IEnumerable _items = new AvaloniaList<object>();
        private IItemContainerGenerator _itemContainerGenerator;
        private IDisposable _itemsCollectionChangedSubscription;

        /// <summary>
        /// Initializes static members of the <see cref="ItemsControl"/> class.
        /// </summary>
        static ItemsControl()
        {
            ItemsProperty.Changed.AddClassHandler<ItemsControl>(x => x.ItemsChanged);
            ItemTemplateProperty.Changed.AddClassHandler<ItemsControl>(x => x.ItemTemplateChanged);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemsControl"/> class.
        /// </summary>
        public ItemsControl()
        {
            PseudoClasses.Add(":empty");
            SubscribeToItems(_items);
        }

        /// <summary>
        /// Gets the <see cref="IItemContainerGenerator"/> for the control.
        /// </summary>
        public IItemContainerGenerator ItemContainerGenerator
        {
            get
            {
                if (_itemContainerGenerator == null)
                {
                    _itemContainerGenerator = CreateItemContainerGenerator();

                    if (_itemContainerGenerator != null)
                    {
                        _itemContainerGenerator.ItemTemplate = ItemTemplate;
                        _itemContainerGenerator.Materialized += (_, e) => OnContainersMaterialized(e);
                        _itemContainerGenerator.Dematerialized += (_, e) => OnContainersDematerialized(e);
                        _itemContainerGenerator.Recycled += (_, e) => OnContainersRecycled(e);
                    }
                }

                return _itemContainerGenerator;
            }
        }

        /// <summary>
        /// Gets or sets the items to display.
        /// </summary>
        [Content]
        public IEnumerable Items
        {
            get { return _items; }
            set { SetAndRaise(ItemsProperty, ref _items, value); }
        }

        public int ItemCount
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the panel used to display the items.
        /// </summary>
        public ITemplate<IPanel> ItemsPanel
        {
            get { return GetValue(ItemsPanelProperty); }
            set { SetValue(ItemsPanelProperty, value); }
        }

        /// <summary>
        /// Gets or sets the data template used to display the items in the control.
        /// </summary>
        public IDataTemplate ItemTemplate
        {
            get { return GetValue(ItemTemplateProperty); }
            set { SetValue(ItemTemplateProperty, value); }
        }

        /// <summary>
        /// Selects a member from <see cref="Items"/> to use as the list item.
        /// </summary>
        public IMemberSelector MemberSelector
        {
            get { return GetValue(MemberSelectorProperty); }
            set { SetValue(MemberSelectorProperty, value); }
        }

        /// <summary>
        /// Gets the items presenter control.
        /// </summary>
        public IItemsPresenter Presenter
        {
            get;
            protected set;
        }

        /// <inheritdoc/>
        void IItemsPresenterHost.RegisterItemsPresenter(IItemsPresenter presenter)
        {
            Presenter = presenter;
            ItemContainerGenerator.Clear();
        }

        /// <summary>
        /// Gets the item at the specified index in a collection.
        /// </summary>
        /// <param name="items">The collection.</param>
        /// <param name="index">The index.</param>
        /// <returns>The index of the item or -1 if the item was not found.</returns>
        protected static object ElementAt(IEnumerable items, int index)
        {
            if (index != -1 && index < items.Count())
            {
                return items.ElementAt(index) ?? null;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the index of an item in a collection.
        /// </summary>
        /// <param name="items">The collection.</param>
        /// <param name="item">The item.</param>
        /// <returns>The index of the item or -1 if the item was not found.</returns>
        protected static int IndexOf(IEnumerable items, object item)
        {
            if (items != null && item != null)
            {
                var list = items as IList;

                if (list != null)
                {
                    return list.IndexOf(item);
                }
                else
                {
                    int index = 0;

                    foreach (var i in items)
                    {
                        if (Equals(i, item))
                        {
                            return index;
                        }

                        ++index;
                    }
                }
            }

            return -1;
        }

        /// <summary>
        /// Creates the <see cref="ItemContainerGenerator"/> for the control.
        /// </summary>
        /// <returns>
        /// An <see cref="IItemContainerGenerator"/> or null.
        /// </returns>
        /// <remarks>
        /// Certain controls such as <see cref="TabControl"/> don't actually create item 
        /// containers; however they want it to be ItemsControls so that they have an Items 
        /// property etc. In this case, a derived class can override this method to return null
        /// in order to disable the creation of item containers.
        /// </remarks>
        protected virtual IItemContainerGenerator CreateItemContainerGenerator()
        {
            return new ItemContainerGenerator(this);
        }

        /// <summary>
        /// Called when new containers are materialized for the <see cref="ItemsControl"/> by its
        /// <see cref="ItemContainerGenerator"/>.
        /// </summary>
        /// <param name="e">The details of the containers.</param>
        protected virtual void OnContainersMaterialized(ItemContainerEventArgs e)
        {
            foreach (var container in e.Containers)
            {
                // If the item is its own container, then it will be added to the logical tree when
                // it was added to the Items collection.
                if (container.ContainerControl != null && container.ContainerControl != container.Item)
                {
                    if (ItemContainerGenerator.ContainerType == null)
                    {
                        var containerControl = container.ContainerControl as ContentPresenter;

                        if (containerControl != null)
                        {
                            ((ISetLogicalParent)containerControl).SetParent(this);
                            containerControl.UpdateChild();

                            if (containerControl.Child != null)
                            {
                                LogicalChildren.Add(containerControl.Child);
                            }
                        }
                    }
                    else
                    {
                        LogicalChildren.Add(container.ContainerControl);
                    }
                }
            }
        }

        /// <summary>
        /// Called when containers are dematerialized for the <see cref="ItemsControl"/> by its
        /// <see cref="ItemContainerGenerator"/>.
        /// </summary>
        /// <param name="e">The details of the containers.</param>
        protected virtual void OnContainersDematerialized(ItemContainerEventArgs e)
        {
            foreach (var container in e.Containers)
            {
                // If the item is its own container, then it will be removed from the logical tree
                // when it is removed from the Items collection.
                if (container?.ContainerControl != container?.Item)
                {
                    if (ItemContainerGenerator.ContainerType == null)
                    {
                        var containerControl = container.ContainerControl as ContentPresenter;

                        if (containerControl != null)
                        {
                            ((ISetLogicalParent)containerControl).SetParent(null);

                            if (containerControl.Child != null)
                            {
                                LogicalChildren.Remove(containerControl.Child);
                            }
                        }
                    }
                    else
                    {
                        LogicalChildren.Remove(container.ContainerControl);
                    }
                }
            }
        }

        /// <summary>
        /// Called when containers are recycled for the <see cref="ItemsControl"/> by its
        /// <see cref="ItemContainerGenerator"/>.
        /// </summary>
        /// <param name="e">The details of the containers.</param>
        protected virtual void OnContainersRecycled(ItemContainerEventArgs e)
        {
            var toRemove = new List<ILogical>();

            foreach (var container in e.Containers)
            {
                // If the item is its own container, then it will be removed from the logical tree
                // when it is removed from the Items collection.
                if (container?.ContainerControl != container?.Item)
                {
                    toRemove.Add(container.ContainerControl);
                }
            }

            LogicalChildren.RemoveAll(toRemove);
        }

        /// <summary>
        /// Handles directional navigation within the <see cref="ItemsControl"/>.
        /// </summary>
        /// <param name="e">The key events.</param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (!e.Handled)
            {
                var focus = this.GetFocusManager();
                if(focus == null)
                    return;
                var direction = e.Key.ToNavigationDirection();
                var container = Presenter?.Panel as INavigableContainer;

                if (container == null ||
                    focus.FocusedElement == null ||
                    direction == null ||
                    direction.Value.IsTab())
                {
                    return;
                }

                var current = focus.FocusedElement
                    .GetSelfAndVisualAncestors()
                    .OfType<IInputElement>()
                    .FirstOrDefault(x => x.VisualParent == container);

                if (current != null)
                {
                    var next = GetNextControl(container, direction.Value, current, false);

                    if (next != null)
                    {
                        focus.Focus(next, NavigationMethod.Directional);
                        e.Handled = true;
                    }
                }
            }

            base.OnKeyDown(e);
        }

        /// <summary>
        /// Called when the <see cref="Items"/> property changes.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected virtual void ItemsChanged(AvaloniaPropertyChangedEventArgs e)
        {
            _itemsCollectionChangedSubscription?.Dispose();
            _itemsCollectionChangedSubscription = null;

            var oldValue = e.OldValue as IEnumerable;
            var newValue = e.NewValue as IEnumerable;

            UpdateItemCount();
            RemoveControlItemsFromLogicalChildren(oldValue);
            AddControlItemsToLogicalChildren(newValue);
            SubscribeToItems(newValue);
        }

        /// <summary>
        /// Called when the <see cref="INotifyCollectionChanged.CollectionChanged"/> event is
        /// raised on <see cref="Items"/>.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        protected virtual void ItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    AddControlItemsToLogicalChildren(e.NewItems);
                    break;

                case NotifyCollectionChangedAction.Remove:
                    RemoveControlItemsFromLogicalChildren(e.OldItems);
                    break;
            }

            UpdateItemCount();

            var collection = sender as ICollection;
            PseudoClasses.Set(":empty", collection == null || collection.Count == 0);
            PseudoClasses.Set(":singleitem", collection != null && collection.Count == 1);
        }

        /// <summary>
        /// Given a collection of items, adds those that are controls to the logical children.
        /// </summary>
        /// <param name="items">The items.</param>
        private void AddControlItemsToLogicalChildren(IEnumerable items)
        {
            var toAdd = new List<ILogical>();

            if (items != null)
            {
                foreach (var i in items)
                {
                    var control = i as IControl;

                    if (control != null && !LogicalChildren.Contains(control))
                    {
                        toAdd.Add(control);
                    }
                }
            }

            LogicalChildren.AddRange(toAdd);
        }

        /// <summary>
        /// Given a collection of items, removes those that are controls to from logical children.
        /// </summary>
        /// <param name="items">The items.</param>
        private void RemoveControlItemsFromLogicalChildren(IEnumerable items)
        {
            var toRemove = new List<ILogical>();

            if (items != null)
            {
                foreach (var i in items)
                {
                    var control = i as IControl;

                    if (control != null)
                    {
                        toRemove.Add(control);
                    }
                }
            }

            LogicalChildren.RemoveAll(toRemove);
        }

        /// <summary>
        /// Subscribes to an <see cref="Items"/> collection.
        /// </summary>
        /// <param name="items">The items collection.</param>
        private void SubscribeToItems(IEnumerable items)
        {
            PseudoClasses.Set(":empty", items == null || items.Count() == 0);
            PseudoClasses.Set(":singleitem", items != null && items.Count() == 1);

            var incc = items as INotifyCollectionChanged;

            if (incc != null)
            {
                _itemsCollectionChangedSubscription = incc.WeakSubscribe(ItemsCollectionChanged);
            }
        }

        /// <summary>
        /// Called when the <see cref="ItemTemplate"/> changes.
        /// </summary>
        /// <param name="e">The event args.</param>
        private void ItemTemplateChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (_itemContainerGenerator != null)
            {
                _itemContainerGenerator.ItemTemplate = (IDataTemplate)e.NewValue;
                // TODO: Rebuild the item containers.
            }
        }

        private void UpdateItemCount()
        {
            if (Items == null)
            {
                ItemCount = 0;
            }
            else if (Items is IList list)
            {
                ItemCount = list.Count;
            }
            else
            {
                ItemCount = Items.Count();
            }
        }

        protected static IInputElement GetNextControl(
            INavigableContainer container,
            NavigationDirection direction,
            IInputElement from,
            bool wrap)
        {
            IInputElement result;

            do
            {
                result = container.GetControl(direction, from, wrap);

                if (result?.Focusable == true)
                {
                    return result;
                }

                from = result;
            } while (from != null);

            return null;
        }
    }
}
