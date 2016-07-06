// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Specialized;
using Avalonia.Controls.Generators;
using Avalonia.Controls.Templates;
using Avalonia.Styling;

namespace Avalonia.Controls.Presenters
{
    /// <summary>
    /// Base class for controls that present items inside an <see cref="ItemsControl"/>.
    /// </summary>
    public abstract class ItemsPresenterBase : Control, IItemsPresenter, ITemplatedControl
    {
        /// <summary>
        /// Defines the <see cref="Items"/> property.
        /// </summary>
        public static readonly DirectProperty<ItemsPresenterBase, IEnumerable> ItemsProperty =
            ItemsControl.ItemsProperty.AddOwner<ItemsPresenterBase>(o => o.Items, (o, v) => o.Items = v);

        /// <summary>
        /// Defines the <see cref="ItemsPanel"/> property.
        /// </summary>
        public static readonly StyledProperty<ITemplate<IPanel>> ItemsPanelProperty =
            ItemsControl.ItemsPanelProperty.AddOwner<ItemsPresenterBase>();

        /// <summary>
        /// Defines the <see cref="ItemTemplate"/> property.
        /// </summary>
        public static readonly StyledProperty<IDataTemplate> ItemTemplateProperty =
            ItemsControl.ItemTemplateProperty.AddOwner<ItemsPresenterBase>();

        /// <summary>
        /// Defines the <see cref="MemberSelector"/> property.
        /// </summary>
        public static readonly StyledProperty<IMemberSelector> MemberSelectorProperty =
            ItemsControl.MemberSelectorProperty.AddOwner<ItemsPresenterBase>();

        private IEnumerable _items;
        private bool _createdPanel;
        private IItemContainerGenerator _generator;

        /// <summary>
        /// Initializes static members of the <see cref="ItemsPresenter"/> class.
        /// </summary>
        static ItemsPresenterBase()
        {
            TemplatedParentProperty.Changed.AddClassHandler<ItemsPresenterBase>(x => x.TemplatedParentChanged);
        }

        /// <summary>
        /// Gets or sets the items to be displayed.
        /// </summary>
        public IEnumerable Items
        {
            get
            {
                return _items;
            }

            set
            {
                if (_createdPanel)
                {
                    INotifyCollectionChanged incc = _items as INotifyCollectionChanged;

                    if (incc != null)
                    {
                        incc.CollectionChanged -= ItemsCollectionChanged;
                    }
                }

                if (_createdPanel && value != null)
                {
                    INotifyCollectionChanged incc = value as INotifyCollectionChanged;

                    if (incc != null)
                    {
                        incc.CollectionChanged += ItemsCollectionChanged;
                    }
                }

                SetAndRaise(ItemsProperty, ref _items, value);

                if (_createdPanel)
                {
                    ItemsChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                }
            }
        }

        /// <summary>
        /// Gets the item container generator.
        /// </summary>
        public IItemContainerGenerator ItemContainerGenerator
        {
            get
            {
                if (_generator == null)
                {
                    _generator = CreateItemContainerGenerator();
                }

                return _generator;
            }

            internal set
            {
                if (_generator != null)
                {
                    throw new InvalidOperationException("ItemContainerGenerator already created.");
                }

                _generator = value;
            }
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
        public virtual void ScrollIntoView(object item)
        {
        }

        /// <summary>
        /// Creates the <see cref="ItemContainerGenerator"/> for the control.
        /// </summary>
        /// <returns>
        /// An <see cref="IItemContainerGenerator"/> or null.
        /// </returns>
        protected virtual IItemContainerGenerator CreateItemContainerGenerator()
        {
            var i = TemplatedParent as ItemsControl;
            var result = i?.ItemContainerGenerator;

            if (result == null)
            {
                result = new ItemContainerGenerator(this);
                result.ItemTemplate = ItemTemplate;
            }

            return result;
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
        /// Called when the <see cref="Panel"/> is created.
        /// </summary>
        /// <param name="panel">The panel.</param>
        protected virtual void PanelCreated(IPanel panel)
        {
        }

        /// <summary>
        /// Called when the items for the presenter change, either because <see cref="Items"/>
        /// has been set, the items collection has been modified, or the panel has been created.
        /// </summary>
        /// <param name="e">A description of the change.</param>
        protected abstract void ItemsChanged(NotifyCollectionChangedEventArgs e);

        /// <summary>
        /// Creates the <see cref="Panel"/> when <see cref="ApplyTemplate"/> is called for the first
        /// time.
        /// </summary>
        private void CreatePanel()
        {
            Panel = ItemsPanel.Build();
            Panel.SetValue(TemplatedParentProperty, TemplatedParent);

            LogicalChildren.Clear();
            VisualChildren.Clear();
            LogicalChildren.Add(Panel);
            VisualChildren.Add(Panel);

            _createdPanel = true;

            INotifyCollectionChanged incc = Items as INotifyCollectionChanged;

            if (incc != null)
            {
                incc.CollectionChanged += ItemsCollectionChanged;
            }

            PanelCreated(Panel);

            ItemsChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
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
                ItemsChanged(e);
            }
        }

        private void TemplatedParentChanged(AvaloniaPropertyChangedEventArgs e)
        {
            (e.NewValue as IItemsPresenterHost)?.RegisterItemsPresenter(this);
        }
    }
}