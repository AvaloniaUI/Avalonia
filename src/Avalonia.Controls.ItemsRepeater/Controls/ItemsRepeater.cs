// This source file is adapted from the WinUI project.
// (https://github.com/microsoft/microsoft-ui-xaml)
//
// Licensed to The Avalonia Project under MIT License, courtesy of The .NET Foundation.

using System;
using System.Collections;
using System.Collections.Specialized;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Logging;
using Avalonia.LogicalTree;
using Avalonia.Metadata;
using Avalonia.Utilities;
using Avalonia.VisualTree;

namespace Avalonia.Controls
{
    /// <summary>
    /// Represents a data-driven collection control that incorporates a flexible layout system,
    /// custom views, and virtualization.
    /// </summary>
    public class ItemsRepeater : Panel, IChildIndexProvider
    {
        /// <summary>
        /// Defines the <see cref="HorizontalCacheLength"/> property.
        /// </summary>
        public static readonly StyledProperty<double> HorizontalCacheLengthProperty =
            AvaloniaProperty.Register<ItemsRepeater, double>(nameof(HorizontalCacheLength), 2.0);

        /// <summary>
        /// Defines the <see cref="ItemTemplate"/> property.
        /// </summary>
        public static readonly StyledProperty<IDataTemplate?> ItemTemplateProperty =
            ItemsControl.ItemTemplateProperty.AddOwner<ItemsRepeater>();

        /// <summary>
        /// Defines the <see cref="ItemsSource"/> property.
        /// </summary>
        public static readonly DirectProperty<ItemsRepeater, IEnumerable?> ItemsSourceProperty =
            AvaloniaProperty.RegisterDirect<ItemsRepeater, IEnumerable?>(
                nameof(ItemsSource),
                o => o.ItemsSource,
                (o, v) => o.ItemsSource = v);

        /// <summary>
        /// Defines the <see cref="Layout"/> property.
        /// </summary>
        public static readonly StyledProperty<AttachedLayout?> LayoutProperty =
            AvaloniaProperty.Register<ItemsRepeater, AttachedLayout?>(nameof(Layout), new StackLayout());

        /// <summary>
        /// Defines the <see cref="VerticalCacheLength"/> property.
        /// </summary>
        public static readonly StyledProperty<double> VerticalCacheLengthProperty =
            AvaloniaProperty.Register<ItemsRepeater, double>(nameof(VerticalCacheLength), 2.0);

        private static readonly StyledProperty<VirtualizationInfo?> VirtualizationInfoProperty =
            AvaloniaProperty.RegisterAttached<ItemsRepeater, Control, VirtualizationInfo?>("VirtualizationInfo");

        internal static readonly Rect InvalidRect = new Rect(-1, -1, -1, -1);
        internal static readonly Point ClearedElementsArrangePosition = new Point(-10000.0, -10000.0);

        private readonly ViewManager _viewManager;
        private readonly ViewportManager _viewportManager;
        private readonly TargetWeakEventSubscriber<ItemsRepeater, EventArgs> _layoutWeakSubscriber;
        private IEnumerable? _itemsSource;
        private RepeaterLayoutContext? _layoutContext;
        private EventHandler<ChildIndexChangedEventArgs>? _childIndexChanged;
        private bool _isLayoutInProgress;
        private NotifyCollectionChangedEventArgs? _processingItemsSourceChange;
        private ItemsRepeaterElementPreparedEventArgs? _elementPreparedArgs;
        private ItemsRepeaterElementClearingEventArgs? _elementClearingArgs;
        private ItemsRepeaterElementIndexChangedEventArgs? _elementIndexChangedArgs;

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemsRepeater"/> class.
        /// </summary>
        public ItemsRepeater()
        {
            _layoutWeakSubscriber = new TargetWeakEventSubscriber<ItemsRepeater, EventArgs>(
                this, static (target, _, ev, _) =>
                {
                    if (ev == AttachedLayout.ArrangeInvalidatedWeakEvent)
                        target.InvalidateArrange();
                    else if (ev == AttachedLayout.MeasureInvalidatedWeakEvent)
                        target.InvalidateMeasure();
                });

            _viewManager = new ViewManager(this);
            _viewportManager = new ViewportManager(this);
            KeyboardNavigation.SetTabNavigation(this, KeyboardNavigationMode.Once);
            OnLayoutChanged(null, Layout);
        }

