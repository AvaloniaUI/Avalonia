// -----------------------------------------------------------------------
// <copyright file="ItemsPresenter.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls.Presenters
{
    using System;
    using System.Collections;
    using System.Collections.Specialized;
    using Perspex.Controls.Generators;
    using Perspex.Controls.Templates;
    using Perspex.Input;
    using Perspex.Styling;

    /// <summary>
    /// Displays items inside an <see cref="ItemsControl"/>.
    /// </summary>
    public class ItemsPresenter : Control, IItemsPresenter, ITemplatedControl
    {
        /// <summary>
        /// Defines the <see cref="Items"/> property.
        /// </summary>
        public static readonly PerspexProperty<IEnumerable> ItemsProperty =
            ItemsControl.ItemsProperty.AddOwner<ItemsPresenter>();

        /// <summary>
        /// Defines the <see cref="ItemsPanel"/> property.
        /// </summary>
        public static readonly PerspexProperty<ITemplate<Panel>> ItemsPanelProperty =
            ItemsControl.ItemsPanelProperty.AddOwner<ItemsPresenter>();

        private bool createdPanel;

        private IItemContainerGenerator generator;

        /// <summary>
        /// Initializes static members of the <see cref="ItemsPresenter"/> class.
        /// </summary>
        static ItemsPresenter()
        {
            KeyboardNavigation.TabNavigationProperty.OverrideDefaultValue(
                typeof(ItemsPresenter),
                KeyboardNavigationMode.Once);
            ItemsProperty.Changed.AddClassHandler<ItemsPresenter>(x => x.ItemsChanged);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemsPresenter"/> class.
        /// </summary>
        public ItemsPresenter()
        {
        }

        /// <summary>
        /// Gets the <see cref="IItemContainerGenerator"/> used to generate item container
        /// controls.
        /// </summary>
        public IItemContainerGenerator ItemContainerGenerator
        {
            get
            {
                if (this.generator == null)
                {
                    var i = this.TemplatedParent as ItemsControl;
                    this.generator = i?.ItemContainerGenerator ?? new ItemContainerGenerator(this);
                }

                return this.generator;
            }

            set
            {
                if (this.generator != null)
                {
                    throw new InvalidOperationException("ItemContainerGenerator is already set.");
                }

                this.generator = value;
            }
        }

        /// <summary>
        /// Gets or sets the items to be displayed.
        /// </summary>
        public IEnumerable Items
        {
            get { return this.GetValue(ItemsProperty); }
            set { this.SetValue(ItemsProperty, value); }
        }

        /// <summary>
        /// Gets or sets a template which creates the <see cref="Panel"/> used to display the items.
        /// </summary>
        public ITemplate<Panel> ItemsPanel
        {
            get { return this.GetValue(ItemsPanelProperty); }
            set { this.SetValue(ItemsPanelProperty, value); }
        }

        /// <summary>
        /// Gets the panel used to display the items.
        /// </summary>
        public Panel Panel
        {
            get;
            private set;
        }

        /// <inheritdoc/>
        public override sealed void ApplyTemplate()
        {
            if (!this.createdPanel)
            {
                this.CreatePanel();
            }
        }

        /// <inheritdoc/>
        protected override Size MeasureOverride(Size availableSize)
        {
            this.Panel.Measure(availableSize);
            return this.Panel.DesiredSize;
        }

        /// <inheritdoc/>
        protected override Size ArrangeOverride(Size finalSize)
        {
            this.Panel.Arrange(new Rect(finalSize));
            return finalSize;
        }

        /// <summary>
        /// Creates the <see cref="Panel"/> when <see cref="ApplyTemplate"/> is called for the first
        /// time.
        /// </summary>
        private void CreatePanel()
        {
            this.ClearVisualChildren();
            this.Panel = this.ItemsPanel.Build();
            this.Panel.SetValue(TemplatedParentProperty, this.TemplatedParent);

            if (!this.Panel.IsSet(KeyboardNavigation.DirectionalNavigationProperty))
            {
                KeyboardNavigation.SetDirectionalNavigation(this.Panel, KeyboardNavigationMode.Contained);
            }

            this.AddVisualChild(this.Panel);

            var logicalHost = this.FindReparentingHost();

            if (logicalHost != null)
            {
                ((IReparentingControl)this.Panel).ReparentLogicalChildren(
                    logicalHost,
                    logicalHost.LogicalChildren);
            }

            KeyboardNavigation.SetTabNavigation(this.Panel, KeyboardNavigation.GetTabNavigation(this));
            this.createdPanel = true;
            this.CreateItemsAndListenForChanges(this.Items);
        }

        /// <summary>
        /// Creates the items for a collection and starts listening for changes on the collection.
        /// </summary>
        /// <param name="items">The items, may be null.</param>
        private void CreateItemsAndListenForChanges(IEnumerable items)
        {
            if (items != null)
            {
                this.Panel.Children.AddRange(
                    this.ItemContainerGenerator.CreateContainers(0, this.Items, null));

                INotifyCollectionChanged incc = items as INotifyCollectionChanged;

                if (incc != null)
                {
                    incc.CollectionChanged += this.ItemsCollectionChanged;
                }
            }
        }

        /// <summary>
        /// Called when the <see cref="Items"/> property changes.
        /// </summary>
        /// <param name="e">The event args.</param>
        private void ItemsChanged(PerspexPropertyChangedEventArgs e)
        {
            if (this.createdPanel)
            {
                var generator = this.ItemContainerGenerator;

                if (e.OldValue != null)
                {
                    generator.ClearContainers();
                    this.Panel.Children.Clear();

                    INotifyCollectionChanged incc = e.OldValue as INotifyCollectionChanged;

                    if (incc != null)
                    {
                        incc.CollectionChanged -= this.ItemsCollectionChanged;
                    }
                }

                if (this.Panel != null)
                {
                    this.CreateItemsAndListenForChanges((IEnumerable)e.NewValue);
                }
            }
        }

        /// <summary>
        /// Called when the <see cref="Items"/> collection changes.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event args.</param>
        private void ItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (this.createdPanel)
            {
                var generator = this.ItemContainerGenerator;

                // TODO: Handle Move and Replace etc.
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        this.Panel.Children.AddRange(
                            generator.CreateContainers(e.NewStartingIndex, e.NewItems, null));
                        break;

                    case NotifyCollectionChangedAction.Remove:
                        this.Panel.Children.RemoveAll(
                            generator.RemoveContainers(e.OldStartingIndex, e.OldItems));
                        break;
                }

                this.InvalidateMeasure();
            }
        }
    }
}
