using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia.Automation.Peers;
using Avalonia.Collections;
using Avalonia.Controls.Generators;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Metadata;
using Avalonia.Styling;

namespace Avalonia.Controls
{
    /// <summary>
    /// Displays a collection of items.
    /// </summary>
    [PseudoClasses(":empty", ":singleitem")]
    public class ItemsControl : TemplatedControl, IChildIndexProvider, IScrollSnapPointsInfo
    {
        /// <summary>
        /// The default value for the <see cref="ItemsPanel"/> property.
        /// </summary>
        private static readonly FuncTemplate<Panel> DefaultPanel =
            new FuncTemplate<Panel>(() => new StackPanel());

        /// <summary>
        /// Defines the <see cref="Items"/> property.
        /// </summary>
        public static readonly DirectProperty<ItemsControl, IEnumerable?> ItemsProperty =
            AvaloniaProperty.RegisterDirect<ItemsControl, IEnumerable?>(nameof(Items), o => o.Items, (o, v) => o.Items = v);

        /// <summary>
        /// Defines the <see cref="ItemContainerTheme"/> property.
        /// </summary>
        public static readonly StyledProperty<ControlTheme?> ItemContainerThemeProperty =
            AvaloniaProperty.Register<ItemsControl, ControlTheme?>(nameof(ItemContainerTheme));

        /// <summary>
        /// Defines the <see cref="ItemCount"/> property.
        /// </summary>
        public static readonly DirectProperty<ItemsControl, int> ItemCountProperty =
            AvaloniaProperty.RegisterDirect<ItemsControl, int>(nameof(ItemCount), o => o.ItemCount);

        /// <summary>
        /// Defines the <see cref="ItemsPanel"/> property.
        /// </summary>
        public static readonly StyledProperty<ITemplate<Panel>> ItemsPanelProperty =
            AvaloniaProperty.Register<ItemsControl, ITemplate<Panel>>(nameof(ItemsPanel), DefaultPanel);

        /// <summary>
        /// Defines the <see cref="ItemTemplate"/> property.
        /// </summary>
        public static readonly StyledProperty<IDataTemplate?> ItemTemplateProperty =
            AvaloniaProperty.Register<ItemsControl, IDataTemplate?>(nameof(ItemTemplate));

        /// <summary>
        /// Defines the <see cref="ItemsView"/> property.
        /// </summary>
        public static readonly DirectProperty<ItemsControl, ItemsSourceView> ItemsViewProperty =
            AvaloniaProperty.RegisterDirect<ItemsControl, ItemsSourceView>(nameof(ItemsView), o => o.ItemsView);

        /// <summary>
        /// Defines the <see cref="DisplayMemberBinding" /> property
        /// </summary>
        public static readonly StyledProperty<IBinding?> DisplayMemberBindingProperty =
            AvaloniaProperty.Register<ItemsControl, IBinding?>(nameof(DisplayMemberBinding));
        
        /// <summary>
        /// Defines the <see cref="AreHorizontalSnapPointsRegular"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> AreHorizontalSnapPointsRegularProperty =
            AvaloniaProperty.Register<ItemsControl, bool>(nameof(AreHorizontalSnapPointsRegular));

        /// <summary>
        /// Defines the <see cref="AreVerticalSnapPointsRegular"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> AreVerticalSnapPointsRegularProperty =
            AvaloniaProperty.Register<ItemsControl, bool>(nameof(AreVerticalSnapPointsRegular));

        /// <summary>
        /// Gets or sets the <see cref="IBinding"/> to use for binding to the display member of each item.
        /// </summary>
        [AssignBinding]
        [InheritDataTypeFromItems(nameof(Items))]
        public IBinding? DisplayMemberBinding
        {
            get => GetValue(DisplayMemberBindingProperty);
            set => SetValue(DisplayMemberBindingProperty, value);
        }
        
        private IEnumerable? _items = new AvaloniaList<object>();
        private ItemsSourceView _itemsView;
        private int _itemCount;
        private ItemContainerGenerator? _itemContainerGenerator;
        private EventHandler<ChildIndexChangedEventArgs>? _childIndexChanged;
        private IDataTemplate? _displayMemberItemTemplate;
        private ScrollViewer? _scrollViewer;
        private ItemsPresenter? _itemsPresenter;

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemsControl"/> class.
        /// </summary>
        public ItemsControl()
        {
            _itemsView = ItemsSourceView.GetOrCreate(_items);
            _itemsView.PostCollectionChanged += ItemsCollectionChanged;
            UpdatePseudoClasses(0);
        }