        static ItemsRepeater()
        {
            ClipToBoundsProperty.OverrideDefaultValue<ItemsRepeater>(true);
            RequestBringIntoViewEvent.AddClassHandler<ItemsRepeater>((x, e) => x.OnRequestBringIntoView(e));
        }

        /// <summary>
        /// Gets or sets the layout used to size and position elements in the ItemsRepeater.
        /// </summary>
        /// <value>
        /// The layout used to size and position elements. The default is a StackLayout with
        /// vertical orientation.
        /// </value>
        public AttachedLayout? Layout
        {
            get => GetValue(LayoutProperty);
            set => SetValue(LayoutProperty, value);
        }

        /// <summary>
        /// Gets or sets an object source used to generate the content of the ItemsRepeater.
        /// </summary>
        public IEnumerable? ItemsSource
        {
            get => _itemsSource;
            set => SetAndRaise(ItemsSourceProperty, ref _itemsSource, value);
        }

        /// <summary>
        /// Gets or sets the template used to display each item.
        /// </summary>
        [InheritDataTypeFromItems(nameof(ItemsSource))]
        public IDataTemplate? ItemTemplate
        {
            get => GetValue(ItemTemplateProperty);
            set => SetValue(ItemTemplateProperty, value);
        }

        /// <summary>
        /// Gets or sets a value that indicates the size of the buffer used to realize items when
        /// panning or scrolling horizontally.
        /// </summary>
        public double HorizontalCacheLength
        {
            get => GetValue(HorizontalCacheLengthProperty);
            set => SetValue(HorizontalCacheLengthProperty, value);
        }

        /// <summary>
        /// Gets or sets a value that indicates the size of the buffer used to realize items when
        /// panning or scrolling vertically.
        /// </summary>
        public double VerticalCacheLength
        {
            get => GetValue(VerticalCacheLengthProperty);
            set => SetValue(VerticalCacheLengthProperty, value);
        }

        /// <summary>
        /// Gets a standardized view of the supported interactions between a given Items object and
        /// the ItemsRepeater control and its associated components.
        /// </summary>
        public ItemsSourceView? ItemsSourceView { get; private set; }

        internal IElementFactory? ItemTemplateShim { get; set; }
        internal Point LayoutOrigin { get; set; }
        internal object? LayoutState { get; set; }
        internal Control? MadeAnchor => _viewportManager.MadeAnchor;
        internal Rect RealizationWindow => _viewportManager.GetLayoutRealizationWindow();
        internal Control? SuggestedAnchor => _viewportManager.SuggestedAnchor;

        private bool IsProcessingCollectionChange => _processingItemsSourceChange != null;

        private RepeaterLayoutContext LayoutContext => _layoutContext ??= new RepeaterLayoutContext(this);

        event EventHandler<ChildIndexChangedEventArgs>? IChildIndexProvider.ChildIndexChanged
        {
            add => _childIndexChanged += value;
            remove => _childIndexChanged -= value;
        }

        int IChildIndexProvider.GetChildIndex(ILogical child)
        {
            return child is Control control
                ? GetElementIndex(control)
                : -1;
        }

        bool IChildIndexProvider.TryGetTotalCount(out int count)
        {
            count = ItemsSourceView?.Count ?? 0;
            return true;
        }

        /// <summary>
        /// Occurs each time an element is cleared and made available to be re-used.
        /// </summary>
        /// <remarks>
        /// This event is raised immediately each time an element is cleared, such as when it falls
        /// outside the range of realized items. Elements are cleared when they become available
        /// for re-use.
        /// </remarks>
        public event EventHandler<ItemsRepeaterElementClearingEventArgs>? ElementClearing;

        /// <summary>
        /// Occurs for each realized <see cref="Control"/> when the index for the item it
        /// represents has changed.
        /// </summary>
        /// <remarks>
        /// When you use ItemsRepeater to build a more complex control that supports specific
        /// interactions on the child elements (such as selection or click), it is useful to be
        /// able to keep an up-to-date identifier for the backing data item.
        ///
        /// This event is raised for each realized Control where the index for the item it
        /// represents has changed. For example, when another item is added or removed in the data
        /// source, the index for items that come after in the ordering will be impacted.
        /// </remarks>
        public event EventHandler<ItemsRepeaterElementIndexChangedEventArgs>? ElementIndexChanged;

