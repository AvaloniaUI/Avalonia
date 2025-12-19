using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Utils;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Reactive;
using Avalonia.Utilities;
using Avalonia.VisualTree;

namespace Avalonia.Controls
{
    /// <summary>
    /// Arranges and virtualizes content on a single line that is oriented either horizontally or vertically.
    /// </summary>
    public class VirtualizingStackPanel : VirtualizingPanel, IScrollSnapPointsInfo
    {
        /// <summary>
        /// Defines the <see cref="Orientation"/> property.
        /// </summary>
        public static readonly StyledProperty<Orientation> OrientationProperty =
            StackPanel.OrientationProperty.AddOwner<VirtualizingStackPanel>();

        /// <summary>
        /// Defines the <see cref="AreHorizontalSnapPointsRegular"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> AreHorizontalSnapPointsRegularProperty =
            AvaloniaProperty.Register<VirtualizingStackPanel, bool>(nameof(AreHorizontalSnapPointsRegular));

        /// <summary>
        /// Defines the <see cref="AreVerticalSnapPointsRegular"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> AreVerticalSnapPointsRegularProperty =
            AvaloniaProperty.Register<VirtualizingStackPanel, bool>(nameof(AreVerticalSnapPointsRegular));

        /// <summary>
        /// Defines the <see cref="HorizontalSnapPointsChanged"/> event.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> HorizontalSnapPointsChangedEvent =
            RoutedEvent.Register<VirtualizingStackPanel, RoutedEventArgs>(
                nameof(HorizontalSnapPointsChanged),
                RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="VerticalSnapPointsChanged"/> event.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> VerticalSnapPointsChangedEvent =
            RoutedEvent.Register<VirtualizingStackPanel, RoutedEventArgs>(
                nameof(VerticalSnapPointsChanged),
                RoutingStrategies.Bubble);
        /// <summary>
        /// Defines the <see cref="CacheLength"/> property.
        /// </summary>
        public static readonly StyledProperty<double> CacheLengthProperty =
            AvaloniaProperty.Register<VirtualizingStackPanel, double>(nameof(CacheLength), 0.0,
                validate: v => v is >= 0 and <= 2);

        /// <summary>
        /// Gets or sets whether container warmup is enabled.
        /// When enabled, containers are pre-created during initialization to improve first-scroll performance.
        /// Default: false (opt-in).
        /// </summary>
        public static readonly StyledProperty<bool> EnableWarmupProperty =
            AvaloniaProperty.Register<VirtualizingStackPanel, bool>(
                nameof(EnableWarmup),
                defaultValue: false);

        /// <summary>
        /// Gets or sets the number of items to sample for template discovery during warmup.
        /// Higher values discover more template types but take longer to analyze.
        /// Default: 50 items.
        /// </summary>
        public static readonly StyledProperty<int> WarmupSampleSizeProperty =
            AvaloniaProperty.Register<VirtualizingStackPanel, int>(
                nameof(WarmupSampleSize),
                defaultValue: 50,
                validate: v => v > 0 && v <= 1000);

        private static readonly AttachedProperty<object?> RecycleKeyProperty =
            AvaloniaProperty.RegisterAttached<VirtualizingStackPanel, Control, object?>("RecycleKey");

        private static readonly object s_itemIsItsOwnContainer = new object();
        private readonly Action<Control, int> _recycleElement;
        private readonly Action<Control> _recycleElementOnItemRemoved;
        private readonly Action<Control, int, int> _updateElementIndex;
        private int _scrollToIndex = -1;
        private Control? _scrollToElement;
        private bool _isInLayout;
        private bool _isWaitingForViewportUpdate;
        private double _lastEstimatedElementSizeU = 25;
        private RealizedStackElements? _measureElements;
        private RealizedStackElements? _realizedElements;
        private IScrollAnchorProvider? _scrollAnchorProvider;
        private Rect _viewport;
        private Dictionary<object, List<Control>>? _recyclePool;
        private Control? _focusedElement;
        private int _focusedIndex = -1;
        private Control? _realizingElement;
        private int _realizingIndex = -1;
        private double _bufferFactor;
        private bool _isWarmupComplete = false;

        private bool _hasReachedStart = false;
        private bool _hasReachedEnd = false;
        private Rect _extendedViewport;
        private Rect _lastMeasuredViewport;
        private bool _suppressScrollIntoView = false;  // Suppress ScrollIntoView after Reset

        // Viewport anchor tracking for scroll jump prevention
        private int _viewportAnchorIndex = -1;        // Index of first visible item
        private double _viewportAnchorU = double.NaN;  // Absolute position of anchor item
        private double _lastMeasuredExtentU = 0;       // Previous extent for delta calculation

        // Track realized range used for last estimate to avoid redundant re-estimation
        private int _lastEstimateFirstIndex = -1;
        private int _lastEstimateLastIndex = -1;

        public bool IsTracingEnabled
        {
            get => GetValue(IsTracingEnabledProperty);
            set => SetValue(IsTracingEnabledProperty, value);
        }
        
        /// <summary>
        /// Defines the <see cref="IsTracingEnabled"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsTracingEnabledProperty =
            AvaloniaProperty.Register<VirtualizingStackPanel,bool>(
                nameof(IsTracingEnabled));

        static VirtualizingStackPanel()
        {
            CacheLengthProperty.Changed.AddClassHandler<VirtualizingStackPanel>((x, e) => x.OnCacheLengthChanged(e));
        }

        public VirtualizingStackPanel()
        {
            _recycleElement = RecycleElement;
            _recycleElementOnItemRemoved = RecycleElementOnItemRemoved;
            _updateElementIndex = UpdateElementIndex;

            _bufferFactor = Math.Max(0, CacheLength);
            EffectiveViewportChanged += OnEffectiveViewportChanged;
        }

        /// <summary>
        /// Gets or sets the axis along which items are laid out.
        /// </summary>
        /// <value>
        /// One of the enumeration values that specifies the axis along which items are laid out.
        /// The default is Vertical.
        /// </value>
        public Orientation Orientation
        {
            get => GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }

        /// <summary>
        /// Occurs when the measurements for horizontal snap points change.
        /// </summary>
        public event EventHandler<RoutedEventArgs>? HorizontalSnapPointsChanged
        {
            add => AddHandler(HorizontalSnapPointsChangedEvent, value);
            remove => RemoveHandler(HorizontalSnapPointsChangedEvent, value);
        }

        /// <summary>
        /// Occurs when the measurements for vertical snap points change.
        /// </summary>
        public event EventHandler<RoutedEventArgs>? VerticalSnapPointsChanged
        {
            add => AddHandler(VerticalSnapPointsChangedEvent, value);
            remove => RemoveHandler(VerticalSnapPointsChangedEvent, value);
        }

        /// <summary>
        /// Gets or sets whether the horizontal snap points for the <see cref="VirtualizingStackPanel"/> are equidistant from each other.
        /// </summary>
        public bool AreHorizontalSnapPointsRegular
        {
            get => GetValue(AreHorizontalSnapPointsRegularProperty);
            set => SetValue(AreHorizontalSnapPointsRegularProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the vertical snap points for the <see cref="VirtualizingStackPanel"/> are equidistant from each other.
        /// </summary>
        public bool AreVerticalSnapPointsRegular
        {
            get => GetValue(AreVerticalSnapPointsRegularProperty);
            set => SetValue(AreVerticalSnapPointsRegularProperty, value);
        }

        /// <summary>
        /// Gets or sets the CacheLength.
        /// </summary>
        /// <remarks>The factor determines how much additional space to maintain above and below the viewport.
        /// A value of 0.5 means half the viewport size will be buffered on each side (up-down or left-right)
        /// This uses more memory as more UI elements are realized, but greatly reduces the number of Measure-Arrange
        /// cycles which can cause heavy GC pressure depending on the complexity of the item layouts.
        /// </remarks>
        public double CacheLength
        {
            get => GetValue(CacheLengthProperty);
            set => SetValue(CacheLengthProperty, value);
        }

        /// <summary>
        /// Gets or sets whether container warmup is enabled.
        /// When enabled, containers are pre-created during initialization to improve first-scroll performance.
        /// </summary>
        public bool EnableWarmup
        {
            get => GetValue(EnableWarmupProperty);
            set => SetValue(EnableWarmupProperty, value);
        }

        /// <summary>
        /// Gets or sets the number of items to sample for template discovery during warmup.
        /// Higher values discover more template types but take longer to analyze.
        /// </summary>
        public int WarmupSampleSize
        {
            get => GetValue(WarmupSampleSizeProperty);
            set => SetValue(WarmupSampleSizeProperty, value);
        }

        /// <summary>
        /// Gets the index of the first realized element, or -1 if no elements are realized.
        /// </summary>
        public int FirstRealizedIndex => _realizedElements?.FirstIndex ?? -1;

        /// <summary>
        /// Gets the index of the last realized element, or -1 if no elements are realized.
        /// </summary>
        public int LastRealizedIndex => _realizedElements?.LastIndex ?? -1;

        /// <summary>
        /// Returns the viewport that contains any visible elements
        /// </summary>
        internal Rect ViewPort => _viewport;

        /// <summary>
        /// Returns the extended viewport that contains any visible elements and the additional elements for fast scrolling (viewport * CacheLength * 2)
        /// </summary>
        internal Rect ExtendedViewPort => _extendedViewport;

        protected override Size MeasureOverride(Size availableSize)
        {
            var items = Items;

            if (items.Count == 0)
                return default;

            var orientation = Orientation;

            System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled,
                $"\n[VSP-MEASURE] ╔════════════════════ MEASURE PASS START ════════════════════");
            System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled,
                $"[VSP-MEASURE] Viewport: {_viewport}, AvailableSize: {availableSize}");

            // If we're bringing an item into view, ignore any layout passes until we receive a new
            // effective viewport.
            if (_isWaitingForViewportUpdate)
            {
                System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled,
                    $"[VSP-MEASURE] Waiting for viewport update, returning estimate");
                return EstimateDesiredSize(orientation, items.Count);
            }

            _isInLayout = true;

            try
            {
                _realizedElements?.ValidateStartU(Orientation);
                _realizedElements ??= new();
                _measureElements ??= new();

                // Capture viewport anchor before measuring to enable extent compensation
                CaptureViewportAnchor(orientation);

                // We handle horizontal and vertical layouts here so X and Y are abstracted to:
                // - Horizontal layouts: U = horizontal, V = vertical
                // - Vertical layouts: U = vertical, V = horizontal
                var viewport = CalculateMeasureViewport(orientation, items);

                // Track the extended viewport we're measuring with to prevent redundant invalidations
                _lastMeasuredViewport = _extendedViewport;

                // If the viewport is disjunct then we can recycle everything.
                if (viewport.viewportIsDisjunct)
                {
                    System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled, $"[VSP] RECYCLING ALL - HasReachedEnd={_hasReachedEnd}, HasReachedStart={_hasReachedStart}, ItemCount={items.Count}");
                    _realizedElements.RecycleAllElements(_recycleElement);
                }

                // Do the measure, creating/recycling elements as necessary to fill the viewport. Don't
                // write to _realizedElements yet, only _measureElements.
                RealizeElements(items, availableSize, ref viewport);

                // Now swap the measureElements and realizedElements collection.
                (_measureElements, _realizedElements) = (_realizedElements, _measureElements);
                _measureElements.ResetForReuse();

                // Calculate estimate from NEWLY measured elements for contextually-accurate extent calculation.
                // This eliminates temporal mismatch where old viewport data was used to estimate new viewport.
                _ = EstimateElementSizeU();

                // If there is a focused element is outside the visible viewport (i.e.
                // _focusedElement is non-null), ensure it's measured.
                _focusedElement?.Measure(availableSize);

                var desiredSize = CalculateDesiredSize(orientation, items.Count, viewport);

                // Compensate for extent changes to prevent scroll jumping
                CompensateForExtentChange(orientation, desiredSize);

                System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled,
                    $"[VSP-MEASURE] DesiredSize: {desiredSize}, " +
                    $"Realized: [{_realizedElements?.FirstIndex ?? -1}-{_realizedElements?.LastIndex ?? -1}]");
                System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled,
                    $"[VSP-MEASURE] ╚════════════════════ MEASURE PASS END ════════════════════\n");

