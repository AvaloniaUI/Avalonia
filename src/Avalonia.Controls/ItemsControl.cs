using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Controls.Generators;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Controls.Utils;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Metadata;
using Avalonia.VisualTree;

#nullable enable

namespace Avalonia.Controls
{
    /// <summary>
    /// Displays a collection of items.
    /// </summary>
    [PseudoClasses(":empty", ":singleitem")]
    public class ItemsControl : TemplatedControl, IItemsPresenterHost, IChildIndexProvider
    {
        /// <summary>
        /// The default value for the <see cref="ItemsPanel"/> property.
        /// </summary>
        private static readonly FuncTemplate<IPanel> DefaultPanel =
            new FuncTemplate<IPanel>(() => new StackPanel());

        /// <summary>
        /// Defines the <see cref="Items"/> property.
        /// </summary>
        public static readonly DirectProperty<ItemsControl, IEnumerable?> ItemsProperty =
            AvaloniaProperty.RegisterDirect<ItemsControl, IEnumerable?>(nameof(Items), o => o.Items, (o, v) => o.Items = v);

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
        public static readonly StyledProperty<IDataTemplate?> ItemTemplateProperty =
            AvaloniaProperty.Register<ItemsControl, IDataTemplate?>(nameof(ItemTemplate));

        /// <summary>
        /// Defines the <see cref="ItemsView"/> property.
        /// </summary>
        public static readonly DirectProperty<ItemsControl, ItemsSourceView> ItemsViewProperty =
            AvaloniaProperty.RegisterDirect<ItemsControl, ItemsSourceView>(nameof(ItemsView), o => o.ItemsView);

        private bool _itemsInitialized;
        private IEnumerable? _items;
        private int _itemCount;
        private ItemsSourceView? _itemsView;
        private IItemContainerGenerator? _generator;
        private EventHandler<ChildIndexChangedEventArgs>? _childIndexChanged;
        private int _blockReentrancyCount;