        /// <summary>
        /// Occurs each time an element is prepared for use.
        /// </summary>
        /// <remarks>
        /// The prepared element might be newly created or an existing element that is being re-
        /// used.
        /// </remarks>
        public event EventHandler<ItemsRepeaterElementPreparedEventArgs>? ElementPrepared;

        /// <summary>
        /// Retrieves the index of the item from the data source that corresponds to the specified
        /// <see cref="Control"/>.
        /// </summary>
        /// <param name="element">
        /// The element that corresponds to the item to get the index of.
        /// </param>
        /// <returns>
        /// The index of the item from the data source that corresponds to the specified UIElement,
        /// or -1 if the element is not supported.
        /// </returns>
        public int GetElementIndex(Control element) => GetElementIndexImpl(element);

        /// <summary>
        /// Retrieves the realized UIElement that corresponds to the item at the specified index in
        /// the data source.
        /// </summary>
        /// <param name="index">The index of the item.</param>
        /// <returns>
        /// he UIElement that corresponds to the item at the specified index if the item is
        /// realized, or null if the item is not realized.
        /// </returns>
        public Control? TryGetElement(int index) => GetElementFromIndexImpl(index);

        /// <summary>
        /// Retrieves the UIElement that corresponds to the item at the specified index in the
        /// data source.
        /// </summary>
        /// <param name="index">The index of the item.</param>
        /// <returns>
        /// An <see cref="Control"/> that corresponds to the item at the specified index. If the
        /// item is not realized, a new UIElement is created.
        /// </returns>
        public Control GetOrCreateElement(int index) => GetOrCreateElementImpl(index);

        internal void PinElement(Control element) => _viewManager.UpdatePin(element, true);

        internal void UnpinElement(Control element) => _viewManager.UpdatePin(element, false);

        internal static VirtualizationInfo? TryGetVirtualizationInfo(Control? element)
        {
            return element?.GetValue(VirtualizationInfoProperty);
        }

        internal static VirtualizationInfo GetVirtualizationInfo(Control element)
        {
            var result = element.GetValue(VirtualizationInfoProperty);

            if (result == null)
            {
                result = new VirtualizationInfo();
                element.SetValue(VirtualizationInfoProperty, result);
            }

            return result;
        }

        private protected override void InvalidateMeasureOnChildrenChanged()
        {
            // Don't invalidate measure when children change.
        }

        /// <inheritdoc />
        protected override Size MeasureOverride(Size availableSize)
        {
            if (_isLayoutInProgress)
            {
                throw new AvaloniaInternalException("Reentrancy detected during layout.");
            }

            if (IsProcessingCollectionChange)
            {
                throw new NotSupportedException("Cannot run layout in the middle of a collection change.");
            }

            _viewportManager.OnOwnerMeasuring();

            _isLayoutInProgress = true;

            try
            {
                _viewManager.PrunePinnedElements();
                var extent = new Rect();
                var desiredSize = new Size();
                var layout = Layout;

                if (layout != null)
                {
                    var layoutContext = LayoutContext;

                    desiredSize = layout.Measure(layoutContext, availableSize);
                    extent = new Rect(LayoutOrigin.X, LayoutOrigin.Y, desiredSize.Width, desiredSize.Height);

                    // Clear auto recycle candidate elements that have not been kept alive by layout - i.e layout did not
                    // call GetElementAt(index).
                    foreach (var element in Children)
                    {
                        var virtInfo = GetVirtualizationInfo(element);

                        if (virtInfo.Owner == ElementOwner.Layout &&
                            virtInfo.AutoRecycleCandidate &&
                            !virtInfo.KeepAlive)
                        {
                            Logger.TryGet(LogEventLevel.Verbose, "Repeater")?.Log(this, "AutoClear - {Index}", virtInfo.Index);
                            ClearElementImpl(element);
                        }
                    }
                }

                _viewportManager.SetLayoutExtent(extent);
                return desiredSize;
            }
            finally
            {
                _isLayoutInProgress = false;
            }
        }

