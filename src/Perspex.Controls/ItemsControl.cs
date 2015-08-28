// -----------------------------------------------------------------------
// <copyright file="ItemsControl.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
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

    /// <summary>
    /// Displays a collection of items.
    /// </summary>
    public class ItemsControl : TemplatedControl, IReparentingHost
    {
        /// <summary>
        /// The default value for the <see cref="ItemsPanel"/> property.
        /// </summary>
        [SuppressMessage("Microsoft.StyleCop.CSharp.NamingRules", "SA1202:ElementsMustBeOrderedByAccess", Justification = "Needs to be before or a NullReferenceException is thrown.")]
        private static readonly ItemsPanelTemplate DefaultPanel =
            new ItemsPanelTemplate(() => new StackPanel());

        /// <summary>
        /// Defines the <see cref="Items"/> property.
        /// </summary>
        public static readonly PerspexProperty<IEnumerable> ItemsProperty =
            PerspexProperty.Register<ItemsControl, IEnumerable>("Items");

        /// <summary>
        /// Defines the <see cref="ItemsPanel"/> property.
        /// </summary>
        public static readonly PerspexProperty<ItemsPanelTemplate> ItemsPanelProperty =
            PerspexProperty.Register<ItemsControl, ItemsPanelTemplate>("ItemsPanel", defaultValue: DefaultPanel);

        private IItemContainerGenerator itemContainerGenerator;

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
            this.Classes.Add(":empty");
            this.Items = new PerspexList<object>();
        }

        /// <summary>
        /// Gets the <see cref="IItemContainerGenerator"/> for the control.
        /// </summary>
        public IItemContainerGenerator ItemContainerGenerator
        {
            get
            {
                if (this.itemContainerGenerator == null)
                {
                    this.itemContainerGenerator = this.CreateItemContainerGenerator();
                }

                return this.itemContainerGenerator;
            }
        }

        /// <summary>
        /// Gets or sets the items to display.
        /// </summary>
        public IEnumerable Items
        {
            get { return this.GetValue(ItemsProperty); }
            set { this.SetValue(ItemsProperty, value); }
        }

        /// <summary>
        /// Gets or sets the panel used to display the items.
        /// </summary>
        public ItemsPanelTemplate ItemsPanel
        {
            get { return this.GetValue(ItemsPanelProperty); }
            set { this.SetValue(ItemsPanelProperty, value); }
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
        IPerspexList<ILogical> IReparentingHost.LogicalChildren
        {
            get { return this.LogicalChildren; }
        }

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
            this.Presenter = this.FindTemplateChild<IItemsPresenter>("itemsPresenter");
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
                incc.CollectionChanged += this.ItemsCollectionChanged;
            }

            var newValue = e.NewValue as IEnumerable;

            if (newValue == null || newValue.Count() == 0)
            {
                this.Classes.Add(":empty");
            }
            else
            {
                this.Classes.Remove(":empty");
            }

            incc = newValue as INotifyCollectionChanged;

            if (incc != null)
            {
                incc.CollectionChanged += this.ItemsCollectionChanged;
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
                this.Classes.Add(":empty");
            }
            else
            {
                this.Classes.Remove(":empty");
            }
        }
    }
}