        /// <summary>
        /// Gets the <see cref="ItemContainerGenerator"/> for the control.
        /// </summary>
        public ItemContainerGenerator ItemContainerGenerator
        {
#pragma warning disable CS0612 // Type or member is obsolete
            get => _itemContainerGenerator ??= CreateItemContainerGenerator();
#pragma warning restore CS0612 // Type or member is obsolete
        }

        /// <summary>
        /// Gets or sets the items to display.
        /// </summary>
        [Content]
        public IEnumerable? Items
        {
            get => _items;
            set => SetAndRaise(ItemsProperty, ref _items, value);
        }

        /// <summary>
        /// Gets or sets the <see cref="ControlTheme"/> that is applied to the container element generated for each item.
        /// </summary>
        public ControlTheme? ItemContainerTheme
        {
            get => GetValue(ItemContainerThemeProperty); 
            set => SetValue(ItemContainerThemeProperty, value);
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
        public ITemplate<Panel> ItemsPanel
        {
            get => GetValue(ItemsPanelProperty);
            set => SetValue(ItemsPanelProperty, value);
        }

        /// <summary>
        /// Gets or sets the data template used to display the items in the control.
        /// </summary>
        [InheritDataTypeFromItems(nameof(Items))]
        public IDataTemplate? ItemTemplate
        {
            get => GetValue(ItemTemplateProperty); 
            set => SetValue(ItemTemplateProperty, value);
        }

        /// <summary>
        /// Gets the items presenter control.
        /// </summary>
        public ItemsPresenter? Presenter { get; private set; }

        /// <summary>
        /// Gets the <see cref="Panel"/> specified by <see cref="ItemsPanel"/>.
        /// </summary>
        public Panel? ItemsPanelRoot => Presenter?.Panel;

        /// <summary>
        /// Gets a standardized view over <see cref="Items"/>.
        /// </summary>
        /// <remarks>
        /// The <see cref="Items"/> property may be an enumerable which does not implement
        /// <see cref="IList"/> or may be null. This view can be used to provide a standardized
        /// view of the current items regardless of the type of the concrete collection, and
        /// without having to deal with null values.
        /// </remarks>
        public ItemsSourceView ItemsView 
        {
            get => _itemsView;
            private set
            {
                if (ReferenceEquals(_itemsView, value))
                    return;

                var oldValue = _itemsView;
                RemoveControlItemsFromLogicalChildren(_itemsView);
                _itemsView.PostCollectionChanged -= ItemsCollectionChanged;
                _itemsView = value;
                _itemsView.PostCollectionChanged += ItemsCollectionChanged;
                AddControlItemsToLogicalChildren(_itemsView);
                RaisePropertyChanged(ItemsViewProperty, oldValue, _itemsView);
            }
        }

        private protected bool WrapFocus { get; set; }

        event EventHandler<ChildIndexChangedEventArgs>? IChildIndexProvider.ChildIndexChanged
        {
            add => _childIndexChanged += value;
            remove => _childIndexChanged -= value;
        }

        /// <inheritdoc />
        public event EventHandler<RoutedEventArgs> HorizontalSnapPointsChanged
        {
            add
            {
                if (_itemsPresenter != null)
                {
                    _itemsPresenter.HorizontalSnapPointsChanged += value;
                }
            }

            remove
            {
                if (_itemsPresenter != null)
                {
                    _itemsPresenter.HorizontalSnapPointsChanged -= value;
                }
            }
        }

        /// <inheritdoc />
        public event EventHandler<RoutedEventArgs> VerticalSnapPointsChanged
        {
            add
            {
                if (_itemsPresenter != null)
                {
                    _itemsPresenter.VerticalSnapPointsChanged += value;
                }
            }

            remove
            {
                if (_itemsPresenter != null)
                {
                    _itemsPresenter.VerticalSnapPointsChanged -= value;
                }
            }
        }

        /// <summary>
        /// Gets or sets whether the horizontal snap points for the <see cref="ItemsControl"/> are equidistant from each other.
        /// </summary>
        public bool AreHorizontalSnapPointsRegular
        {
            get => GetValue(AreHorizontalSnapPointsRegularProperty); 
            set => SetValue(AreHorizontalSnapPointsRegularProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the vertical snap points for the <see cref="ItemsControl"/> are equidistant from each other.
        /// </summary>
        public bool AreVerticalSnapPointsRegular
        {
            get => GetValue(AreVerticalSnapPointsRegularProperty); 
            set => SetValue(AreVerticalSnapPointsRegularProperty, value);
        }

        /// <summary>
        /// Returns the container for the item at the specified index.
        /// </summary>
        /// <param name="index">The index of the item to retrieve.</param>
        /// <returns>
        /// The container for the item at the specified index within the item collection, if the
        /// item has a container; otherwise, null.
        /// </returns>
        public Control? ContainerFromIndex(int index) => Presenter?.ContainerFromIndex(index);

        /// <summary>
        /// Returns the container corresponding to the specified item.
        /// </summary>
        /// <param name="item">The item to retrieve the container for.</param>
        /// <returns>
        /// A container that corresponds to the specified item, if the item has a container and
        /// exists in the collection; otherwise, null.
        /// </returns>
        public Control? ContainerFromItem(object item)
        {
            var index = ItemsView.IndexOf(item);
            return index >= 0 ? ContainerFromIndex(index) : null;
        }

        /// <summary>
        /// Returns the index to the item that has the specified, generated container.
        /// </summary>
        /// <param name="container">The generated container to retrieve the item index for.</param>
        /// <returns>
        /// The index to the item that corresponds to the specified generated container, or -1 if 
        /// <paramref name="container"/> is not found.
        /// </returns>
        public int IndexFromContainer(Control container) => Presenter?.IndexFromContainer(container) ?? -1;

        /// <summary>
        /// Returns the item that corresponds to the specified, generated container.
        /// </summary>
        /// <param name="container">The control that corresponds to the item to be returned.</param>
        /// <returns>
        /// The contained item, or the container if it does not contain an item.
        /// </returns>
        public object? ItemFromContainer(Control container)
        {
            var index = IndexFromContainer(container);
            return index >= 0 && index < ItemsView.Count ? ItemsView[index] : null;
        }

        /// <summary>
        /// Gets the currently realized containers.
        /// </summary>
        public IEnumerable<Control> GetRealizedContainers() => Presenter?.GetRealizedContainers() ?? Array.Empty<Control>();

        /// <summary>
        /// Creates or a container that can be used to display an item.
        /// </summary>
        protected internal virtual Control CreateContainerForItemOverride() => new ContentPresenter();

        /// <summary>
        /// Prepares the specified element to display the specified item.
        /// </summary>
        /// <param name="container">The element that's used to display the specified item.</param>
        /// <param name="item">The item to display.</param>
        /// <param name="index">The index of the item to display.</param>
        protected internal virtual void PrepareContainerForItemOverride(Control container, object? item, int index)
        {
            if (container == item)
                return;

            var itemTemplate = GetEffectiveItemTemplate();

            if (container is HeaderedContentControl hcc)
            {
                hcc.Content = item;

                if (item is IHeadered headered)
                    hcc.Header = headered.Header;
                else if (item is not Visual)
                    hcc.Header = item;

                if (itemTemplate is not null)
                    hcc.HeaderTemplate = itemTemplate;
            }
            else if (container is ContentControl cc)
            {
                cc.Content = item;
                if (itemTemplate is not null)
                    cc.ContentTemplate = itemTemplate;
            }
            else if (container is ContentPresenter p)
            {
                p.Content = item;
                if (itemTemplate is not null)
                    p.ContentTemplate = itemTemplate;
            }
            else if (container is ItemsControl ic)
            {
                if (itemTemplate is not null)
                    ic.ItemTemplate = itemTemplate;
                if (ItemContainerTheme is { } ict && !ict.IsSet(ItemContainerThemeProperty))
                    ic.ItemContainerTheme = ict;
            }

            // This condition is separate because HeaderedItemsControl needs to also run the
            // ItemsControl preparation.
            if (container is HeaderedItemsControl hic)
            {
                hic.Header = item;
                hic.HeaderTemplate = itemTemplate;
                hic.PrepareItemContainer();
            }
        }

        /// <summary>
        /// Called when the index for a container changes due to an insertion or removal in the
        /// items collection.
        /// </summary>
        /// <param name="container">The container whose index changed.</param>
        /// <param name="oldIndex">The old index.</param>
        /// <param name="newIndex">The new index.</param>
        protected virtual void ContainerIndexChangedOverride(Control container, int oldIndex, int newIndex)
        {
        }

        /// <summary>
        /// Undoes the effects of the <see cref="PrepareContainerForItemOverride(Control, object?, int)"/> method.
        /// </summary>
        /// <param name="container">The container element.</param>
        protected internal virtual void ClearContainerForItemOverride(Control container)
        {
            // Feels like we should be clearing the HeaderedItemsControl.Items binding here, but looking at
            // the WPF source it seems that this isn't done there.
        }

        /// <summary>
        /// Determines whether the specified item is (or is eligible to be) its own container.
        /// </summary>
        /// <param name="item">The item to check.</param>
        /// <returns>true if the item is (or is eligible to be) its own container; otherwise, false.</returns>
        protected internal virtual bool IsItemItsOwnContainerOverride(Control item) => true;

        /// <inheritdoc />
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            _scrollViewer = e.NameScope.Find<ScrollViewer>("PART_ScrollViewer");
            _itemsPresenter = e.NameScope.Find<ItemsPresenter>("PART_ItemsPresenter");
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
                    focus?.Current == null ||
                    direction == null ||
                    direction.Value.IsTab())
                {
                    return;
                }

                Visual? current = focus.Current as Visual;

                while (current != null)
                {
                    if (current.VisualParent == container && current is IInputElement inputElement)
                    {
                        var next = GetNextControl(container, direction.Value, inputElement, WrapFocus);

                        if (next != null)
                        {
                            focus.Focus(next, NavigationMethod.Directional, e.KeyModifiers);
                            e.Handled = true;
                        }

                        break;
                    }

                    current = current.VisualParent;
                }
            }

            base.OnKeyDown(e);
        }