        /// <inheritdoc />
        protected override Size ArrangeOverride(Size finalSize)
        {
            if (_isLayoutInProgress)
            {
                throw new AvaloniaInternalException("Reentrancy detected during layout.");
            }

            if (IsProcessingCollectionChange)
            {
                throw new NotSupportedException("Cannot run layout in the middle of a collection change.");
            }

            _isLayoutInProgress = true;

            try
            {
                var arrangeSize = Layout?.Arrange(LayoutContext, finalSize) ?? default;

                // The view manager might clear elements during this call.
                // That's why we call it before arranging cleared elements
                // off screen.
                _viewManager.OnOwnerArranged();

                foreach (var element in Children)
                {
                    var virtInfo = GetVirtualizationInfo(element);
                    virtInfo.KeepAlive = false;

                    if (virtInfo.Owner == ElementOwner.ElementFactory ||
                        virtInfo.Owner == ElementOwner.PinnedPool)
                    {
                        // Toss it away. And arrange it with size 0 so that XYFocus won't use it.
                        element.Arrange(new Rect(
                            ClearedElementsArrangePosition.X - element.DesiredSize.Width,
                            ClearedElementsArrangePosition.Y - element.DesiredSize.Height,
                            0,
                            0));
                    }
                    else
                    {
                        var newBounds = element.Bounds;
                        virtInfo.ArrangeBounds = newBounds;

                        _viewportManager.RegisterScrollAnchorCandidate(element, virtInfo);
                    }
                }

                _viewportManager.OnOwnerArranged();

                return arrangeSize;
            }
            finally
            {
                _isLayoutInProgress = false;
            }
        }

        /// <inheritdoc />
        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            InvalidateMeasure();
            _viewportManager.ResetScrollers();
        }

