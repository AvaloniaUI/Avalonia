// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
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
            ItemsControl.ItemsProperty.AddOwner<ItemsPresenter>(o => o.Items, (o, v) => o.Items = v);

        /// <summary>
        /// Defines the <see cref="ItemsPanel"/> property.
        /// </summary>
        public static readonly PerspexProperty<ITemplate<IPanel>> ItemsPanelProperty =
            ItemsControl.ItemsPanelProperty.AddOwner<ItemsPresenter>();

        /// <summary>
        /// Defines the <see cref="MemberSelector"/> property.
        /// </summary>
        public static readonly PerspexProperty<IMemberSelector> MemberSelectorProperty =
            ItemsControl.MemberSelectorProperty.AddOwner<ItemsPresenter>();

        private IEnumerable _items;
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
            TemplatedParentProperty.Changed.AddClassHandler<ItemsPresenter>(x => x.TemplatedParentChanged);
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
            get { return _items; }
            set { SetAndRaise(ItemsProperty, ref _items, value); }
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
        /// Selects a member from <see cref="Items"/> to use as the list item.
        /// </summary>
        public IMemberSelector MemberSelector
        {
            get { return GetValue(MemberSelectorProperty); }
            set { SetValue(MemberSelectorProperty, value); }
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
            Panel = ItemsPanel.Build();
            Panel.SetValue(TemplatedParentProperty, TemplatedParent);

            if (!Panel.IsSet(KeyboardNavigation.DirectionalNavigationProperty))
            {
                KeyboardNavigation.SetDirectionalNavigation(
                    (InputElement)Panel,
                    KeyboardNavigationMode.Contained);
            }

            LogicalChildren.Clear();
            VisualChildren.Clear();
            LogicalChildren.Add(Panel);
            VisualChildren.Add(Panel);

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
                AddContainers(ItemContainerGenerator.Materialize(0, Items, MemberSelector));

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
                    generator.Clear();
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
                        AddContainers(generator.Materialize(0, Items, MemberSelector));
                        break;
                }

                InvalidateMeasure();
            }
        }

        private void TemplatedParentChanged(PerspexPropertyChangedEventArgs e)
        {
            (e.NewValue as IItemsPresenterHost)?.RegisterItemsPresenter(this);
        }

        private void AddContainers(IEnumerable<ItemContainer> items)
        {
            foreach (var i in items)
            {
                if (i.ContainerControl != null)
                {
                    this.Panel.Children.Add(i.ContainerControl);
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