        /// <inheritdoc />
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new ItemsControlAutomationPeer(this);
        }

        /// <inheritdoc />
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == ItemsProperty)
            {
                ItemsView = ItemsSourceView.GetOrCreate(change.GetNewValue<IEnumerable?>());
                ItemCount = ItemsView.Count;
            }
            else if (change.Property == ItemCountProperty)
            {
                UpdatePseudoClasses(change.GetNewValue<int>());
                _childIndexChanged?.Invoke(this, ChildIndexChangedEventArgs.TotalCountChanged);
            }
            else if (change.Property == ItemContainerThemeProperty && _itemContainerGenerator is not null)
            {
                RefreshContainers();
            }
            else if (change.Property == ItemTemplateProperty)
            {
                if (change.NewValue is not null && DisplayMemberBinding is not null)
                    throw new InvalidOperationException("Cannot set both DisplayMemberBinding and ItemTemplate.");
                RefreshContainers();
            }
            else if (change.Property == DisplayMemberBindingProperty)
            {
                if (change.NewValue is not null && ItemTemplate is not null)
                    throw new InvalidOperationException("Cannot set both DisplayMemberBinding and ItemTemplate.");
                _displayMemberItemTemplate = null;
                RefreshContainers();
            }
        }

        /// <summary>
        /// Refreshes the containers displayed by the control.
        /// </summary>
        /// <remarks>
        /// Causes all containers to be unrealized and re-realized.
        /// </remarks>
        protected void RefreshContainers() => Presenter?.Refresh();

        /// <summary>
        /// Called when the <see cref="INotifyCollectionChanged.CollectionChanged"/> event is
        /// raised on <see cref="Items"/>.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        protected virtual void ItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            ItemCount = _itemsView.Count;

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    AddControlItemsToLogicalChildren(e.NewItems);
                    break;

                case NotifyCollectionChangedAction.Remove:
                    RemoveControlItemsFromLogicalChildren(e.OldItems);
                    break;
            }
        }

        /// <summary>
        /// Creates the <see cref="ItemContainerGenerator"/>
        /// </summary>
        /// <remarks>
        /// This method is only present for backwards compatibility with 0.10.x in order for
        /// TreeView to be able to create a <see cref="TreeItemContainerGenerator"/>. Can be
        /// removed in 12.0.
        /// </remarks>
        [Obsolete]
        private protected virtual ItemContainerGenerator CreateItemContainerGenerator()
        {
            return new ItemContainerGenerator(this);
        }

        internal void AddLogicalChild(Control c)
        {
            if (!LogicalChildren.Contains(c))
                LogicalChildren.Add(c);
        }

        internal void RemoveLogicalChild(Control c) => LogicalChildren.Remove(c);

        /// <summary>
        /// Called by <see cref="ItemsPresenter"/> to register with the <see cref="ItemsControl"/>.
        /// </summary>
        /// <param name="presenter">The items presenter.</param>
        /// <remarks>
        /// ItemsPresenters can be within nested templates or in popups and so are not necessarily
        /// created immediately when the ItemsControl control's template is instantiated. Instead
        /// they register themselves using this method.
        /// </remarks>
        internal void RegisterItemsPresenter(ItemsPresenter presenter)
        {
            Presenter = presenter;
            _childIndexChanged?.Invoke(this, ChildIndexChangedEventArgs.ChildIndexesReset);
        }

        internal void PrepareItemContainer(Control container, object? item, int index)
        {
            var itemContainerTheme = ItemContainerTheme;

            if (itemContainerTheme is not null && 
                !container.IsSet(ThemeProperty) &&
                ((IStyleable)container).StyleKey == itemContainerTheme.TargetType)
            {
                container.Theme = itemContainerTheme;
            }

            if (item is not Control)
                container.DataContext = item;

            PrepareContainerForItemOverride(container, item, index);
        }

        internal void ItemContainerPrepared(Control container, object? item, int index)
        {
            _childIndexChanged?.Invoke(this, new ChildIndexChangedEventArgs(container, index));
            _scrollViewer?.RegisterAnchorCandidate(container);
        }

        internal void ItemContainerIndexChanged(Control container, int oldIndex, int newIndex)
        {
            ContainerIndexChangedOverride(container, oldIndex, newIndex);
            _childIndexChanged?.Invoke(this, new ChildIndexChangedEventArgs(container, newIndex));
        }

        internal void ClearItemContainer(Control container)
        {
            _scrollViewer?.UnregisterAnchorCandidate(container);
            ClearContainerForItemOverride(container);
        }

        /// <summary>
        /// Given a collection of items, adds those that are controls to the logical children.
        /// </summary>
        /// <param name="items">The items.</param>
        private void AddControlItemsToLogicalChildren(IEnumerable? items)
        {
            if (items is null)
                return;

            List<ILogical>? toAdd = null;

            foreach (var i in items)
            {
                if (i is Control control && !LogicalChildren.Contains(control))
                {
                    toAdd ??= new();
                    toAdd.Add(control);
                }
            }

            if (toAdd is not null)
                LogicalChildren.AddRange(toAdd);
        }

        /// <summary>
        /// Given a collection of items, removes those that are controls to from logical children.
        /// </summary>
        /// <param name="items">The items.</param>
        private void RemoveControlItemsFromLogicalChildren(IEnumerable? items)
        {
            if (items is null)
                return;

            List<ILogical>? toRemove = null;

            foreach (var i in items)
            {
                if (i is Control control)
                {
                    toRemove ??= new();
                    toRemove.Add(control);
                }
            }

            if (toRemove is not null)
                LogicalChildren.RemoveAll(toRemove);
        }

        private IDataTemplate? GetEffectiveItemTemplate()
        {
            if (ItemTemplate is { } itemTemplate)
                return itemTemplate;

            if (_displayMemberItemTemplate is null && DisplayMemberBinding is { } binding)
            {
                _displayMemberItemTemplate = new FuncDataTemplate<object?>((_, _) =>
                    new TextBlock
                    {
                        [!TextBlock.TextProperty] = binding,
                    });
            }

            return _displayMemberItemTemplate;
        }

        private void UpdatePseudoClasses(int itemCount)
        {
            PseudoClasses.Set(":empty", itemCount == 0);
            PseudoClasses.Set(":singleitem", itemCount == 1);
        }

        protected static IInputElement? GetNextControl(
            INavigableContainer container,
            NavigationDirection direction,
            IInputElement? from,
            bool wrap)
        {
            var current = from;

            for (;;)
            {
                var result = container.GetControl(direction, current, wrap);

                if (result is null)
                {
                    return null;
                }

                if (result.Focusable &&
                    result.IsEffectivelyEnabled &&
                    result.IsEffectivelyVisible)
                {
                    return result;
                }

                current = result;

                if (current == from)
                {
                    return null;
                }

                switch (direction)
                {
                    //We did not find an enabled first item. Move downwards until we find one.
                    case NavigationDirection.First:
                        direction = NavigationDirection.Down;
                        from = result;
                        break;

                    //We did not find an enabled last item. Move upwards until we find one.
                    case NavigationDirection.Last:
                        direction = NavigationDirection.Up;
                        from = result;
                        break;

                }
            }
        }

        int IChildIndexProvider.GetChildIndex(ILogical child)
        {
            return child is Control container ? IndexFromContainer(container) : -1;
        }

        bool IChildIndexProvider.TryGetTotalCount(out int count)
        {
            count = ItemsView.Count;
            return true;
        }

        /// <inheritdoc />
        public IReadOnlyList<double> GetIrregularSnapPoints(Orientation orientation, SnapPointsAlignment snapPointsAlignment)
        {
            return _itemsPresenter?.GetIrregularSnapPoints(orientation, snapPointsAlignment) ?? new List<double>();
        }

        /// <inheritdoc />
        public double GetRegularSnapPoints(Orientation orientation, SnapPointsAlignment snapPointsAlignment, out double offset)
        {
            offset = 0;

            return _itemsPresenter?.GetRegularSnapPoints(orientation, snapPointsAlignment, out offset) ?? 0;
        }
    }
}