        /// <inheritdoc />
        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            _viewportManager.ResetScrollers();
        }

        /// <inheritdoc />
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            if (change.Property == ItemsSourceProperty)
            {
                var (oldEnumerable, newEnumerable) = change.GetOldAndNewValue<IEnumerable?>();

                if (oldEnumerable != newEnumerable)
                {
                    var newDataSource = newEnumerable as ItemsSourceView;
                    if (newEnumerable != null && newDataSource == null)
                    {
                        newDataSource = ItemsSourceView.GetOrCreate(newEnumerable);
                    }

                    OnDataSourcePropertyChanged(ItemsSourceView, newDataSource);
                }
            }
            else if (change.Property == ItemTemplateProperty)
            {
                var (oldvalue, newValue) = change.GetOldAndNewValue<IDataTemplate?>();
                OnItemTemplateChanged(oldvalue, newValue);
            }
            else if (change.Property == LayoutProperty)
            {
                var (oldvalue, newValue) = change.GetOldAndNewValue<AttachedLayout>();
                OnLayoutChanged(oldvalue, newValue);
            }
            else if (change.Property == HorizontalCacheLengthProperty)
            {
                _viewportManager.HorizontalCacheLength = change.GetNewValue<double>();
            }
            else if (change.Property == VerticalCacheLengthProperty)
            {
                _viewportManager.VerticalCacheLength = change.GetNewValue<double>();
            }

            base.OnPropertyChanged(change);
        }

        internal Control GetElementImpl(int index, bool forceCreate, bool suppressAutoRecycle)
        {
            var element = _viewManager.GetElement(index, forceCreate, suppressAutoRecycle);
            return element;
        }

        internal void ClearElementImpl(Control element)
        {
            // Clearing an element due to a collection change
            // is more strict in that pinned elements will be forcibly
            // unpinned and sent back to the view generator.
            var isClearedDueToCollectionChange =
                _processingItemsSourceChange != null &&
                (_processingItemsSourceChange.Action == NotifyCollectionChangedAction.Remove ||
                    _processingItemsSourceChange.Action == NotifyCollectionChangedAction.Replace ||
                    _processingItemsSourceChange.Action == NotifyCollectionChangedAction.Reset);

            _viewManager.ClearElement(element, isClearedDueToCollectionChange);
            _viewportManager.OnElementCleared(element, GetVirtualizationInfo(element));
        }

        private int GetElementIndexImpl(Control element)
        {
            // Verify that element is actually a child of this ItemsRepeater
            var parent = element.GetVisualParent();
            
            if (parent == this)
            {
                var virtInfo = TryGetVirtualizationInfo(element);
                return _viewManager.GetElementIndex(virtInfo);
            }

            return -1;
        }

        private Control? GetElementFromIndexImpl(int index)
        {
            Control? result = null;

            var children = Children;
            for (var i = 0; i < children.Count && result == null; ++i)
            {
                var element = children[i];
                var virtInfo = TryGetVirtualizationInfo(element);
                if (virtInfo?.IsRealized == true && virtInfo.Index == index)
                {
                    result = element;
                }
            }

            return result;
        }

        private Control GetOrCreateElementImpl(int index)
        {
            if (index >= 0 && index >= (ItemsSourceView?.Count ?? 0))
            {
                throw new ArgumentException("Argument index is invalid.", nameof(index));
            }

            if (_isLayoutInProgress)
            {
                throw new NotSupportedException("GetOrCreateElement invocation is not allowed during layout.");
            }

            var element = GetElementFromIndexImpl(index);
            bool isAnchorOutsideRealizedRange = element == null;

            if (isAnchorOutsideRealizedRange)
            {
                if (Layout == null)
                {
                    throw new InvalidOperationException("Cannot make an Anchor when there is no attached layout.");
                }

                element = (Control)LayoutContext.GetOrCreateElementAt(index);
                element.Measure(Size.Infinity);
            }

            _viewportManager.OnMakeAnchor(element, isAnchorOutsideRealizedRange);
            InvalidateMeasure();

            return element!;
        }

        internal void OnElementPrepared(Control element, VirtualizationInfo virtInfo)
        {
            var index = virtInfo.Index;

            _viewportManager.OnElementPrepared(element, virtInfo);

            if (ElementPrepared != null)
            {

                if (_elementPreparedArgs == null)
                {
                    _elementPreparedArgs = new ItemsRepeaterElementPreparedEventArgs(element, index);
                }
                else
                {
                    _elementPreparedArgs.Update(element, index);
                }

                ElementPrepared(this, _elementPreparedArgs);
            }

            _childIndexChanged?.Invoke(this, new ChildIndexChangedEventArgs(element, index));
        }

        internal void OnElementClearing(Control element)
        {
            if (ElementClearing != null)
            {
                if (_elementClearingArgs == null)
                {
                    _elementClearingArgs = new ItemsRepeaterElementClearingEventArgs(element);
                }
                else
                {
                    _elementClearingArgs.Update(element);
                }

                ElementClearing(this, _elementClearingArgs);
            }

            _childIndexChanged?.Invoke(this, new ChildIndexChangedEventArgs(element, -1));
        }

        internal void OnElementIndexChanged(Control element, int oldIndex, int newIndex)
        {
            if (ElementIndexChanged != null)
            {
                if (_elementIndexChangedArgs == null)
                {
                    _elementIndexChangedArgs = new ItemsRepeaterElementIndexChangedEventArgs(element, oldIndex, newIndex);
                }
                else
                {
                    _elementIndexChangedArgs.Update(element, oldIndex, newIndex);
                }

                ElementIndexChanged(this, _elementIndexChangedArgs);
            }

            _childIndexChanged?.Invoke(this, new ChildIndexChangedEventArgs(element, newIndex));
        }

        private void OnDataSourcePropertyChanged(ItemsSourceView? oldValue, ItemsSourceView? newValue)
        {
            if (_isLayoutInProgress)
            {
                throw new AvaloniaInternalException("Cannot set ItemsSourceView during layout.");
            }

            if (oldValue != null)
            {
                oldValue.CollectionChanged -= OnItemsSourceViewChanged;
            }

            ItemsSourceView = newValue;

            if (newValue != null)
            {
                newValue.CollectionChanged += OnItemsSourceViewChanged;
            }

            if (Layout != null)
            {
                var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);

                try
                {
                    _processingItemsSourceChange = args;

                    if (Layout is VirtualizingLayout virtualLayout)
                    {
                        virtualLayout.OnItemsChanged(LayoutContext, newValue, args);
                    }
                    else if (Layout is NonVirtualizingLayout)
                    {
                        // Walk through all the elements and make sure they are cleared for
                        // non-virtualizing layouts.
                        foreach (var element in Children)
                        {
                            if (GetVirtualizationInfo(element).IsRealized)
                            {
                                ClearElementImpl(element);
                            }
                        }

                        Children.Clear();
                    }
                }
                finally
                {
                    _processingItemsSourceChange = null;
                }

                InvalidateMeasure();
            }
        }

        private void OnItemTemplateChanged(IDataTemplate? oldValue, IDataTemplate? newValue)
        {
            if (_isLayoutInProgress && oldValue != null)
            {
                throw new AvaloniaInternalException("ItemTemplate cannot be changed during layout.");
            }

            // Since the ItemTemplate has changed, we need to re-evaluate all the items that
            // have already been created and are now in the tree. The easiest way to do that
            // would be to do a reset.. Note that this has to be done before we change the template
            // so that the cleared elements go back into the old template.
            if (Layout != null)
            {
                if (Layout is VirtualizingLayout virtualLayout)
                {
                    var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
                    _processingItemsSourceChange = args;

                    try
                    {
                        virtualLayout.OnItemsChanged(LayoutContext, newValue, args);
                    }
                    finally
                    {
                        _processingItemsSourceChange = null;
                    }
                }
                else if (Layout is NonVirtualizingLayout)
                {
                    // Walk through all the elements and make sure they are cleared for
                    // non-virtualizing layouts.
                    foreach (var element in Children)
                    {
                        if (GetVirtualizationInfo(element).IsRealized)
                        {
                            ClearElementImpl(element);
                        }
                    }
                }
            }

            ItemTemplateShim = newValue switch
            {
                IElementFactory factory => factory,
                null => null,
                _ => new ItemTemplateWrapper(newValue)
            };

            InvalidateMeasure();
        }

        private void OnLayoutChanged(AttachedLayout? oldValue, AttachedLayout? newValue)
        {
            if (_isLayoutInProgress)
            {
                throw new InvalidOperationException("Layout cannot be changed during layout.");
            }

            _viewManager.OnLayoutChanging();

            if (oldValue != null)
            {
                oldValue.UninitializeForContext(LayoutContext);

                AttachedLayout.MeasureInvalidatedWeakEvent.Unsubscribe(oldValue, _layoutWeakSubscriber);
                AttachedLayout.ArrangeInvalidatedWeakEvent.Unsubscribe(oldValue, _layoutWeakSubscriber);

                // Walk through all the elements and make sure they are cleared
                foreach (var element in Children)
                {
                    if (GetVirtualizationInfo(element).IsRealized)
                    {
                        ClearElementImpl(element);
                    }
                }

                LayoutState = null;
            }

            if (newValue != null)
            {
                newValue.InitializeForContext(LayoutContext);

                AttachedLayout.MeasureInvalidatedWeakEvent.Subscribe(newValue, _layoutWeakSubscriber);
                AttachedLayout.ArrangeInvalidatedWeakEvent.Subscribe(newValue, _layoutWeakSubscriber);
            }

            bool isVirtualizingLayout = newValue is VirtualizingLayout;
            _viewportManager.OnLayoutChanged(isVirtualizingLayout);
            InvalidateMeasure();
        }

        private void OnItemsSourceViewChanged(object? sender, NotifyCollectionChangedEventArgs args)
        {
            if (_isLayoutInProgress)
            {
                // Bad things will follow if the data changes while we are in the middle of a layout pass.
                throw new InvalidOperationException("Changes in data source are not allowed during layout.");
            }

            if (IsProcessingCollectionChange)
            {
                throw new InvalidOperationException("Changes in the data source are not allowed during another change in the data source.");
            }

            _processingItemsSourceChange = args;

            try
            {
                _viewManager.OnItemsSourceChanged(sender, args);

                if (Layout != null)
                {
                    if (Layout is VirtualizingLayout virtualLayout)
                    {
                        virtualLayout.OnItemsChanged(LayoutContext, sender, args);
                    }
                    else
                    {
                        // NonVirtualizingLayout
                        InvalidateMeasure();
                    }
                }
            }
            finally
            {
                _processingItemsSourceChange = null;
            }
        }

        private void OnRequestBringIntoView(RequestBringIntoViewEventArgs e)
        {
            _viewportManager.OnBringIntoViewRequested(e);
        }
    }
}
