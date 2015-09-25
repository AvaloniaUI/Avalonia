// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Specialized;
using Perspex.Controls.Generators;
using Perspex.Controls.Templates;
using Perspex.Input;
using Perspex.Styling;

namespace Perspex.Controls.Presenters
{
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
        public static readonly PerspexProperty<ITemplate<IPanel>> ItemsPanelProperty =
            ItemsControl.ItemsPanelProperty.AddOwner<ItemsPresenter>();

        private bool _createdPanel;

        private IItemContainerGenerator _generator;

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
                if (_generator == null)
                {
                    var i = TemplatedParent as ItemsControl;
                    _generator = i?.ItemContainerGenerator ?? new ItemContainerGenerator(this);
                }

                return _generator;
            }

            set
            {
                if (_generator != null)
                {
                    throw new InvalidOperationException("ItemContainerGenerator is already set.");
                }

                _generator = value;
            }
        }

        /// <summary>
        /// Gets or sets the items to be displayed.
        /// </summary>
        public IEnumerable Items
        {
            get { return GetValue(ItemsProperty); }
            set { SetValue(ItemsProperty, value); }
        }

        /// <summary>
        /// Gets or sets a template which creates the <see cref="Panel"/> used to display the items.
        /// </summary>
        public ITemplate<IPanel> ItemsPanel
        {
            get { return GetValue(ItemsPanelProperty); }
            set { SetValue(ItemsPanelProperty, value); }
        }

        /// <summary>
        /// Gets the panel used to display the items.
        /// </summary>
        public IPanel Panel
        {
            get;
            private set;
        }

        /// <inheritdoc/>
        public override sealed void ApplyTemplate()
        {
            if (!_createdPanel)
            {
                CreatePanel();
            }
        }

        /// <inheritdoc/>
        protected override Size MeasureOverride(Size availableSize)
        {
            Panel.Measure(availableSize);
            return Panel.DesiredSize;
        }

        /// <inheritdoc/>
        protected override Size ArrangeOverride(Size finalSize)
        {
            Panel.Arrange(new Rect(finalSize));
            return finalSize;
        }

        /// <summary>
        /// Creates the <see cref="Panel"/> when <see cref="ApplyTemplate"/> is called for the first
        /// time.
        /// </summary>
        private void CreatePanel()
        {
            ClearVisualChildren();
            Panel = ItemsPanel.Build();
            Panel.SetValue(TemplatedParentProperty, TemplatedParent);

            if (!Panel.IsSet(KeyboardNavigation.DirectionalNavigationProperty))
            {
                KeyboardNavigation.SetDirectionalNavigation(
                    (InputElement)Panel,
                    KeyboardNavigationMode.Contained);
            }

            AddVisualChild(Panel);

            var logicalHost = this.FindReparentingHost();

            if (logicalHost != null)
            {
                ((IReparentingControl)Panel).ReparentLogicalChildren(
                    logicalHost,
                    logicalHost.LogicalChildren);
            }

            KeyboardNavigation.SetTabNavigation(
                (InputElement)Panel,
                KeyboardNavigation.GetTabNavigation(this));
            _createdPanel = true;
            CreateItemsAndListenForChanges(Items);
        }

        /// <summary>
        /// Creates the items for a collection and starts listening for changes on the collection.
        /// </summary>
        /// <param name="items">The items, may be null.</param>
        private void CreateItemsAndListenForChanges(IEnumerable items)
        {
            if (items != null)
            {
                Panel.Children.AddRange(
                    ItemContainerGenerator.CreateContainers(0, Items));

                INotifyCollectionChanged incc = items as INotifyCollectionChanged;

                if (incc != null)
                {
                    incc.CollectionChanged += ItemsCollectionChanged;
                }
            }
        }

        /// <summary>
        /// Called when the <see cref="Items"/> property changes.
        /// </summary>
        /// <param name="e">The event args.</param>
        private void ItemsChanged(PerspexPropertyChangedEventArgs e)
        {
            if (_createdPanel)
            {
                var generator = ItemContainerGenerator;

                if (e.OldValue != null)
                {
                    generator.ClearContainers();
                    Panel.Children.Clear();

                    INotifyCollectionChanged incc = e.OldValue as INotifyCollectionChanged;

                    if (incc != null)
                    {
                        incc.CollectionChanged -= ItemsCollectionChanged;
                    }
                }

                if (Panel != null)
                {
                    CreateItemsAndListenForChanges((IEnumerable)e.NewValue);
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
            if (_createdPanel)
            {
                var generator = ItemContainerGenerator;

                // TODO: Handle Move and Replace etc.
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        Panel.Children.AddRange(
                            generator.CreateContainers(e.NewStartingIndex, e.NewItems));
                        break;

                    case NotifyCollectionChangedAction.Remove:
                        Panel.Children.RemoveAll(
                            generator.RemoveContainers(e.OldStartingIndex, e.OldItems));
                        break;
                }

                InvalidateMeasure();
            }
        }
    }
}
