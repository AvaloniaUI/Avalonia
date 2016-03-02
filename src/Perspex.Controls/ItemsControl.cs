// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Perspex.Collections;
using Perspex.Controls.Generators;
using Perspex.Controls.Presenters;
using Perspex.Controls.Primitives;
using Perspex.Controls.Templates;
using Perspex.Controls.Utils;
using Perspex.LogicalTree;
using Perspex.Metadata;

namespace Perspex.Controls
{
    /// <summary>
    /// Displays a collection of items.
    /// </summary>
    public class ItemsControl : TemplatedControl, IItemsPresenterHost
    {
        /// <summary>
        /// The default value for the <see cref="ItemsPanel"/> property.
        /// </summary>
        [SuppressMessage("Microsoft.StyleCop.CSharp.NamingRules", "SA1202:ElementsMustBeOrderedByAccess", Justification = "Needs to be before or a NullReferenceException is thrown.")]
        private static readonly FuncTemplate<IPanel> DefaultPanel =
            new FuncTemplate<IPanel>(() => new StackPanel());

        /// <summary>
        /// Defines the <see cref="Items"/> property.
        /// </summary>
        public static readonly DirectProperty<ItemsControl, IEnumerable> ItemsProperty =
            PerspexProperty.RegisterDirect<ItemsControl, IEnumerable>(nameof(Items), o => o.Items, (o, v) => o.Items = v);

        /// <summary>
        /// Defines the <see cref="ItemsPanel"/> property.
        /// </summary>
        public static readonly StyledProperty<ITemplate<IPanel>> ItemsPanelProperty =
            PerspexProperty.Register<ItemsControl, ITemplate<IPanel>>(nameof(ItemsPanel), DefaultPanel);

        /// <summary>
        /// Defines the <see cref="MemberSelector"/> property.
        /// </summary>
        public static readonly StyledProperty<IMemberSelector> MemberSelectorProperty =
            PerspexProperty.Register<ItemsControl, IMemberSelector>(nameof(MemberSelector));

        private IEnumerable _items = new PerspexList<object>();
        private IItemContainerGenerator _itemContainerGenerator;

        /// <summary>
        /// Initializes static members of the <see cref="ItemsControl"/> class.
        /// </summary>
        static ItemsControl()
        {
            ItemsProperty.Changed.AddClassHandler<ItemsControl>(x => x.ItemsChanged);
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
                        _itemContainerGenerator.Materialized += (_, e) => OnContainersMaterialized(e);
                        _itemContainerGenerator.Dematerialized += (_, e) => OnContainersDematerialized(e);
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
        /// Gets or sets the panel used to display the items.
        /// </summary>
        public ITemplate<IPanel> ItemsPanel
        {
            get { return GetValue(ItemsPanelProperty); }
            set { SetValue(ItemsPanelProperty, value); }
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
        }

        /// <summary>
        /// Gets the item at the specified index in a collection.
        /// </summary>
        /// <param name="items">The collection.</param>
        /// <param name="index">The index.</param>
        /// <returns>The index of the item or -1 if the item was not found.</returns>
        protected static object ElementAt(IEnumerable items, int index)
        {
            var typedItems = items?.Cast<object>();

            if (index != -1 && typedItems != null && index < typedItems.Count())
            {
                return typedItems.ElementAt(index) ?? null;
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
            var toAdd = new List<ILogical>();

            foreach (var container in e.Containers)
            {
                // If the item is its own container, then it will be added to the logical tree when
                // it was added to the Items collection.
                if (container.ContainerControl != null && container.ContainerControl != container.Item)
                {
                    toAdd.Add(container.ContainerControl);
                }
            }

            LogicalChildren.AddRange(toAdd);
        }

        /// <summary>
        /// Called when containers are dematerialized for the <see cref="ItemsControl"/> by its
        /// <see cref="ItemContainerGenerator"/>.
        /// </summary>
        /// <param name="e">The details of the containers.</param>
        protected virtual void OnContainersDematerialized(ItemContainerEventArgs e)
        {
            var toRemove = new List<ILogical>();

            foreach (var container in e.Containers)
            {
                // If the item is its own container, then it will be removed from the logical tree
                // when it is removed from the Items collection.
                if (container.ContainerControl != container.Item)
                {
                    toRemove.Add(container.ContainerControl);
                }
            }

            LogicalChildren.RemoveAll(toRemove);
        }

        /// <inheritdoc/>
        protected override void OnTemplateChanged(PerspexPropertyChangedEventArgs e)
        {
            base.OnTemplateChanged(e);

            if (e.NewValue == null)
            {
                ItemContainerGenerator?.Clear();
            }
        }

        /// <summary>
        /// Caled when the <see cref="Items"/> property changes.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected virtual void ItemsChanged(PerspexPropertyChangedEventArgs e)
        {
            var incc = e.OldValue as INotifyCollectionChanged;

            if (incc != null)
            {
                incc.CollectionChanged -= ItemsCollectionChanged;
            }

            var oldValue = e.OldValue as IEnumerable;
            var newValue = e.NewValue as IEnumerable;

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

            var collection = sender as ICollection;
            PseudoClasses.Set(":empty", collection.Count == 0);
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
        /// <param name="items"></param>
        private void SubscribeToItems(IEnumerable items)
        {
            PseudoClasses.Set(":empty", items == null || items.Count() == 0);

            var incc = items as INotifyCollectionChanged;

            if (incc != null)
            {
                incc.CollectionChanged += ItemsCollectionChanged;
            }
        }
    }
}
