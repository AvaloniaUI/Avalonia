// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Perspex.Collections;
using Perspex.Controls.Generators;
using Perspex.Controls.Presenters;
using Perspex.Controls.Primitives;
using Perspex.Controls.Templates;
using Perspex.Controls.Utils;
using Perspex.Styling;

namespace Perspex.Controls
{
    /// <summary>
    /// Displays a collection of items.
    /// </summary>
    public class ItemsControl : TemplatedControl, IReparentingHost
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
        public static readonly PerspexProperty<IEnumerable> ItemsProperty =
            PerspexProperty.RegisterDirect<ItemsControl, IEnumerable>(nameof(Items), o => o.Items, (o, v) => o.Items = v);

        /// <summary>
        /// Defines the <see cref="ItemsPanel"/> property.
        /// </summary>
        public static readonly PerspexProperty<ITemplate<IPanel>> ItemsPanelProperty =
            PerspexProperty.Register<ItemsControl, ITemplate<IPanel>>(nameof(ItemsPanel), DefaultPanel);

        /// <summary>
        /// Defines the <see cref="MemberSelector"/> property.
        /// </summary>
        public static readonly PerspexProperty<IMemberSelector> MemberSelectorProperty =
            PerspexProperty.Register<ItemsControl, IMemberSelector>(nameof(MemberSelector));

        private IEnumerable _items;
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
            Classes.Add(":empty");
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
                }

                return _itemContainerGenerator;
            }
        }

        /// <summary>
        /// Gets or sets the items to display.
        /// </summary>
        public IEnumerable Items
        {
            get
            {
                if (_items == null)
                {
                    _items = new PerspexList<object>();
                }

                return _items;
            }

            set
            {
                SetAndRaise(ItemsProperty, ref _items, value);
            }
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
            set;
        }

        /// <inheritdoc/>
        IPerspexList<ILogical> IReparentingHost.LogicalChildren => LogicalChildren;

        /// <summary>
        /// Asks the control whether it wants to reparent the logical children of the specified
        /// control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <returns>
        /// True if the control wants to reparent its logical children otherwise false.
        /// </returns>
        bool IReparentingHost.WillReparentChildrenOf(IControl control)
        {
            return control is IItemsPresenter && control.TemplatedParent == this;
        }

        /// <summary>
        /// Creates the <see cref="ItemContainerGenerator"/> for the control.
        /// </summary>
        /// <returns>An <see cref="IItemContainerGenerator"/>.</returns>
        protected virtual IItemContainerGenerator CreateItemContainerGenerator()
        {
            return new ItemContainerGenerator(this);
        }

        /// <inheritdoc/>
        protected override void OnTemplateApplied()
        {
            Presenter = this.FindTemplateChild<IItemsPresenter>("itemsPresenter");
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

            var newValue = e.NewValue as IEnumerable;

            if (newValue == null || newValue.Count() == 0)
            {
                Classes.Add(":empty");
            }
            else
            {
                Classes.Remove(":empty");
            }

            incc = newValue as INotifyCollectionChanged;

            if (incc != null)
            {
                incc.CollectionChanged += ItemsCollectionChanged;
            }
        }

        /// <summary>
        /// Called when the <see cref="INotifyCollectionChanged.CollectionChanged"/> event is
        /// raised on <see cref="Items"/>.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        protected virtual void ItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var collection = sender as ICollection;

            if (collection.Count == 0)
            {
                Classes.Add(":empty");
            }
            else
            {
                Classes.Remove(":empty");
            }
        }
    }
}