                return desiredSize;
            }
            finally
            {
                _isInLayout = false;
                // Don't clear _suppressScrollIntoView here - it will be cleared when extent stabilizes
            }
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (_realizedElements is null)
                return default;

            _isInLayout = true;

            try
            {
                var orientation = Orientation;
                var u = _realizedElements!.StartU;

                for (var i = 0; i < _realizedElements.Count; ++i)
                {
                    var e = _realizedElements.Elements[i];

                    if (e is not null)
                    {
                        var sizeU = _realizedElements.SizeU[i];
                        var rect = orientation == Orientation.Horizontal ?
                            new Rect(u, 0, sizeU, finalSize.Height) :
                            new Rect(0, u, finalSize.Width, sizeU);

                        e.Arrange(rect);
                    
                        if (_viewport.Intersects(rect))
                            _scrollAnchorProvider?.RegisterAnchorCandidate(e);
                        
                        u += orientation == Orientation.Horizontal ? rect.Width : rect.Height;
                    }
                }

                // Ensure that the focused element is in the correct position.
                if (_focusedElement is not null && _focusedIndex >= 0)
                {
                    u = GetOrEstimateElementU(_focusedIndex);
                    var rect = orientation == Orientation.Horizontal ?
                        new Rect(u, 0, _focusedElement.DesiredSize.Width, finalSize.Height) :
                        new Rect(0, u, finalSize.Width, _focusedElement.DesiredSize.Height);

                    _focusedElement.Arrange(rect);
                }

                return finalSize;
            }
            finally
            {
                _isInLayout = false;

                RaiseEvent(new RoutedEventArgs(Orientation == Orientation.Horizontal ? HorizontalSnapPointsChangedEvent : VerticalSnapPointsChangedEvent));
            }
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            _scrollAnchorProvider = this.FindAncestorOfType<IScrollAnchorProvider>();

            // Schedule warmup after initial render if enabled
            if (EnableWarmup && !_isWarmupComplete)
            {
                Threading.Dispatcher.UIThread.Post(PerformWarmup, Threading.DispatcherPriority.Background);
            }
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            _scrollAnchorProvider = null;
        }

        protected override void OnItemsChanged(IReadOnlyList<object?> items, NotifyCollectionChangedEventArgs e)
        {
            InvalidateMeasure();

            // Handle async collection loading - trigger warmup when first items become available
            if (EnableWarmup && !_isWarmupComplete && items.Count > 0 && e.Action == NotifyCollectionChangedAction.Add)
            {
                if (_recyclePool == null || _recyclePool.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled,
                        $"[VSP-WARMUP] First items added to collection, triggering warmup");

                    Threading.Dispatcher.UIThread.Post(PerformWarmup, Threading.DispatcherPriority.Background);
                }
            }

            if (_realizedElements is null)
                return;

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    _realizedElements.ItemsInserted(e.NewStartingIndex, e.NewItems!.Count, _updateElementIndex);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    _realizedElements.ItemsRemoved(e.OldStartingIndex, e.OldItems!.Count, _updateElementIndex, _recycleElementOnItemRemoved);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    _realizedElements.ItemsReplaced(e.OldStartingIndex, e.OldItems!.Count, _recycleElementOnItemRemoved);
                    break;
                case NotifyCollectionChangedAction.Move:
                    if (e.OldStartingIndex < 0)
                    {
                        goto case NotifyCollectionChangedAction.Reset;
                    }

                    _realizedElements.ItemsRemoved(e.OldStartingIndex, e.OldItems!.Count, _updateElementIndex, _recycleElementOnItemRemoved);
                    var insertIndex = e.NewStartingIndex;

                    if (e.NewStartingIndex > e.OldStartingIndex)
                    {
                        insertIndex -= e.OldItems.Count - 1;
                    }

                    _realizedElements.ItemsInserted(insertIndex, e.NewItems!.Count, _updateElementIndex);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    // Try to preserve scroll position during Reset
                    // Strategy: Validate that realized items still exist in the new collection
                    // If they do, keep them realized to maintain scroll stability
                    // If they don't, recycle everything (collection replacement scenario)

                    var shouldPreserveRealizedElements = false;

                    if (_realizedElements.Count > 0)
                    {
                        // Check if any realized items still exist in the new collection
                        var preservedCount = 0;
                        for (var i = 0; i < _realizedElements.Count; i++)
                        {
                            if (_realizedElements.Elements[i] == null)
                                continue;

                            var oldIndex = _realizedElements.FirstIndex + i;
                            if (oldIndex >= 0 && oldIndex < items.Count)
                            {
                                // Check if the item at this index is the same object
                                var element = _realizedElements.Elements[i];
                                var dataContext = (element as IDataContextProvider)?.DataContext;

                                if (dataContext != null && ReferenceEquals(items[oldIndex], dataContext))
                                {
                                    preservedCount++;
                                }
                            }
                        }

                        // If most realized items still exist at same indices, this is likely
                        // an append operation (infinite scroll), not a replacement
                        shouldPreserveRealizedElements = preservedCount > _realizedElements.Count / 2;

                        System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled,
                            $"[VSP-RESET] {preservedCount}/{_realizedElements.Count} realized items still valid, " +
                            $"preserve={shouldPreserveRealizedElements}");
                    }

                    if (shouldPreserveRealizedElements)
                    {
                        // Keep realized elements - they're still valid
                        // The normal virtualization logic will handle any adjustments
                        // Suppress ScrollIntoView to prevent ListBox from interfering with scroll position
                        _suppressScrollIntoView = true;

                        System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled,
                            $"[VSP-RESET] Preserving realized elements for scroll stability");

                        // DON'T reset estimate tracking - realized elements unchanged, estimate still valid
                        // This prevents extent oscillation during infinite scroll
                    }
                    else
                    {
                        // Collection was replaced - recycle everything
                        System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled,
                            $"[VSP-RESET] Collection replaced, recycling all elements");
                        _realizedElements.ItemsReset(_recycleElementOnItemRemoved);

                        // Reset estimate tracking since all elements were recycled
                        _lastEstimateFirstIndex = -1;
                        _lastEstimateLastIndex = -1;
                    }

                    // WARMUP OPTIMIZATION: After reset, clear only obsolete keys and top-up if needed
                    if (EnableWarmup && _isWarmupComplete && !shouldPreserveRealizedElements && items.Count > 0)
                    {
                        // Clear only containers whose keys are no longer in the new collection
                        ClearObsoleteWarmupContainers();

                        // Discover what keys we need now
                        var currentKeys = DiscoverTemplateKeys();

                        // Check if we need to warm up any new keys or top-up existing ones
                        bool needsWarmup = false;
                        foreach (var kvp in currentKeys)
                        {
                            var existingCount = _recyclePool?.TryGetValue(kvp.Key, out var pool) == true
                                ? pool.Count
                                : 0;

                            if (existingCount < kvp.Value)
                            {
                                needsWarmup = true;
                                break;
                            }
                        }

                        if (needsWarmup)
                        {
                            System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled,
                                $"[VSP-WARMUP] Reset detected need for warmup, scheduling background warmup");

                            _isWarmupComplete = false;
                            Threading.Dispatcher.UIThread.Post(PerformWarmup, Threading.DispatcherPriority.Background);
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled,
                                $"[VSP-WARMUP] Reset completed, existing warmup sufficient");
                        }
                    }

                    break;
            }
        }

        protected override void OnItemsControlChanged(ItemsControl? oldValue)
        {
            base.OnItemsControlChanged(oldValue);

            if (oldValue is not null)
                oldValue.PropertyChanged -= OnItemsControlPropertyChanged;
            if (ItemsControl is not null)
                ItemsControl.PropertyChanged += OnItemsControlPropertyChanged;
        }

        protected override IInputElement? GetControl(NavigationDirection direction, IInputElement? from, bool wrap)
        {
            var count = Items.Count;
            var fromControl = from as Control;

            if (count == 0 || 
                (fromControl is null && direction is not NavigationDirection.First and not NavigationDirection.Last))
                return null;

            var horiz = Orientation == Orientation.Horizontal;
            var fromIndex = fromControl != null ? IndexFromContainer(fromControl) : -1;
            var toIndex = fromIndex;

            switch (direction)
            {
                case NavigationDirection.First:
                    toIndex = 0;
                    break;
                case NavigationDirection.Last:
                    toIndex = count - 1;
                    break;
                case NavigationDirection.Next:
                    ++toIndex;
                    break;
                case NavigationDirection.Previous:
                    --toIndex;
                    break;
                case NavigationDirection.Left:
                    if (horiz)
                        --toIndex;
                    break;
                case NavigationDirection.Right:
                    if (horiz)
                        ++toIndex;
                    break;
                case NavigationDirection.Up:
                    if (!horiz)
                        --toIndex;
                    break;
                case NavigationDirection.Down:
                    if (!horiz)
                        ++toIndex;
                    break;
                default:
                    return null;
            }

            if (fromIndex == toIndex)
                return from;

            if (wrap)
            {
                if (toIndex < 0)
                    toIndex = count - 1;
                else if (toIndex >= count)
                    toIndex = 0;
            }

            return ScrollIntoView(toIndex);
        }

        protected internal override IEnumerable<Control>? GetRealizedContainers()
        {
            return _realizedElements?.Elements.Where(x => x is not null)!;
        }

        protected internal override Control? ContainerFromIndex(int index)
        {
            if (index < 0 || index >= Items.Count)
                return null;
            if (_scrollToIndex == index)
                return _scrollToElement;
            if (_focusedIndex == index)
                return _focusedElement;
            if (index == _realizingIndex)
                return _realizingElement;
            if (GetRealizedElement(index) is { } realized)
                return realized;
            if (Items[index] is Control c && c.GetValue(RecycleKeyProperty) == s_itemIsItsOwnContainer)
                return c;
            return null;
        }

        protected internal override int IndexFromContainer(Control container)
        {
            if (container == _scrollToElement)
                return _scrollToIndex;
            if (container == _focusedElement)
                return _focusedIndex;
            if (container == _realizingElement)
                return _realizingIndex;
            return _realizedElements?.GetIndex(container) ?? -1;
        }

        protected internal override Control? ScrollIntoView(int index)
        {
            var items = Items;

            // Always log ScrollIntoView calls to trace what's triggering them
            System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled,
                $"[VSP-SCROLLINTO] ScrollIntoView({index}) called, Realized=[{_realizedElements?.FirstIndex ?? -1}-{_realizedElements?.LastIndex ?? -1}], " +
                $"Suppressed={_suppressScrollIntoView}, InLayout={_isInLayout}");

            if (_isInLayout || index < 0 || index >= items.Count || _realizedElements is null || !IsEffectivelyVisible)
            {
                System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled,
                    $"[VSP-SCROLLINTO] ScrollIntoView({index}) rejected: InLayout={_isInLayout}, ValidIndex={index >= 0 && index < items.Count}, " +
                    $"HasRealized={_realizedElements is not null}, Visible={IsEffectivelyVisible}");
                return null;
            }

            // Suppress ScrollIntoView temporarily after Reset to prevent viewport jumps
            if (_suppressScrollIntoView)
            {
                System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled,
                    $"[VSP-SCROLLINTO] ScrollIntoView({index}) suppressed after Reset");
                return GetRealizedElement(index);
            }

            if (GetRealizedElement(index) is Control element)
            {
                System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled,
                    $"[VSP-SCROLLINTO] ScrollIntoView({index}) - element already realized, calling BringIntoView");
                element.BringIntoView();
                return element;
            }
            else if (this.GetVisualRoot() is ILayoutRoot root)
            {
                System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled,
                    $"[VSP-SCROLLINTO] ScrollIntoView({index}) - element not realized, creating and measuring");

                // Create and measure the element to be brought into view. Store it in a field so that
                // it can be re-used in the layout pass.
                var scrollToElement = GetOrCreateElement(items, index);

                scrollToElement.Measure(Size.Infinity);

                // Get the expected position of the element and put it in place.
                var anchorU = GetOrEstimateElementU(index);
                var rect = Orientation == Orientation.Horizontal ?
                    new Rect(anchorU, 0, scrollToElement.DesiredSize.Width, scrollToElement.DesiredSize.Height) :
                    new Rect(0, anchorU, scrollToElement.DesiredSize.Width, scrollToElement.DesiredSize.Height);
                scrollToElement.Arrange(rect);

                // Store the element and index so that they can be used in the layout pass.
                _scrollToElement = scrollToElement;
                _scrollToIndex = index;

                // If the item being brought into view was added since the last layout pass then
                // our bounds won't be updated, so any containing scroll viewers will not have an
                // updated extent. Do a layout pass to ensure that the containing scroll viewers
                // will be able to scroll the new item into view.
                if (!Bounds.Contains(rect) && !_viewport.Contains(rect))
                {
                    System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled,
                        $"[VSP-SCROLLINTO] Item {index} outside bounds/viewport, executing layout pass");
                    _isWaitingForViewportUpdate = true;
                    root.LayoutManager.ExecuteLayoutPass();
                    _isWaitingForViewportUpdate = false;
                }

                // Try to bring the item into view.
                System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled,
                    $"[VSP-SCROLLINTO] Calling BringIntoView on element {index} at position {anchorU:F2}");
                scrollToElement.BringIntoView();

                // If the viewport does not contain the item to scroll to, set _isWaitingForViewportUpdate:
                // this should cause the following chain of events:
                // - Measure is first done with the old viewport (which will be a no-op, see MeasureOverride)
                // - The viewport is then updated by the layout system which invalidates our measure
                // - Measure is then done with the new viewport.
                var viewportContainsItem = _viewport.Contains(rect);
                System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled,
                    $"[VSP-SCROLLINTO] Viewport {_viewport} contains item rect {rect}: {viewportContainsItem}, " +
                    $"setting _isWaitingForViewportUpdate={!viewportContainsItem}");
                _isWaitingForViewportUpdate = !viewportContainsItem;
                root.LayoutManager.ExecuteLayoutPass();

                // If for some reason the layout system didn't give us a new viewport during the layout, we
                // need to do another layout pass as the one that took place was a no-op.
                if (_isWaitingForViewportUpdate)
                {
                    _isWaitingForViewportUpdate = false;
                    InvalidateMeasure();
                    root.LayoutManager.ExecuteLayoutPass();
                }

                // During the previous BringIntoView, the scroll width extent might have been out of date if
                // elements have different widths. Because of that, the ScrollViewer might not scroll to the correct offset.
                // After the previous BringIntoView, Y offset should be correct and an extra layout pass has been executed,
                // hence the width extent should be correct now, and we can try to scroll again.
                scrollToElement.BringIntoView();

                _scrollToElement = null;
                _scrollToIndex = -1;
                return scrollToElement;
            }

            return null;
        }

        internal IReadOnlyList<Control?> GetRealizedElements()
        {
            return _realizedElements?.Elements ?? Array.Empty<Control>();
        }

        private MeasureViewport CalculateMeasureViewport(Orientation orientation, IReadOnlyList<object?> items)
        {
            Debug.Assert(_realizedElements is not null);

            // Use the extended viewport for calculations
            var viewport = _extendedViewport;

            // Get the viewport in the orientation direction.
            var viewportStart = orientation == Orientation.Horizontal ? viewport.X : viewport.Y;
            var viewportEnd = orientation == Orientation.Horizontal ? viewport.Right : viewport.Bottom;

            // Get or estimate the anchor element from which to start realization. If we are
            // scrolling to an element, use that as the anchor element. Otherwise, estimate the
            // anchor element based on the current viewport.
            int anchorIndex;
            double anchorU;

            if (_scrollToIndex >= 0)
            {
                // Scroll to specific index (e.g., after Reset to preserve position)
                anchorIndex = _scrollToIndex;

                if (_scrollToElement is not null)
                {
                    // Use element's actual position if available
                    anchorU = orientation == Orientation.Horizontal ? _scrollToElement.Bounds.Left : _scrollToElement.Bounds.Top;
                }
                else
                {
                    // Estimate position based on index (e.g., after Reset when no elements realized)
                    anchorU = _scrollToIndex * EstimateElementSizeU();

                    System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled,
                        $"[VSP] Using _scrollToIndex={_scrollToIndex} with estimated position {anchorU:F2}");
                }
            }
            else
            {
                GetOrEstimateAnchorElementForViewport(
                    viewportStart,
                    viewportEnd,
                    items.Count,
                    out anchorIndex,
                    out anchorU);
            }

            // Check if the anchor element is not within the currently realized elements.
            // Use distance-based tolerance for variable-height items to prevent excessive recycling.
            var gapBefore = _realizedElements.FirstIndex - anchorIndex;
            var gapAfter = anchorIndex - _realizedElements.LastIndex;

            // Calculate the actual pixel distance of the gap using estimated element size
            var estimatedSize = EstimateElementSizeU();
            var gapDistanceBefore = gapBefore * estimatedSize;
            var gapDistanceAfter = gapAfter * estimatedSize;

            // Calculate viewport size and buffer tolerance
            var viewportSize = viewportEnd - viewportStart;

            // Allow gaps up to a fraction of the viewport size:
            // - Backward gaps (scrolling up): Allow up to 100% of viewport size
            //   This is typically one buffer's worth and can be realized efficiently
            // - Forward gaps (scrolling down): Allow up to 50% of viewport size
            //   Keep this tighter since forward scrolling is usually faster
            var maxDistanceBefore = viewportSize * 1.0;  // 100% of viewport
            var maxDistanceAfter = viewportSize * 0.5;   // 50% of viewport

            // A gap is only disjunct if BOTH conditions are true:
            // 1. The item count gap exceeds a minimum threshold (2 items)
            // 2. The pixel distance exceeds the viewport-based tolerance
            var disjunct = (gapBefore > 2 && gapDistanceBefore > maxDistanceBefore) ||
                          (gapAfter > 2 && gapDistanceAfter > maxDistanceAfter);

            System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled, $"[VSP] CalculateMeasureViewport: Anchor={anchorIndex} (u={anchorU:F2}), Realized=[{_realizedElements.FirstIndex}-{_realizedElements.LastIndex}] (Count={_realizedElements.Count}), GapBefore={gapBefore} items ({gapDistanceBefore:F0}px/{maxDistanceBefore:F0}px), GapAfter={gapAfter} items ({gapDistanceAfter:F0}px/{maxDistanceAfter:F0}px), Disjunct={disjunct}, ViewportSize={viewportSize:F0}px");
            // // Check if the anchor element is not within the currently realized elements.
            // var disjunct = anchorIndex < _realizedElements.FirstIndex || 
            //                anchorIndex > _realizedElements.LastIndex;

            return new MeasureViewport
            {
                anchorIndex = anchorIndex,
                anchorU = anchorU,
                viewportUStart = viewportStart,
                viewportUEnd = viewportEnd,
                viewportIsDisjunct = disjunct,
            };
        }

        private Size CalculateDesiredSize(Orientation orientation, int itemCount, in MeasureViewport viewport)
        {
            var sizeU = 0.0;
            var sizeV = viewport.measuredV;

            if (viewport.lastIndex >= 0)
            {
                var remaining = itemCount - viewport.lastIndex - 1;
                sizeU = viewport.realizedEndU + (remaining * _lastEstimatedElementSizeU);
            }

            return orientation == Orientation.Horizontal ? new(sizeU, sizeV) : new(sizeV, sizeU);
        }

        private Size EstimateDesiredSize(Orientation orientation, int itemCount)
        {
            if (_scrollToIndex >= 0 && _scrollToElement is not null)
            {
                // We have an element to scroll to, so we can estimate the desired size based on the
                // element's position and the remaining elements.
                var remaining = itemCount - _scrollToIndex - 1;
                var u = orientation == Orientation.Horizontal ? 
                    _scrollToElement.Bounds.Right :
                    _scrollToElement.Bounds.Bottom;
                var sizeU = u + (remaining * _lastEstimatedElementSizeU);
                return orientation == Orientation.Horizontal ? 
                    new(sizeU, DesiredSize.Height) : 
                    new(DesiredSize.Width, sizeU);
            }

            return DesiredSize;
        }

        private double EstimateElementSizeU()
        {
            if (_realizedElements is null)
                return _lastEstimatedElementSizeU;

            // Skip re-estimation if realized range hasn't changed
            // This prevents smoothing convergence over multiple passes when measuring the same elements
            var firstIndex = _realizedElements.FirstIndex;
            var lastIndex = _realizedElements.LastIndex;
            if (firstIndex == _lastEstimateFirstIndex && lastIndex == _lastEstimateLastIndex)
            {
                System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled,
                    $"[VSP-ESTIMATE] Skipping re-estimation: realized range unchanged [{firstIndex}-{lastIndex}]");
                return _lastEstimatedElementSizeU;
            }

            var orientation = Orientation;
            var total = 0.0;
            var divisor = 0.0;

            // Average the desired size of the realized, measured elements.
            foreach (var element in _realizedElements.Elements)
            {
                if (element is null || !element.IsMeasureValid)
                    continue;
                var sizeU = orientation == Orientation.Horizontal ?
                    element.DesiredSize.Width :
                    element.DesiredSize.Height;
                total += sizeU;
                ++divisor;
            }

            // Check we have enough information on which to base our estimate.
            if (divisor == 0 || total == 0)
                return _lastEstimatedElementSizeU;

            var newAverage = total / divisor;

            // Use direct averaging for accurate extent calculation
            // With Phase 1 fix (temporal mismatch eliminated) and "skip re-estimation when range
            // unchanged" optimization, we don't need smoothing for larger samples anymore
            if (_lastEstimatedElementSizeU > 0 && divisor < 5)
            {
                // Apply smoothing only for very small samples (< 5 items) to prevent wild swings
                var smoothingFactor = 0.3;
                var smoothedEstimate = (_lastEstimatedElementSizeU * (1 - smoothingFactor)) +
                                      (newAverage * smoothingFactor);

                System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled,
                    $"[VSP-ESTIMATE] Realized range [{firstIndex}-{lastIndex}]: " +
                    $"avg={newAverage:F2}, smoothed={smoothedEstimate:F2} " +
                    $"(old={_lastEstimatedElementSizeU:F2}, factor={smoothingFactor:F2})");

                // Track the realized range used for this estimate
                _lastEstimateFirstIndex = firstIndex;
                _lastEstimateLastIndex = lastIndex;

                return _lastEstimatedElementSizeU = smoothedEstimate;
            }

            // For larger samples (>= 5), use direct average without smoothing
            // This provides immediate adaptation to new item sizes
            System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled,
                $"[VSP-ESTIMATE] Realized range [{firstIndex}-{lastIndex}]: " +
                $"avg={newAverage:F2} (direct, no smoothing)");

            _lastEstimateFirstIndex = firstIndex;
            _lastEstimateLastIndex = lastIndex;

            return _lastEstimatedElementSizeU = newAverage;
        }

        private void GetOrEstimateAnchorElementForViewport(
            double viewportStartU,
            double viewportEndU,
            int itemCount,
            out int index,
            out double position)
        {
            // We have no elements, or we're at the start of the viewport.
            if (itemCount <= 0 || MathUtilities.IsZero(viewportStartU))
            {
                index = 0;
                position = 0;
                return;
            }

            // If we have realised elements and a valid StartU then try to use this information to
            // get the anchor element.
            if (_realizedElements?.StartU is { } u && !double.IsNaN(u))
            {
                var orientation = Orientation;

                for (var i = 0; i < _realizedElements.Elements.Count; ++i)
                {
                    if (_realizedElements.Elements[i] is not { } element)
                        continue;

                    var sizeU = orientation == Orientation.Horizontal ?
                        element.DesiredSize.Width :
                        element.DesiredSize.Height;
                    var endU = u + sizeU;

                    if (endU > viewportStartU && u < viewportEndU)
                    {
                        index = _realizedElements.FirstIndex + i;
                        position = u;
                        return;
                    }

                    u = endU;
                }
            }

            // We don't have any realized elements in the requested viewport, or can't rely on
            // StartU being valid. Estimate the index using realized element positions if available.
            var estimatedSize = EstimateElementSizeU();

            // If we have realized elements, use their actual positions to improve estimation accuracy.
            // This prevents anchor jumps when scrolling with variable-sized items.
            if (_realizedElements != null && _realizedElements.Count > 0 && _realizedElements.StartU is { } startU && !double.IsNaN(startU))
            {
                var firstIndex = _realizedElements.FirstIndex;
                var lastIndex = _realizedElements.LastIndex;
            
                // If viewport is before realized elements, extrapolate backward from first element
                if (viewportStartU < startU)
                {
                    var distanceBack = startU - viewportStartU;
                    var itemsBack = (int)(distanceBack / estimatedSize);
                    index = Math.Max(0, firstIndex - itemsBack);
                    position = startU - (itemsBack * estimatedSize);
                    System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled, $"[VSP] Anchor estimation: Extrapolating BACKWARD from first realized element. FirstIndex={firstIndex}, StartU={startU:F2}, ItemsBack={itemsBack}, EstimatedAnchor={index}");
                    return;
                }
            
                // If viewport is after realized elements, extrapolate forward from last element
                var lastElementU = _realizedElements.GetElementU(lastIndex);
                if (!double.IsNaN(lastElementU))
                {
                    var lastElementSize = _realizedElements.SizeU[_realizedElements.Count - 1];
                    var lastElementEndU = lastElementU + lastElementSize;
            
                    if (viewportStartU >= lastElementEndU)
                    {
                        var distanceForward = viewportStartU - lastElementEndU;
                        var itemsForward = (int)(distanceForward / estimatedSize);
                        index = Math.Min(lastIndex + 1 + itemsForward, itemCount - 1);
                        position = lastElementEndU + (itemsForward * estimatedSize);
                        System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled, $"[VSP] Anchor estimation: Extrapolating FORWARD from last realized element. LastIndex={lastIndex}, LastEndU={lastElementEndU:F2}, ItemsForward={itemsForward}, EstimatedAnchor={index}");
                        return;
                    }
                }
            }

            // Fallback: No realized elements or unable to extrapolate, use simple estimation
            var startIndex = Math.Min((int)(viewportStartU / estimatedSize), itemCount - 1);
            index = startIndex;
            position = startIndex * estimatedSize;
            System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled, $"[VSP] Anchor estimation: Using SIMPLE estimation (no realized elements). EstimatedSize={estimatedSize:F2}, EstimatedAnchor={index}");
        }

        /// <summary>
        /// Captures the current viewport anchor to enable scroll jump compensation.
        /// The anchor is the first element that intersects the viewport start.
        /// </summary>
        private void CaptureViewportAnchor(Orientation orientation)
        {
            _viewportAnchorIndex = -1;
            _viewportAnchorU = double.NaN;

            if (_realizedElements == null || _realizedElements.Count == 0)
            {
                System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled,
                    $"[VSP-ANCHOR] No realized elements to capture anchor");
                return;
            }

            var viewportStartU = orientation == Orientation.Horizontal ? _viewport.X : _viewport.Y;
            var viewportEndU = orientation == Orientation.Horizontal ? _viewport.Right : _viewport.Bottom;
            var startU = _realizedElements.StartU;

            if (double.IsNaN(startU))
            {
                System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled,
                    $"[VSP-ANCHOR] StartU is NaN (unstable), cannot capture anchor");
                return;
            }

            System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled,
                $"[VSP-ANCHOR] Viewport: [{viewportStartU:F2}, {viewportEndU:F2}], " +
                $"Realized range: [{_realizedElements.FirstIndex}-{_realizedElements.LastIndex}], " +
                $"StartU={startU:F2}, Count={_realizedElements.Count}");

            var u = startU;

            // Find first element that intersects viewport start
            for (var i = 0; i < _realizedElements.Count; i++)
            {
                if (_realizedElements.Elements[i] == null)
                    continue;

                var sizeU = _realizedElements.SizeU[i];
                var elementEndU = u + sizeU;
                var itemIndex = _realizedElements.FirstIndex + i;

                if (elementEndU > viewportStartU && u <= viewportStartU)
                {
                    _viewportAnchorIndex = itemIndex;
                    _viewportAnchorU = u;

                    System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled,
                        $"[VSP-ANCHOR] ✓ Captured anchor: Index={_viewportAnchorIndex}, " +
                        $"U={u:F2}, Size={sizeU:F2}, " +
                        $"Overlap=[{u:F2}, {elementEndU:F2}] ∩ [{viewportStartU:F2}, {viewportEndU:F2}]");
                    return;
                }

                u = elementEndU;
            }

            System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled,
                $"[VSP-ANCHOR] ✗ No anchor found (viewport outside realized range?)");
        }

        /// <summary>
        /// Compensates for extent changes by checking anchor stability.
        /// Relies on Avalonia's built-in scroll anchoring (IScrollAnchorProvider).
        /// </summary>
        private void CompensateForExtentChange(Orientation orientation, Size desiredSize)
        {
            var currentExtentU = orientation == Orientation.Horizontal ?
                desiredSize.Width : desiredSize.Height;

            var isFirstMeasure = MathUtilities.AreClose(_lastMeasuredExtentU, 0);

            if (MathUtilities.AreClose(_lastMeasuredExtentU, currentExtentU))
            {
                _lastMeasuredExtentU = currentExtentU;

                System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled,
                    $"[VSP-EXTENT] No change: Extent={currentExtentU:F2}");
                return;
            }


            var extentDelta = currentExtentU - _lastMeasuredExtentU;
            var previousExtent = _lastMeasuredExtentU;

            if (isFirstMeasure)
            {
                _lastMeasuredExtentU = currentExtentU;
                System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled,
                    $"[VSP-EXTENT] Initial extent: {currentExtentU:F2}");
                return;
            }

            // Detect extreme extent oscillations that can confuse ScrollViewer
            // This happens when we have very few realized items and many unrealized items
            var extentChangeRatio = Math.Abs(extentDelta / previousExtent);
            if (extentChangeRatio > 0.5 && _realizedElements != null)
            {
                var items = Items;
                var realizedCount = _realizedElements.Count;
                var totalCount = items?.Count ?? 0;
                var unrealizedCount = totalCount - realizedCount;

                // If we have less than 10% of items realized and extent changed >50%
                // This indicates estimation instability
                if (realizedCount < totalCount * 0.1 && unrealizedCount > 10)
                {
                    System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled,
                        $"[VSP-EXTENT] ⚠ EXTREME OSCILLATION DETECTED: {extentChangeRatio:P0} change " +
                        $"with only {realizedCount}/{totalCount} items realized");
                    System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled,
                        $"[VSP-EXTENT] → Dampening extent change to prevent ScrollViewer jump");

                    // Dampen the extent change to prevent ScrollViewer from overshooting
                    // Use a weighted average instead of accepting the full change
                    var dampenedExtent = previousExtent + (extentDelta * 0.3);
                    _lastMeasuredExtentU = dampenedExtent;

                    System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled,
                        $"[VSP-EXTENT] Dampened: {previousExtent:F2} → {dampenedExtent:F2} " +
                        $"(instead of {currentExtentU:F2})");
                    return;
                }
            }

            _lastMeasuredExtentU = currentExtentU;

            System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled,
                $"[VSP-EXTENT] ═══════════════════════════════════════════════════");
            System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled,
                $"[VSP-EXTENT] EXTENT CHANGED: {previousExtent:F2} → {currentExtentU:F2} " +
                $"(Δ={extentDelta:F2})");

            if (_viewportAnchorIndex < 0 || double.IsNaN(_viewportAnchorU))
            {
                System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled,
                    $"[VSP-EXTENT] ✗ No anchor to track stability");
                System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled,
                    $"[VSP-EXTENT] ═══════════════════════════════════════════════════");
                return;
            }

            // Check if anchor is still realized
            var currentAnchorU = _realizedElements?.GetElementU(_viewportAnchorIndex);

            if (currentAnchorU == null || double.IsNaN(currentAnchorU.Value))
            {
                System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled,
                    $"[VSP-EXTENT] ✗ Anchor index {_viewportAnchorIndex} no longer realized");
                System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled,
                    $"[VSP-EXTENT] ═══════════════════════════════════════════════════");
                return;
            }

            var anchorDrift = currentAnchorU.Value - _viewportAnchorU;

            System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled,
                $"[VSP-EXTENT] Anchor index: {_viewportAnchorIndex}");
            System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled,
                $"[VSP-EXTENT] Anchor position: Expected={_viewportAnchorU:F2}, " +
                $"Actual={currentAnchorU.Value:F2}, Drift={anchorDrift:F2}");

            if (_realizedElements != null)
            {
                System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled,
                    $"[VSP-EXTENT] Realized range: [{_realizedElements.FirstIndex}-{_realizedElements.LastIndex}], " +
                    $"StartU={_realizedElements.StartU:F2}");
            }

            // CRITICAL: If item 0 is realized at position 0, NEVER apply compensation.
            // Any anchor drift is due to estimation errors in other items, and compensating
            // would incorrectly move item 0 away from its correct position (0).
            if (_realizedElements != null &&
                _realizedElements.FirstIndex == 0 &&
                _realizedElements.StartU is { } startU &&
                !double.IsNaN(startU) &&
                MathUtilities.AreClose(startU, 0))
            {
                // System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled,
                //     $"[VSP-EXTENT] ✓ Item 0 is realized at position 0");
                // System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled,
                //     $"[VSP-EXTENT] → Skipping ALL compensation to preserve item 0 position");
                // System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled,
                //     $"[VSP-EXTENT] → (Anchor drift of {anchorDrift:F2}px accepted as estimation error)");
                return;
            }

            if (MathUtilities.AreClose(anchorDrift, 0))
            {
                System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled,
                    $"[VSP-EXTENT] ✓ Anchor stable (no drift), extent change likely in unrealized items");

                // Anchor is stable - extent changed in unrealized items
                // This is the common case with heterogeneous items
                // The ScrollViewer might still jump due to extent changes, so we rely on
                // IScrollAnchorProvider to maintain the anchor's viewport position
            }
            else
            {
                System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled,
                    $"[VSP-EXTENT] ⚠ Anchor DRIFTED by {anchorDrift:F2}px!");
                System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled,
                    $"[VSP-EXTENT] → Compensating by adjusting StartU");

                // Anchor drifted - this means items BEFORE the anchor changed size
                // Compensate by shifting StartU to restore the anchor's position
                _realizedElements?.CompensateStartU(-anchorDrift);

                System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled,
                    $"[VSP-EXTENT] → StartU adjusted by {-anchorDrift:F2}px to restore anchor position");
            }

            System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled,
                $"[VSP-EXTENT] ═══════════════════════════════════════════════════");
        }

        private double GetOrEstimateElementU(int index)
        {
            // Return the position of the existing element if realized.
            var u = _realizedElements?.GetElementU(index) ?? double.NaN;

            if (!double.IsNaN(u))
                return u;

            // Estimate the element size.
            var estimatedSize = EstimateElementSizeU();

            // TODO: Use _startU to work this out.
            return index * estimatedSize;
        }

        private void RealizeElements(
            IReadOnlyList<object?> items,
            Size availableSize,
            ref MeasureViewport viewport)
        {
            Debug.Assert(_measureElements is not null);
            Debug.Assert(_realizedElements is not null);
            Debug.Assert(items.Count > 0);

            var index = viewport.anchorIndex;
            var horizontal = Orientation == Orientation.Horizontal;
            var u = viewport.anchorU;
                    
            // Reset boundary flags
            _hasReachedStart = false;
            _hasReachedEnd = false;

            // If the anchor element is at the beginning of, or before, the start of the viewport
            // then we can recycle all elements before it.
            if (u <= viewport.anchorU)
                _realizedElements.RecycleElementsBefore(viewport.anchorIndex, _recycleElement);

            // Start at the anchor element and move forwards, realizing elements.
            do
            {
                _realizingIndex = index;
                var e = GetOrCreateElement(items, index);
                _realizingElement = e;

                e.Measure(availableSize);

                var sizeU = horizontal ? e.DesiredSize.Width : e.DesiredSize.Height;
                var sizeV = horizontal ? e.DesiredSize.Height : e.DesiredSize.Width;

                // Pre-emptive fix: Force item 0 to position U=0 to prevent clipping
                // This handles the case when item 0 is the anchor element with wrong estimated position
                if (index == 0 && !MathUtilities.AreClose(u, 0))
                {
                   // System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled,
                   //    $"[VSP-CLIP-FIX] FORWARD LOOP: Item 0 at {u:F2}px, forcing to U=0");
                   u = 0;
                }

                _measureElements!.Add(index, e, u, sizeU);
                viewport.measuredV = Math.Max(viewport.measuredV, sizeV);

                u += sizeU;
                ++index;
                _realizingIndex = -1;
                _realizingElement = null;
            } while (u < viewport.viewportUEnd && index < items.Count);
            
            // Check if we reached the end of the collection
            _hasReachedEnd = index >= items.Count;
            
            // Store the last index and end U position for the desired size calculation.
            viewport.lastIndex = index - 1;
            viewport.realizedEndU = u;

            // We can now recycle elements after the last element.
            _realizedElements.RecycleElementsAfter(viewport.lastIndex, _recycleElement);

            // Next move backwards from the anchor element, realizing elements.
            index = viewport.anchorIndex - 1;
            u = viewport.anchorU;

            System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled,
               $"[VSP-CLIP-FIX] BACKWARD LOOP START: StartIndex={index}, " +
               $"StartU={u:F2}, ViewportStart={viewport.viewportUStart:F2}");

            while (u > viewport.viewportUStart && index >= 0)
            {
                var e = GetOrCreateElement(items, index);
                
                e.Measure(availableSize);
                var sizeU = horizontal ? e.DesiredSize.Width : e.DesiredSize.Height;
                var sizeV = horizontal ? e.DesiredSize.Height : e.DesiredSize.Width;
                u -= sizeU;

                // Pre-emptive fix: Force item 0 to position U=0 to prevent clipping
                if (index == 0)
                {
                   if (!MathUtilities.AreClose(u, 0))
                   {
                      var estimationError = u;
                      System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled,
                         $"[VSP-CLIP-FIX] PRE-EMPTIVE: Item 0 at {u:F2}px " +
                         $"(error={estimationError:F2}px), forcing to U=0");
                      u = 0;
                   }
                   else
                   {
                      System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled,
                         $"[VSP-CLIP-FIX] PRE-EMPTIVE: Item 0 already at U=0 ✓");
                   }
                }

                _measureElements!.Add(index, e, u, sizeU);
                viewport.measuredV = Math.Max(viewport.measuredV, sizeV);
                --index;
            }
            
            // Check if we reached the start of the collection
            _hasReachedStart = index < 0;

            // Log backward loop completion if item 0 was realized
            if (_measureElements.FirstIndex == 0)
            {
               var item0U = _measureElements.StartU;
               System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled,
                  $"[VSP-CLIP-FIX] BACKWARD LOOP COMPLETE: Item 0 at U={item0U:F2}px " +
                  $"(expected=0, error={item0U:F2}px)");
            }

            // If we've reached the start (realized item 0), ensure item 0 is positioned at exactly U=0
            // to prevent the "cut off first item" issue when scrolling up fast
            if (_hasReachedStart && _measureElements.Count > 0 && _measureElements.FirstIndex == 0)
            {
                var firstItemU = _measureElements.StartU;

                // Defensive check: warn if StartU is NaN (should not happen during normal scrolling)
                if (double.IsNaN(firstItemU))
                {
                   // System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled,
                   //    $"[VSP-CLIP-FIX] ⚠ SAFETY NET: StartU is NaN, cannot fix item 0 position!");
                }
                else if (!MathUtilities.AreClose(firstItemU, 0))
                {
                    // Item 0 is not at position 0 - this happens due to estimation errors
                    // Adjust all realized element positions so item 0 starts at exactly 0
                    var adjustment = -firstItemU;

                    // Warn if adjustment is very large (indicates serious estimation error)
                    // System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled && Math.Abs(adjustment) > 100,
                    //    $"[VSP-CLIP-FIX] ⚠ SAFETY NET: Large adjustment {adjustment:F2}px needed for item 0");

                    // System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled,
                    //     $"[VSP-CLIP-FIX] ⚠ SAFETY NET TRIGGERED: Item 0 at {firstItemU:F2}px " +
                    //     $"(pre-emptive fix should have handled this), adjusting by {adjustment:F2}px");

                    // Shift all positions using CompensateStartU
                    _measureElements.CompensateStartU(adjustment);

                    // Also adjust the viewport positions that reference these elements
                    viewport.realizedEndU += adjustment;

                    // Verify the adjustment worked
                    var newStartU = _measureElements.StartU;
                    // System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled && !double.IsNaN(newStartU) && !MathUtilities.AreClose(newStartU, 0),
                    //    $"[VSP-CLIP-FIX] ✗ SAFETY NET FAILED: Item 0 still at {newStartU:F2}px after adjustment!");

                    // System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled,
                    //     $"[VSP-CLIP-FIX] ✓ SAFETY NET: Item 0 successfully positioned at U=0");
                }
                else
                {
                    // System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled,
                    //     $"[VSP-CLIP-FIX] SAFETY NET: Item 0 already at U=0, pre-emptive fix succeeded ✓");
                }
            }

            // We can now recycle elements before the first element.
            _realizedElements.RecycleElementsBefore(index + 1, _recycleElement);
        }

        private Control GetOrCreateElement(IReadOnlyList<object?> items, int index)
        {
            Debug.Assert(ItemContainerGenerator is not null);

            if ((GetRealizedElement(index) ??
                 GetRealizedElement(index, ref _focusedIndex, ref _focusedElement) ??
                 GetRealizedElement(index, ref _scrollToIndex, ref _scrollToElement)) is { } realized)
                return realized;

            var item = items[index];
            var generator = ItemContainerGenerator!;

            if (generator.NeedsContainer(item, index, out var recycleKey))
            {
                return GetRecycledElement(item, index, recycleKey) ??
                       CreateElement(item, index, recycleKey);
            }
            else
            {
                return GetItemAsOwnContainer(item, index);
            }
        }

        private Control? GetRealizedElement(int index)
        {
            return _realizedElements?.GetElement(index);
        }
        
        private static Control? GetRealizedElement(
            int index,
            ref int specialIndex,
            ref Control? specialElement)
        {
            if (specialIndex == index)
            {
                Debug.Assert(specialElement is not null);

                var result = specialElement;
                specialIndex = -1;
                specialElement = null;
                return result;
            }

            return null;
        }

        private Control GetItemAsOwnContainer(object? item, int index)
        {
            Debug.Assert(ItemContainerGenerator is not null);

            var controlItem = (Control)item!;
            var generator = ItemContainerGenerator!;

            if (!controlItem.IsSet(RecycleKeyProperty))
            {
                generator.PrepareItemContainer(controlItem, controlItem, index);
                AddInternalChild(controlItem);
                controlItem.SetValue(RecycleKeyProperty, s_itemIsItsOwnContainer);
                generator.ItemContainerPrepared(controlItem, item, index);
            }

            controlItem.SetCurrentValue(Visual.IsVisibleProperty, true);
            return controlItem;
        }

        private Control? GetRecycledElement(object? item, int index, object? recycleKey)
        {
            Debug.Assert(ItemContainerGenerator is not null);

            if (recycleKey is null)
                return null;

            var generator = ItemContainerGenerator!;

            if (_recyclePool?.TryGetValue(recycleKey, out var recyclePool) == true && recyclePool.Count > 0)
            {
                // edge case: The item is already datacontext of a recyclable item
                var recycleIndex = recyclePool.Count - 1;
                for (int i = 0; i < recyclePool.Count; i++)
                {
                    if (recyclePool[i].DataContext == item)
                    {
                        recycleIndex = i;
                        break;
                    }
                }

                var recycled = recyclePool[recycleIndex];
                recyclePool.RemoveAt(recycleIndex);
                recycled.SetCurrentValue(Visual.IsVisibleProperty, true);
                generator.PrepareItemContainer(recycled, item, index);
                generator.ItemContainerPrepared(recycled, item, index);
                return recycled;
            }

            return null;
        }

        private Control CreateElement(object? item, int index, object? recycleKey)
        {
            Debug.Assert(ItemContainerGenerator is not null);

            var generator = ItemContainerGenerator!;
            var container = generator.CreateContainer(item, index, recycleKey);

            container.SetValue(RecycleKeyProperty, recycleKey);
            generator.PrepareItemContainer(container, item, index);
            AddInternalChild(container);
            generator.ItemContainerPrepared(container, item, index);

            return container;
        }

        private void RecycleElement(Control element, int index)
        {
            Debug.Assert(ItemsControl is not null);
            Debug.Assert(ItemContainerGenerator is not null);
            
            _scrollAnchorProvider?.UnregisterAnchorCandidate(element);

            var recycleKey = element.GetValue(RecycleKeyProperty);

            if (recycleKey is null)
            {
                RemoveInternalChild(element);
            }
            else if (recycleKey == s_itemIsItsOwnContainer)
            {
                element.SetCurrentValue(Visual.IsVisibleProperty, false);
            }
            else if (KeyboardNavigation.GetTabOnceActiveElement(ItemsControl) == element)
            {
                _focusedElement = element;
                _focusedIndex = index;
            }
            else
            {
                ItemContainerGenerator!.ClearItemContainer(element);
                PushToRecyclePool(recycleKey, element);
                element.SetCurrentValue(Visual.IsVisibleProperty, false);
            }
        }

        private void RecycleElementOnItemRemoved(Control element)
        {
            Debug.Assert(ItemContainerGenerator is not null);

            _scrollAnchorProvider?.UnregisterAnchorCandidate(element);

            var recycleKey = element.GetValue(RecycleKeyProperty);
            
            if (recycleKey is null || recycleKey == s_itemIsItsOwnContainer)
            {
                RemoveInternalChild(element);
            }
            else
            {
                ItemContainerGenerator!.ClearItemContainer(element);
                PushToRecyclePool(recycleKey, element);
                element.SetCurrentValue(Visual.IsVisibleProperty, false);
            }
        }

        private void PushToRecyclePool(object recycleKey, Control element)
        {
            _recyclePool ??= new();

            if (!_recyclePool.TryGetValue(recycleKey, out var pool))
            {
                pool = new();
                _recyclePool.Add(recycleKey, pool);
            }

            // respect max poolsize per key of IVirtualizingDataTemplate
            if (ItemsControl?.ItemTemplate is Templates.IVirtualizingDataTemplate vdt &&
                pool.Count >= vdt.MaxPoolSizePerKey)
                return;
            
            pool.Add(element);
        }

        private void UpdateElementIndex(Control element, int oldIndex, int newIndex)
        {
            Debug.Assert(ItemContainerGenerator is not null);

            ItemContainerGenerator.ItemContainerIndexChanged(element, oldIndex, newIndex);
        }
        
        private void OnEffectiveViewportChanged(object? sender, EffectiveViewportChangedEventArgs e)
        {
            var vertical = Orientation == Orientation.Vertical;
            var oldViewportStart = vertical ? _viewport.Top : _viewport.Left;
            var oldViewportEnd = vertical ? _viewport.Bottom : _viewport.Right;
            var oldExtendedViewportStart = vertical ? _extendedViewport.Top : _extendedViewport.Left;
            var oldExtendedViewportEnd = vertical ? _extendedViewport.Bottom : _extendedViewport.Right;

            // Update current viewport
            _viewport = e.EffectiveViewport.Intersect(new(Bounds.Size));
            _isWaitingForViewportUpdate = false;

            // Calculate buffer sizes based on viewport dimensions
            var viewportSize = vertical ? _viewport.Height : _viewport.Width;
            var bufferSize = viewportSize * _bufferFactor;
            
            // Calculate extended viewport with relative buffers
            var extendedViewportStart = vertical ? 
                Math.Max(0, _viewport.Top - bufferSize) : 
                Math.Max(0, _viewport.Left - bufferSize);
                
            var extendedViewportEnd = vertical ? 
                Math.Min(Bounds.Height, _viewport.Bottom + bufferSize) : 
                Math.Min(Bounds.Width, _viewport.Right + bufferSize);

            // special case:
            // If we are at the start of the list, append 2 * CacheLength additional items
            // If we are at the end of the list, prepend 2 * CacheLength additional items
            // - this way we always maintain "2 * CacheLength * element" items. 
            if (vertical)
            {
                var spaceAbove = _viewport.Top - bufferSize;
                var spaceBelow = Bounds.Height - (_viewport.Bottom + bufferSize);
                
                if (spaceAbove < 0 && spaceBelow >= 0)
                    extendedViewportEnd = Math.Min(Bounds.Height, extendedViewportEnd + Math.Abs(spaceAbove));
                if (spaceAbove >= 0 && spaceBelow < 0)
                    extendedViewportStart = Math.Max(0, extendedViewportStart - Math.Abs(spaceBelow));
            }
            else
            {
                var spaceLeft = _viewport.Left - bufferSize;
                var spaceRight = Bounds.Width - (_viewport.Right + bufferSize);
                
                if (spaceLeft < 0 && spaceRight >= 0)
                    extendedViewportEnd = Math.Min(Bounds.Width, extendedViewportEnd + Math.Abs(spaceLeft));
                if(spaceLeft >= 0 && spaceRight < 0)
                    extendedViewportStart = Math.Max(0, extendedViewportStart - Math.Abs(spaceRight));
            }

            Rect extendedViewPort;
            if (vertical)
            {
                extendedViewPort = new Rect(
                    _viewport.X, 
                    extendedViewportStart,
                    _viewport.Width,
                    extendedViewportEnd - extendedViewportStart);
            }
            else
            {
                extendedViewPort = new Rect(
                    extendedViewportStart,
                    _viewport.Y,
                    extendedViewportEnd - extendedViewportStart,
                    _viewport.Height);
            }

            // Determine if we need a new measure
            var newViewportStart = vertical ? _viewport.Top : _viewport.Left;
            var newViewportEnd = vertical ? _viewport.Bottom : _viewport.Right;
            var newExtendedViewportStart = vertical ? extendedViewPort.Top : extendedViewPort.Left;
            var newExtendedViewportEnd = vertical ? extendedViewPort.Bottom : extendedViewPort.Right;

            var needsMeasure = false;
            
           
            // Case 1: Viewport has changed significantly
            if (!MathUtilities.AreClose(oldViewportStart, newViewportStart) ||
                !MathUtilities.AreClose(oldViewportEnd, newViewportEnd))
            {
                // Case 1a: The new viewport exceeds the old extended viewport
                if (newViewportStart < oldExtendedViewportStart || 
                    newViewportEnd > oldExtendedViewportEnd)
                {
                    needsMeasure = true;
                }
                // Case 1b: The extended viewport has changed significantly
                else if (!MathUtilities.AreClose(oldExtendedViewportStart, newExtendedViewportStart) ||
                         !MathUtilities.AreClose(oldExtendedViewportEnd, newExtendedViewportEnd))
                {
                    // Check if we're about to scroll into an area where we don't have realized elements
                    // This would be the case if we're near the edge of our current extended viewport
                    var nearingEdge = false;
                    
                    if (_realizedElements != null)
                    {
                        var firstRealizedElementU = _realizedElements.StartU;
                        var lastRealizedElementU = _realizedElements.StartU;
                        
                        for (var i = 0; i < _realizedElements.Count; i++)
                        {
                            lastRealizedElementU += _realizedElements.SizeU[i];
                        }
                        
                        // If scrolling up/left and nearing the top/left edge of realized elements
                        if (newViewportStart < oldViewportStart && 
                            newViewportStart - newExtendedViewportStart < bufferSize)
                        {
                            // Edge case: We're at item 0 with excess measurement space.
                            // Skip re-measuring since we're at the list start and it won't change the result.
                            // This prevents redundant Measure-Arrange cycles when at list beginning.
                            nearingEdge = !_hasReachedStart;
                        }
                        
                        // If scrolling down/right and nearing the bottom/right edge of realized elements
                        if (newViewportEnd > oldViewportEnd && 
                            newExtendedViewportEnd - newViewportEnd < bufferSize)
                        {
                            // Edge case: We're at the last item with excess measurement space.
                            // Skip re-measuring since we're at the list end and it won't change the result.
                            // This prevents redundant Measure-Arrange cycles when at list beginning.
                            nearingEdge = !_hasReachedEnd;
                        }
                    }
                    else
                    {
                        nearingEdge = true;
                    }
                    
                    needsMeasure = nearingEdge;
                }
            }

            System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled, $"[VSP] OnEffectiveViewportChanged: NeedsMeasure={needsMeasure}, HasReachedStart={_hasReachedStart}, HasReachedEnd={_hasReachedEnd}");
            System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled, $"[VSP]   OldViewport=[{oldViewportStart:F2}-{oldViewportEnd:F2}], NewViewport=[{newViewportStart:F2}-{newViewportEnd:F2}]");
            System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled, $"[VSP]   OldExtended=[{oldExtendedViewportStart:F2}-{oldExtendedViewportEnd:F2}], NewExtended=[{newExtendedViewportStart:F2}-{newExtendedViewportEnd:F2}]");

            if (needsMeasure)
            {
                // Check if we're already measuring with this viewport (or very close to it)
                // This prevents layout cycles during fast scrolling where viewport shifts slightly
                // as heterogeneous items are measured
                if (_isInLayout &&
                    MathUtilities.AreClose(_lastMeasuredViewport.X, extendedViewPort.X) &&
                    MathUtilities.AreClose(_lastMeasuredViewport.Y, extendedViewPort.Y) &&
                    MathUtilities.AreClose(_lastMeasuredViewport.Width, extendedViewPort.Width) &&
                    MathUtilities.AreClose(_lastMeasuredViewport.Height, extendedViewPort.Height))
                {
                    // We're already measuring with this viewport - don't invalidate again
                    System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled,
                        $"[VSP] OnEffectiveViewportChanged: Skipping InvalidateMeasure (already measuring this viewport)");
                    _extendedViewport = extendedViewPort;
                    return;
                }

                // Always update the extended viewport to prevent stale comparisons in the next
                // OnEffectiveViewportChanged call, which would cause repeated layout cycles.
                _extendedViewport = extendedViewPort;
                InvalidateMeasure();
            }

        }

        private void OnItemsControlPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (_focusedElement is not null &&
                e.Property == KeyboardNavigation.TabOnceActiveElementProperty &&
                e.GetOldValue<IInputElement?>() == _focusedElement)
            {
                // TabOnceActiveElement has moved away from _focusedElement so we can recycle it.
                RecycleElement(_focusedElement, _focusedIndex);
                _focusedElement = null;
                _focusedIndex = -1;
            }

            // Handle ItemTemplate changes - invalidate warmup and re-trigger if enabled
            if (e.Property == ItemsControl.ItemTemplateProperty)
            {
                if (EnableWarmup && _isWarmupComplete)
                {
                    System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled,
                        $"[VSP-WARMUP] ItemTemplate changed, clearing warmup containers");

                    ClearWarmupContainers();
                    _isWarmupComplete = false;

                    Threading.Dispatcher.UIThread.Post(PerformWarmup, Threading.DispatcherPriority.Background);
                }
            }
        }

        /// <summary>
        /// Clears unused warmup containers from the recycle pool.
        /// Only removes containers that haven't been used yet (null DataContext and invisible).
        /// </summary>
        private void ClearWarmupContainers()
        {
            if (_recyclePool == null)
                return;

            int clearedCount = 0;

            foreach (var pool in _recyclePool.Values)
            {
                for (int i = pool.Count - 1; i >= 0; i--)
                {
                    var container = pool[i];
                    if (container.DataContext == null && !container.IsVisible)
                    {
                        RemoveInternalChild(container);
                        pool.RemoveAt(i);
                        clearedCount++;
                    }
                }
            }

            System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled,
                $"[VSP-WARMUP] Cleared {clearedCount} unused warmup containers");
        }

        /// <summary>
        /// Clears only obsolete warmup containers from the recycle pool.
        /// Preserves containers whose recycleKey is still active in the current collection.
        /// </summary>
        private void ClearObsoleteWarmupContainers()
        {
            if (_recyclePool == null)
                return;

            // Get currently needed keys from the new collection
            var activeKeys = new HashSet<object>(DiscoverTemplateKeys().Keys);

            var keysToRemove = new List<object>();
            int clearedCount = 0;

            foreach (var kvp in _recyclePool)
            {
                var recycleKey = kvp.Key;
                var pool = kvp.Value;

                // Only clear pools for obsolete keys (not in new collection)
                if (!activeKeys.Contains(recycleKey))
                {
                    for (int i = pool.Count - 1; i >= 0; i--)
                    {
                        var container = pool[i];
                        if (container.DataContext == null && !container.IsVisible)
                        {
                            RemoveInternalChild(container);
                            pool.RemoveAt(i);
                            clearedCount++;
                        }
                    }

                    if (pool.Count == 0)
                        keysToRemove.Add(recycleKey);
                }
            }

            // Remove empty pools
            foreach (var key in keysToRemove)
                _recyclePool.Remove(key);

            System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled,
                $"[VSP-WARMUP] Cleared {clearedCount} obsolete containers for {keysToRemove.Count} unused keys");
        }

        private void OnCacheLengthChanged(AvaloniaPropertyChangedEventArgs e)
        {
            var newValue = e.GetNewValue<double>();
            _bufferFactor = newValue;

            // Force a recalculation of the extended viewport on the next layout pass
            InvalidateMeasure();
        }

        /// <summary>
        /// Discovers unique template types/keys by sampling items from the collection.
        /// Returns a dictionary mapping recycle keys to target warmup counts.
        /// </summary>
        private Dictionary<object, int> DiscoverTemplateKeys()
        {
            var templateKeys = new Dictionary<object, int>();
            var items = Items;

            if (items == null || items.Count == 0 || ItemContainerGenerator == null)
                return templateKeys;

            System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled,
                $"[VSP-WARMUP] Starting template discovery, ItemCount={items.Count}");

            // Sample first N items to discover template types
            int sampleSize = Math.Min(WarmupSampleSize, items.Count);

            for (int i = 0; i < sampleSize; i++)
            {
                var item = items[i];

                // Use ItemContainerGenerator to determine recycle key without creating container
                if (ItemContainerGenerator.NeedsContainer(item, i, out var recycleKey) && recycleKey != null)
                {
                    if (!templateKeys.ContainsKey(recycleKey))
                    {
                        templateKeys[recycleKey] = 0;
                    }
                    templateKeys[recycleKey]++;
                }
            }

            // Query MaxPoolSizePerKey from IVirtualizingDataTemplate if available
            if (ItemsControl?.ItemTemplate is Templates.IVirtualizingDataTemplate vdt)
            {
                foreach (var key in templateKeys.Keys.ToList())
                {
                    templateKeys[key] = vdt.MinPoolSizePerKey;
                }

                System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled,
                    $"[VSP-WARMUP] Using MaxPoolSizePerKey={vdt.MinPoolSizePerKey} from IVirtualizingDataTemplate");
            }
            else
            {
                // Default to 3 containers per type if no MaxPoolSizePerKey available
                foreach (var key in templateKeys.Keys.ToList())
                {
                    templateKeys[key] = 3;
                }
            }

            System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled,
                $"[VSP-WARMUP] Discovered {templateKeys.Count} template types from {sampleSize} items");

            return templateKeys;
        }

        /// <summary>
        /// Pre-creates containers with their content for each discovered template type.
        /// Containers are stored in the recycle pool with their Child controls already attached,
        /// ready to be reused during scrolling. This eliminates the expensive template instantiation
        /// cost during the first scroll.
        /// </summary>
        private void PerformWarmup()
        {
            if (_isWarmupComplete || Items == null || Items.Count == 0)
                return;

            System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled,
                $"[VSP-WARMUP] Starting warmup");

            var startTime = System.Diagnostics.Stopwatch.StartNew();
            var templateKeys = DiscoverTemplateKeys();

            if (templateKeys.Count == 0)
            {
                System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled,
                    $"[VSP-WARMUP] No templates discovered, skipping warmup");
                _isWarmupComplete = true;
                return;
            }

            _recyclePool ??= new Dictionary<object, List<Control>>();
            
            var orientation = Orientation;
            var availableSize = orientation == Orientation.Horizontal
                ? new Size(double.PositiveInfinity, Bounds.Height > 0 ? Bounds.Height : _lastEstimatedElementSizeU)
                : new Size(Bounds.Width > 0 ? Bounds.Width : double.PositiveInfinity, double.PositiveInfinity);
            
            int totalCreated = 0;

            int alreadyRealized = _realizedElements?.Elements?.Count ?? 0;
            Dictionary<object, List<Control?>> realizedElementsLookup = new();
            if(_realizedElements is { Elements: not null } realizedElements)
            {
                realizedElementsLookup = realizedElements.Elements.Where(re => re != null)
                    .GroupBy(re => re!.GetValue(RecycleKeyProperty))
                    .ToDictionary(g => g.Key??new object(), g => g.ToList());
                
            }

            foreach (var kvp in templateKeys)
            {
                var recycleKey = kvp.Key;
                var targetCount = kvp.Value;

                // OPTIMIZATION: Check existing pool size
                var existingCount = _recyclePool.TryGetValue(recycleKey, out var existingPool)
                    ? existingPool.Count
                    : 0;
                // OPTIMIZATION 2: Check realized elements
                if (realizedElementsLookup.TryGetValue(recycleKey, out var realized))
                    existingCount += realized.Count;

                var neededCount = Math.Max(0, targetCount - existingCount);

                if (neededCount == 0)
                {
                    System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled,
                        $"[VSP-WARMUP] Pool for {recycleKey} already has {existingCount}/{targetCount} containers, skipping");
                    continue;
                }

                System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled,
                    $"[VSP-WARMUP] Creating {neededCount} containers for key: {recycleKey} " +
                    $"(existing: {existingCount}, target: {targetCount})");

                // Collect actual items from the ItemsSource that match this recycle key
                // CHANGED: Only collect neededCount items, not targetCount
                var matchingItems = new List<(object item, int index)>();
                var startIndex = Math.Max(alreadyRealized - 1, 0);
                for (int i = startIndex; i < Math.Min(WarmupSampleSize+alreadyRealized, Items.Count); i++)
                {
                    var item = Items[i];
                    if (ItemContainerGenerator!.NeedsContainer(item, i, out var key) &&
                        Equals(key, recycleKey) && item is not null)
                    {
                        matchingItems.Add((item, i));
                        if (matchingItems.Count >= neededCount)  // CHANGED: from targetCount
                            break;
                    }
                }

                if (matchingItems.Count == 0)
                    continue;

                // Create containers for real items (but only those not yet realized)
                for (int i = 0; i < matchingItems.Count; i++)
                {
                    var (item, index) = matchingItems[i];

                    try
                    {
                        // Create container with real item - this creates the Container + Child together
                        // PrepareContainerForItemOverride is called, which creates the Child control
                        var container = CreateElement(item, index, recycleKey);
                        
                        // Pre-measure with typical available size to cache layout
                        container.Measure(availableSize);
                        
                        // IMPORTANT: Do NOT clear the container!
                        // The Child control should stay attached with its template instantiated.
                        // When reused, only the data binding will update (cheap operation).

                        // Push to recycle pool - container + child are pooled together
                        PushToRecyclePool(recycleKey, container);
                        container.SetCurrentValue(Visual.IsVisibleProperty, false);

                        totalCreated++;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled,
                            $"[VSP-WARMUP] Error creating container for key {recycleKey}: {ex.Message}");
                        break;
                    }
                }
            }

            _isWarmupComplete = true;
            startTime.Stop();

            System.Diagnostics.Debug.WriteLineIf(IsTracingEnabled,
                $"[VSP-WARMUP] Completed: {templateKeys.Count} template types, " +
                $"{totalCreated} containers pre-created in {startTime.ElapsedMilliseconds}ms");
        }

        /// <inheritdoc/>
        public IReadOnlyList<double> GetIrregularSnapPoints(Orientation orientation, SnapPointsAlignment snapPointsAlignment)
        {
            if(_realizedElements == null)
                return new List<double>();

            return new VirtualizingSnapPointsList(_realizedElements, ItemsControl?.ItemsSource?.Count() ?? 0, orientation, Orientation, snapPointsAlignment, EstimateElementSizeU());
        }

        /// <inheritdoc/>
        public double GetRegularSnapPoints(Orientation orientation, SnapPointsAlignment snapPointsAlignment, out double offset)
        {
            offset = 0f;
            var firstRealizedChild = _realizedElements?.Elements.FirstOrDefault();

            if (firstRealizedChild == null)
            {
                return 0;
            }

            double snapPoint = 0;

            switch (Orientation)
            {
                case Orientation.Horizontal:
                    if (!AreHorizontalSnapPointsRegular)
                        throw new InvalidOperationException();

                    snapPoint = firstRealizedChild.Bounds.Width;
                    switch (snapPointsAlignment)
                    {
                        case SnapPointsAlignment.Near:
                            offset = 0;
                            break;
                        case SnapPointsAlignment.Center:
                            offset = (firstRealizedChild.Bounds.Right - firstRealizedChild.Bounds.Left) / 2;
                            break;
                        case SnapPointsAlignment.Far:
                            offset = firstRealizedChild.Bounds.Width;
                            break;
                    }
                    break;
                case Orientation.Vertical:
                    if (!AreVerticalSnapPointsRegular)
                        throw new InvalidOperationException();
                    snapPoint = firstRealizedChild.Bounds.Height;
                    switch (snapPointsAlignment)
                    {
                        case SnapPointsAlignment.Near:
                            offset = 0;
                            break;
                        case SnapPointsAlignment.Center:
                            offset = (firstRealizedChild.Bounds.Bottom - firstRealizedChild.Bounds.Top) / 2;
                            break;
                        case SnapPointsAlignment.Far:
                            offset = firstRealizedChild.Bounds.Height;
                            break;
                    }
                    break;
            }

            return snapPoint;
        }

        private struct MeasureViewport
        {
            public int anchorIndex;
            public double anchorU;
            public double viewportUStart;
            public double viewportUEnd;
            public double measuredV;
            public double realizedEndU;
            public int lastIndex;
            public bool viewportIsDisjunct;
        }
    }
}