        /// <summary>
        /// Initializes static members of the <see cref="ItemsControl"/> class.
        /// </summary>
        static ItemsControl()
        {
            ItemTemplateProperty.Changed.AddClassHandler<ItemsControl>((x, e) => x.ItemTemplateChanged(e));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemsControl"/> class.
        /// </summary>
        public ItemsControl()
        {
            UpdatePseudoClasses();
        }

        /// <summary>
        /// Gets the <see cref="IItemContainerGenerator"/> which creates containers for the control.
        /// </summary>
        public IItemContainerGenerator? ItemContainerGenerator => _generator ??= CreateItemContainerGenerator();

        /// <summary>
        /// Gets or sets the items to display.
        /// </summary>
        [Content]
        public IEnumerable? Items
        {
            get
            {
                if (!_itemsInitialized)
                {
                    _items = new AvaloniaList<object>();
                    _itemsInitialized = true;
                    CreateItemsView();
                }

                return _items;
            }
            set
            {
                using var monitor = new SimpleMonitor(this, true);

                if (_items != value)
                {
                    var oldItems = _items;
                    var oldItemsView = _itemsView;

                    _items = value;
                    _itemsInitialized = true;
                    CreateItemsView();

                    RaisePropertyChanged(ItemsProperty,
                        new Optional<IEnumerable?>(oldItems),
                        new BindingValue<IEnumerable?>(_items));
                    RaisePropertyChanged(ItemsViewProperty, oldItemsView, _itemsView);
                }
            }
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
        /// Gets an <see cref="ItemsSourceView"/> over <see cref="Items"/>.
        /// </summary>
        public ItemsSourceView ItemsView
        {
            get
            {
                if (!_itemsInitialized)
                {
                    _items = new AvaloniaList<object>();
                    _itemsInitialized = true;
                    CreateItemsView();
                }

                return _itemsView!;
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
        /// Gets or sets the data template used to display the items in the control.
        /// </summary>
        public IDataTemplate? ItemTemplate
        {
            get { return GetValue(ItemTemplateProperty); }
            set { SetValue(ItemTemplateProperty, value); }
        }

        /// <summary>
        /// Gets the items presenter control.
        /// </summary>
        public IItemsPresenter? Presenter { get; protected set; }

        /// <summary>
        /// Occurs when an item container is realized.
        /// </summary>
        public event EventHandler<ItemContainerRealizedEventArgs>? ContainerRealized;
        
        /// <summary>
        /// Occurs when an item container is unrealized.
        /// </summary>
        public event EventHandler<ItemContainerUnrealizedEventArgs>? ContainerUnrealized;

        /// <summary>
        /// Occurs when the index of an item container is changed.
        /// </summary>
        public event EventHandler<ItemContainerIndexChangedEventArgs>? ContainerIndexChanged;

        event EventHandler<ChildIndexChangedEventArgs>? IChildIndexProvider.ChildIndexChanged
        {
            add => _childIndexChanged += value;
            remove => _childIndexChanged -= value;
        }

        internal event NotifyCollectionChangedEventHandler? ItemsChanged;

        public IControl? GetContainerForIndex(int index) => Presenter?.GetContainerForIndex(index);

        public int GetContainerIndex(IControl container) => Presenter?.GetContainerIndex(container) ?? -1;

        void IItemsPresenterHost.RegisterItemsPresenter(IItemsPresenter presenter)
        {
            if (Presenter is IChildIndexProvider oldInnerProvider)
            {
                oldInnerProvider.ChildIndexChanged -= PresenterChildIndexChanged;
            }

            Presenter = presenter;

            if (Presenter is IChildIndexProvider innerProvider)
            {
                innerProvider.ChildIndexChanged += PresenterChildIndexChanged;
                _childIndexChanged?.Invoke(this, new ChildIndexChangedEventArgs());
            }
        }

        /// <summary>
        /// Called by an <see cref="ItemsPresenter"/> to raise the <see cref="ContainerRealized"/> event.
        /// </summary>
        internal void RaiseContainerRealized(IControl container, int index, object? item)
        {
            using var monitor = new SimpleMonitor(this, false);
            OnContainerRealized(container, index, item);
        }

        /// <summary>
        /// Called by an <see cref="ItemsPresenter"/> to raise the <see cref="ContainerUnrealized"/> event.
        /// </summary>
        internal void RaiseContainerUnrealized(IControl container, int index)
        {
            using var monitor = new SimpleMonitor(this, false);
            OnContainerUnrealized(container, index);
        }

        /// <summary>
        /// Called by an <see cref="ItemsPresenter"/> to raise the <see cref="ContainerIndexChanged"/> event.
        /// </summary>
        internal void RaiseContainerIndexChanged(IControl container, int oldIndex, int newIndex)
        {
            using var monitor = new SimpleMonitor(this, false);
            OnContainerIndexChanged(container, oldIndex, newIndex);
        }

        /// <summary>
        /// Gets the item at the specified index in a collection.
        /// </summary>
        /// <param name="items">The collection.</param>
        /// <param name="index">The index.</param>
        /// <returns>The item at the given index or null if the index is out of bounds.</returns>
        protected static object? ElementAt(IEnumerable items, int index)
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
        /// Creates the <see cref="ItemContainerGenerator"/> which creates containers for the control.
        /// </summary>
        /// <returns>
        /// An <see cref="IItemContainerGenerator"/> or null.
        /// </returns>
        /// <remarks>
        /// Certain controls such as <see cref="TabControl"/> don't actually create item 
        /// containers; however they want to be ItemsControls so that they have an Items 
        /// property etc. In this case, a derived class can override this method to return null
        /// in order to disable the creation of item containers.
        /// </remarks>
        protected virtual IItemContainerGenerator? CreateItemContainerGenerator() => new ItemContainerGenerator(this);

        /// <summary>
        /// Raises the <see cref="ContainerRealized"/> event.
        /// </summary>
        protected virtual void OnContainerRealized(IControl container, int index, object? item)
        {
            if (container != item)
                LogicalChildren.Add(container);
            ContainerRealized?.Invoke(this, new ItemContainerRealizedEventArgs(container, index, item));
        }

        /// <summary>
        /// Raises the <see cref="ContainerUnrealized"/> event.
        /// </summary>
        protected virtual void OnContainerUnrealized(IControl container, int index)
        {
            LogicalChildren.Remove(container);
            ContainerUnrealized?.Invoke(this, new ItemContainerUnrealizedEventArgs(container, index));
        }

        /// <summary>
        /// Raises the <see cref="ContainerIndexChanged"/> event.
        /// </summary>
        protected virtual void OnContainerIndexChanged(IControl container, int oldIndex, int newIndex)
        {
            ContainerIndexChanged?.Invoke(this, new ItemContainerIndexChangedEventArgs(container, oldIndex, newIndex));
        }

        /// <summary>
        /// Called when a change is made to the items displayed by the control, either due to the
        /// value of <see cref="Items"/> changing or a
        /// <see cref="INotifyCollectionChanged.CollectionChanged"/> event occuring on the items.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected virtual void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    AddControlItemsToLogicalChildren(e.NewItems);
                    break;

                case NotifyCollectionChangedAction.Remove:
                    RemoveControlItemsFromLogicalChildren(e.OldItems);
                    break;

                case NotifyCollectionChangedAction.Reset:
                    LogicalChildren.Clear();
                    AddControlItemsToLogicalChildren(ItemsView);
                    break;
            }

            UpdatePseudoClasses();
            ItemCount = ItemsView.Count;
            ItemsChanged?.Invoke(this, e);
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

                IVisual? current = focus.Current;

                while (current != null)
                {
                    if (current.VisualParent == container && current is IInputElement inputElement)
                    {
                        IInputElement next = GetNextControl(container, direction.Value, inputElement, false);

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

        protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == ItemCountProperty)
            {
                UpdatePseudoClasses();
            }
        }

        private void CreateItemsView()
        {
            void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                using var monitor = new SimpleMonitor(this, true);
                OnItemsChanged(e);
            }

            var oldView = _itemsView;

            if (_itemsView is object)
            {
                RemoveControlItemsFromLogicalChildren(_itemsView);
                _itemsView.CollectionChanged -= OnCollectionChanged;
            }

            _itemsView = ItemsSourceView.GetOrCreate(_items);
            AddControlItemsToLogicalChildren(_itemsView);
            UpdatePseudoClasses();
            ItemCount = ItemsView.Count;

            if (_itemsView != ItemsSourceView.Empty)
            {
                _itemsView.CollectionChanged += OnCollectionChanged;
            }

            RaisePropertyChanged(ItemsViewProperty, oldView, _itemsView);
            OnItemsChanged(CollectionExtensions.ResetEvent);

            if (oldView is object && oldView != ItemsSourceView.Empty)
            {
                oldView?.Dispose();
            }
        }

        /// <summary>
        /// Given a collection of items, adds those that are controls to the logical children.
        /// </summary>
        /// <param name="items">The items.</param>
        private void AddControlItemsToLogicalChildren(IEnumerable? items)
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
        private void RemoveControlItemsFromLogicalChildren(IEnumerable? items)
        {
            if (items is null || items == ItemsSourceView.Empty)
                return;

            var toRemove = new List<ILogical>();

            foreach (var i in items)
            {
                var control = i as IControl;

                if (control != null)
                {
                    toRemove.Add(control);
                }
            }

            LogicalChildren.RemoveAll(toRemove);
        }

        /// <summary>
        /// Called when the <see cref="ItemTemplate"/> changes.
        /// </summary>
        /// <param name="e">The event args.</param>
        private void ItemTemplateChanged(AvaloniaPropertyChangedEventArgs e)
        {
            ////Presenter?.UnrealizeAll();
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

        private void UpdatePseudoClasses()
        {
            var count = _itemsView?.Count ?? 0;
            PseudoClasses.Set(":empty", count == 0);
            PseudoClasses.Set(":singleitem", count == 1);
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

                if (result != null &&
                    result.Focusable &&
                    result.IsEffectivelyEnabled &&
                    result.IsEffectivelyVisible)
                {
                    return result;
                }

                c = result;
            } while (c != null && c != from);

            return null;
        }

        private void PresenterChildIndexChanged(object sender, ChildIndexChangedEventArgs e)
        {
            _childIndexChanged?.Invoke(this, e);
        }

        int IChildIndexProvider.GetChildIndex(ILogical child)
        {
            return Presenter is IChildIndexProvider innerProvider
                ? innerProvider.GetChildIndex(child) : -1;
        }

        bool IChildIndexProvider.TryGetTotalCount(out int count)
        {
            if (Presenter is IChildIndexProvider presenter
                && presenter.TryGetTotalCount(out count))
            {
                return true;
            }

            count = ItemCount;
            return true;
        }


        private struct SimpleMonitor : IDisposable
        {
            private readonly ItemsControl _owner;

            public SimpleMonitor(ItemsControl owner, bool throwOnEntry)
            {
                if (throwOnEntry && owner._blockReentrancyCount > 0)
                    throw new InvalidOperationException(
                        "Cannot change ItemsControl items during a container lifecycle event.");
                ++owner._blockReentrancyCount;
                _owner = owner;
            }

            public void Dispose() => --_owner._blockReentrancyCount;
        }
    }
}
