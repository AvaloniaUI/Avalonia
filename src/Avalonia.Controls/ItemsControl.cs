// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia.Collections;
using Avalonia.Controls.Generators;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Controls.Utils;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
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
        /// Defines the <see cref="ItemCount"/> property.
        /// </summary>
        public static readonly DirectProperty<ItemsControl, int> ItemCountProperty =
            AvaloniaProperty.RegisterDirect<ItemsControl, int>(nameof(ItemCount), o => o.ItemCount);

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
        /// Defines the <see cref="Layout"/> property.
        /// </summary>
        public static readonly AvaloniaProperty<AttachedLayout> LayoutProperty =
            ItemsRepeater.LayoutProperty.AddOwner<ItemsControl>();

        private IEnumerable _items = new AvaloniaList<object>();
        private int _itemCount;
        private IItemContainerGenerator _itemContainerGenerator;
        private IDisposable _itemsCollectionChangedSubscription;

        /// <summary>
        /// Initializes static members of the <see cref="ItemsControl"/> class.
        /// </summary>
        static ItemsControl()
        {
            ItemsProperty.Changed.AddClassHandler<ItemsControl>((x, e) => x.ItemsChanged(e));
            ItemTemplateProperty.Changed.AddClassHandler<ItemsControl>((x, e) => x.ItemTemplateChanged(e));
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

        /// <summary>
        /// Gets the number of items in <see cref="Items"/>.
        /// </summary>
        public int ItemCount
        {
            get => _itemCount;
            private set => SetAndRaise(ItemCountProperty, ref _itemCount, value);
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
        /// Gets or sets the layout used to size and position elements in the <see cref="ItemsControl"/>.
        /// </summary>
        /// <value>
        /// The layout used to size and position elements. The default is a <see cref="StackLayout"/> with
        /// vertical orientation.
        /// </value>
        public AttachedLayout Layout
        {
            get => GetValue(LayoutProperty);
            set => SetValue(LayoutProperty, value);
        }

        /// <summary>
        /// Gets the items presenter control.
        /// </summary>
        public IItemsPresenter Presenter
        {
            get;
            protected set;
        }

        /// <summary>
        /// Occurs each time a container is cleared and made available to be re-used.
        /// </summary>
        /// <remarks>
        /// This event is raised immediately each time a a container is cleared, such as when it
        /// falls outside the range of realized items. Elements are cleared when they become
        /// available for re-use.
        /// </remarks>
        public event EventHandler<ElementClearingEventArgs> ContainerClearing;

        /// <summary>
        /// Occurs each time a container is prepared for use.
        /// </summary>
        /// <remarks>
        /// The prepared container might be newly created or an existing element that is being re-
        /// used.
        /// </remarks>
        public event EventHandler<ElementPreparedEventArgs> ContainerPrepared;

        /// <inheritdoc/>
        IControl IItemsPresenterHost.CreateContainer(object data) => CreateContainer(data);

        /// <inheritdoc/>
        void IItemsPresenterHost.RegisterItemsPresenter(IItemsPresenter presenter)
        {
            var oldValue = Presenter;
            Presenter = presenter;
            ItemsPresenterChanged(oldValue as IItemsRepeaterPresenter, presenter as IItemsRepeaterPresenter);
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
        /// Creates the container which will display an item in the control.
        /// </summary>
        /// <param name="data">The item.</param>
        /// <returns>The container control.</returns>
        /// <remarks>
        /// By default this method creates a <see cref="ContentPresenter"/> for non-control items
        /// and returns the control itself for control items.
        /// 
        /// This method can be overridden to create a specific container type for items. The easiest
        /// way to do that is to call <see cref="DefaultCreateContainer{T}(object)"/> with
        /// the desired container type.
        /// </remarks>
        protected virtual IControl CreateContainer(object data)
        {
            if (data is IControl control)
            {
                return control;
            }
            else
            {
                var result = new ContentPresenter();
                result.Bind(
                    ContentPresenter.ContentProperty,
                    result.GetObservable(DataContextProperty));
                return result;
            }
        }

        /// <summary>
        /// Creates a <see cref="ContentControl"/>-derived container for an item.
        /// </summary>
        /// <typeparam name="T">The type of the container control to create.</typeparam>
        /// <param name="data">The item.</param>
        /// <returns>The created container control.</returns>
        protected T DefaultCreateContainer<T>(object data)
            where T : ContentControl, new()
        {
            if (data is T t)
            {
                return t;
            }
            else
            {
                var result = new T();
                result.Bind(
                    ContentPresenter.ContentProperty,
                    result.GetObservable(ContentPresenter.DataContextProperty),
                    BindingPriority.TemplatedParent);
                result.SetValue(
                    ContentPresenter.ContentTemplateProperty,
                    ItemTemplate,
                    BindingPriority.TemplatedParent);
                return result;
            }
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
        /// Raises the <see cref="ContainerPrepared"/> event.
        /// </summary>
        /// <param name="e">The event args.</param>
        /// <remarks>
        /// If you override this method, be sure to call the base method implementation.
        /// </remarks>
        protected virtual void OnContainerPrepared(ElementPreparedEventArgs e)
        {
            ContainerPrepared?.Invoke(this, e);
        }

        /// <summary>
        /// Raises the <see cref="ContainerClearing"/> event.
        /// </summary>
        /// <param name="e">The event args.</param>
        /// <remarks>
        /// If you override this method, be sure to call the base method implementation.
        /// </remarks>
        protected virtual void OnContainerClearing(ElementClearingEventArgs e)
        {
            ContainerClearing?.Invoke(this, e);
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
        }

        /// <summary>
        /// Handles directional navigation within the <see cref="ItemsControl"/>.
        /// </summary>
        /// <param name="e">The key events.</param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (!e.Handled)
            {
                var focus = FocusManager.Instance;
                var direction = e.Key.ToNavigationDirection();
                var container = Presenter?.Panel as INavigableContainer;

                if (container == null ||
                    focus.Current == null ||
                    direction == null ||
                    direction.Value.IsTab())
                {
                    return;
                }

                IVisual current = focus.Current;

                while (current != null)
                {
                    if (current.VisualParent == container && current is IInputElement inputElement)
                    {
                        IInputElement next = GetNextControl(container, direction.Value, inputElement, false);

                        if (next != null)
                        {
                            focus.Focus(next, NavigationMethod.Directional);
                            e.Handled = true;
                        }

                        break;
                    }

                    current = current.VisualParent;
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

            if (Presenter != null)
            {
                Presenter.Items = newValue;
            }

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
            UpdateItemCount();

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    AddControlItemsToLogicalChildren(e.NewItems);
                    break;

                case NotifyCollectionChangedAction.Remove:
                    RemoveControlItemsFromLogicalChildren(e.OldItems);
                    break;
            }

            Presenter?.ItemsChanged(e);

            var collection = sender as ICollection;
            PseudoClasses.Set(":empty", collection == null || collection.Count == 0);
            PseudoClasses.Set(":singleitem", collection != null && collection.Count == 1);
        }

        protected virtual void ItemsPresenterChanged(IItemsRepeaterPresenter oldValue, IItemsRepeaterPresenter newValue)
        {
            if (oldValue != null)
            {
                oldValue.LogicalChildren.CollectionChanged -= PresenterLogicalChildrenChanged;
                oldValue.ElementPrepared -= PresenterElementPrepared;
                oldValue.ElementClearing -= PresenterElementClearing;
                LogicalChildren.Clear();
            }

            if (newValue != null)
            {
                newValue.LogicalChildren.CollectionChanged += PresenterLogicalChildrenChanged;
                newValue.ElementPrepared += PresenterElementPrepared;
                newValue.ElementClearing += PresenterElementClearing;
                LogicalChildren.AddRange(newValue.LogicalChildren);
            }

            ItemContainerGenerator.Clear();
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

        private void PresenterLogicalChildrenChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            void Add(IList items)
            {
                foreach (ILogical l in items)
                {
                    LogicalChildren.Add(l);
                }
            }

            void Remove(IList items)
            {
                foreach (ILogical l in items)
                {
                    LogicalChildren.Remove(l);
                }
            }

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    Add(e.NewItems);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    Remove(e.OldItems);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    Remove(e.OldItems);
                    Add(e.NewItems);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    LogicalChildren.Clear();
                    LogicalChildren.AddRange(Presenter.LogicalChildren);
                    break;
            }
        }

        private void PresenterElementPrepared(object sender, ElementPreparedEventArgs e)
        {
            OnContainerPrepared(e);
        }

        private void PresenterElementClearing(object sender, ElementClearingEventArgs e)
        {
            OnContainerClearing(e);
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
            var c = from;

            do
            {
                result = container.GetControl(direction, c, wrap);
                from = from ?? result;

                if (result?.Focusable == true)
                {
                    return result;
                }

                c = result;
            } while (c != null && c != from);

            return null;
        }
    }
}
