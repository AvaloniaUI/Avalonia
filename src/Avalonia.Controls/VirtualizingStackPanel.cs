using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Utils;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Logging;
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

        /// <summary>
        /// Exposes the recycle pool for unit testing.
        /// </summary>
        internal IReadOnlyDictionary<object, List<Control>>? RecyclePoolForTesting => _recyclePool;

        private Control? _focusedElement;
        private int _focusedIndex = -1;
        private Control? _realizingElement;
        private int _realizingIndex = -1;
        private double _bufferFactor;
        private bool _isWarmupComplete = false;

        private bool _hasReachedStart = false;
        private bool _hasReachedEnd = false;
        private Rect _lastMeasuredViewport;
        private bool _suppressScrollIntoView = false;  // Suppress ScrollIntoView after Reset
        private Rect _lastMeasuredExtendedViewport;
        private Rect _lastKnownExtendedViewport;

        // Viewport anchor tracking for scroll jump prevention
        private int _viewportAnchorIndex = -1;        // Index of first visible item
        private double _viewportAnchorU = double.NaN;  // Absolute position of anchor item
        private double _lastMeasuredExtentU = 0;       // Previous extent for delta calculation

        // Track realized range used for last estimate to avoid redundant re-estimation
        private int _lastEstimateFirstIndex = -1;
        private int _lastEstimateLastIndex = -1;

        // Cache for CaptureViewportAnchor to avoid redundant O(n) scans
        private double _lastCapturedViewportStart = double.NaN;

        // Retained containers for smart reuse during disjunct recycle
        private Dictionary<object, (Control element, int oldIndex, double sizeU)>? _retainedForReuse;

        // Suppress ValidateStartU after it fires once, until Arrange completes.
        // Complex controls can produce non-deterministic Measure results (>1px variation),
        // causing ValidateStartU to fire every pass and create an infinite layout cycle.
        private bool _suppressValidateStartU;

        // Layout cycle prevention: counts consecutive MeasureOverride calls without an
        // intervening ArrangeOverride. Reset in ArrangeOverride, OnEffectiveViewportChanged
        // (when needsMeasure=true), and OnItemsChanged. Used by the cycle breaker to
        // short-circuit MeasureOverride after the first pass.
        private int _consecutiveMeasureCount;
        private bool _measurePostponed;

        // Extent oscillation detection: tracks alternating extent changes (up/down/up)
        // that indicate a non-deterministic measurement loop. When detected, freeze the
        // reported extent to stop the ScrollViewer's scroll anchor from drifting the
        // viewport. The freeze is permanent (only cleared on OnItemsChanged) because
        // unfreezing restarts the oscillation. A convergence mechanism updates the frozen
        // value toward reality when the actual extent stabilizes.
        private int _extentOscillationSign;   // +1 or -1: direction of last extent delta
        private int _extentOscillationCount;  // consecutive direction reversals
        private double _frozenExtentU = double.NaN; // locked extent when oscillation detected (NaN = not frozen)
        private double _frozenLastActualExtentU; // last actual extent seen while frozen (for convergence)
        private int _frozenStableCount;         // consecutive passes where actual extent is stable while frozen

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
        internal Rect LastMeasuredExtendedViewPort => _lastMeasuredExtendedViewport;

        protected override Size MeasureOverride(Size availableSize)
        {
            var items = Items;

            if (items.Count == 0)
                return default;

            var orientation = Orientation;

            _consecutiveMeasureCount++;

            // If we're bringing an item into view, ignore any layout passes until we receive a new
            // effective viewport.
            if (_isWaitingForViewportUpdate)
            {
                return EstimateDesiredSize(orientation, items.Count);
            }

            // Break layout cycles: after 1 full measure pass, return the previous DesiredSize
            // without doing any work. Complex controls (async image loading, text wrapping,
            // deferred bindings) can produce non-deterministic Measure results, causing:
            //   extent oscillation → parent re-measures VSP → different sizes → repeat forever.
            // One pass suffices for legitimate layout work. The counter is reset in
            // ArrangeOverride, OnEffectiveViewportChanged, and OnItemsChanged.
            if (_consecutiveMeasureCount > 1 && DesiredSize != default)
            {
                if (!_measurePostponed)
                {
                    _measurePostponed = true;
                    // Use Loaded priority (higher than Background) so the deferred measure
                    // runs before the next input/scroll event. Background priority risked
                    // the measure firing after the user scrolled further, causing the size
                    // change to be applied at the wrong scroll position (scroll jump).
                    Threading.Dispatcher.UIThread.Post(() =>
                    {
                        _measurePostponed = false;
                        _consecutiveMeasureCount = 0;
                        InvalidateMeasure();
                    }, Threading.DispatcherPriority.Loaded);

                }
                return DesiredSize;
            }

            _isInLayout = true;

            try
            {
                _realizedElements ??= new();
                _measureElements ??= new();

                // Capture viewport anchor BEFORE ValidateStartU so we know which items
                // are before/after the visible area for scroll position compensation.
                CaptureViewportAnchor(orientation);

                // ValidateStartU checks whether realized elements' DesiredSize still matches
                // stored sizes. If a genuine resize occurred (>= 1px), it updates stored sizes
                // in-place and compensates StartU for items before the viewport anchor.
                // This prevents scroll jumping when async content (e.g. images) loads and
                // changes item heights.
                // After firing once, suppress until Arrange to prevent repeated instability.
                double sizeChangeDelta = 0;
                var lockSizes = !double.IsNaN(_frozenExtentU);
                var validateFired = !_suppressValidateStartU &&
                    _realizedElements.ValidateStartU(orientation, _viewportAnchorIndex, out sizeChangeDelta, lockSizes);
                var startUAfterValidate = _realizedElements.StartU;
                if (validateFired)
                {
                    // Only reset estimate cache when startU became unstable (NaN) —
                    // i.e., a genuine uniform resize that requires full re-estimation.
                    // When items change but startU stays stable (async loading, single
                    // item oscillation), preserving the estimate prevents wild extent
                    // swings that cause the ScrollViewer to drift.
                    if (double.IsNaN(startUAfterValidate))
                    {
                        _lastEstimateFirstIndex = -1;
                        _lastEstimateLastIndex = -1;
                    }
                    _suppressValidateStartU = true;
                }

                // We handle horizontal and vertical layouts here so X and Y are abstracted to:
                // - Horizontal layouts: U = horizontal, V = vertical
                // - Vertical layouts: U = vertical, V = horizontal
                // Note: capture _scrollToIndex before CalculateMeasureViewport/RealizeElements
                // clears it via GetRealizedElement.
                var isScrollingToElement = _scrollToIndex >= 0;
                var viewport = CalculateMeasureViewport(orientation, items);

                // Track the extended viewport we're measuring with to prevent redundant invalidations
                _lastMeasuredViewport = _lastMeasuredExtendedViewport;

                // If the viewport is disjunct then we can recycle everything.
                // First, retain containers whose DataContext matches items in the new viewport
                // so they can be reused without full PrepareItemContainer + Measure overhead.
                if (viewport.viewportIsDisjunct)
                {
                    var estimatedSize = EstimateElementSizeU();
                    var viewportSize = viewport.viewportUEnd - viewport.viewportUStart;
                    var estimatedCount = estimatedSize > 0
                        ? (int)Math.Ceiling(viewportSize / estimatedSize) + 1
                        : 10;
                    RetainMatchingContainers(items, viewport.anchorIndex,
                        viewport.anchorIndex + estimatedCount);
                    _realizedElements!.RecycleAllElements(_recycleElement);
                }

                // Do the measure, creating/recycling elements as necessary to fill the viewport. Don't
                // write to _realizedElements yet, only _measureElements.
                RealizeElements(items, availableSize, ref viewport);

                // Recycle any retained containers that weren't reused during realization
                RecycleUnusedRetainedContainers();

                // Now swap the measureElements and realizedElements collection.
                (_measureElements, _realizedElements) = (_realizedElements, _measureElements);
                _measureElements!.ResetForReuse();

                // Calculate estimate from NEWLY measured elements for contextually-accurate extent calculation.
                // This eliminates temporal mismatch where old viewport data was used to estimate new viewport.
                _ = EstimateElementSizeU();

                // If there is a focused element is outside the visible viewport (i.e.
                // _focusedElement is non-null), ensure it's measured.
                _focusedElement?.Measure(availableSize);

                var desiredSize = CalculateDesiredSize(orientation, items.Count, viewport);

                // Compensate for extent changes to prevent scroll jumping.
                // Skip during ScrollIntoView - the anchor position is intentionally estimated
                // and compensation would incorrectly shift it.
                // Also skip when ValidateStartU marked startU as unstable (NaN) — positions
                // were re-estimated from scratch and compensation would undo the correction.
                // The scroll anchor mechanism will adjust the viewport in a subsequent pass.
                var startUWasUnstable = validateFired && double.IsNaN(_realizedElements!.StartU) == false
                    && double.IsNaN(startUAfterValidate);
                if (!isScrollingToElement && !startUWasUnstable)
                    CompensateForExtentChange(orientation, desiredSize);
                else if (startUWasUnstable)
                {
                    // Update extent tracking so next pass has correct baseline
                    _lastMeasuredExtentU = orientation == Orientation.Horizontal
                        ? desiredSize.Width : desiredSize.Height;
                }

                // When extent is frozen due to oscillation, report the frozen extent
                // to the ScrollViewer. This prevents the scroll anchor mechanism from
                // adjusting the offset in response to oscillating extent values.
                if (!double.IsNaN(_frozenExtentU))
                {
                    desiredSize = orientation == Orientation.Horizontal
                        ? new Size(_frozenExtentU, desiredSize.Height)
                        : new Size(desiredSize.Width, _frozenExtentU);
                }

                return desiredSize;
            }
            finally
            {
                _isInLayout = false;
                _suppressScrollIntoView = false;
            }
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (_realizedElements is null)
                return default;

            _isInLayout = true;
            _consecutiveMeasureCount = 0;  // Reset: arrange means we're not in a tight measure loop
            _measurePostponed = false;
            _suppressValidateStartU = false;  // Allow ValidateStartU to check again after arrange

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
                    
                        if (e.IsVisible && _viewport.Intersects(rect))
                        {
                            try
                            {
                                _scrollAnchorProvider?.RegisterAnchorCandidate(e);
                            }
                            catch (InvalidOperationException ex)
                            {
                                // Element might have been removed/reparented during virtualization; ignore but log for diagnostics.
                                Logger.TryGet(LogEventLevel.Verbose, LogArea.Layout)?.Log(this,
                                    "RegisterAnchorCandidate ignored for {Element}: not a descendant of ScrollAnchorProvider. {Message}",
                                    e, ex.Message);
                            }
                        }
                        
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
            _lastCapturedViewportStart = double.NaN;
            _consecutiveMeasureCount = 0;  // Allow fresh passes for the new items
            _measurePostponed = false;
            _frozenExtentU = double.NaN;
            _extentOscillationSign = 0;
            _extentOscillationCount = 0;
            _frozenStableCount = 0;
            InvalidateMeasure();

            // Handle async collection loading - trigger warmup when first items become available
            if (EnableWarmup && !_isWarmupComplete && items.Count > 0 && e.Action == NotifyCollectionChangedAction.Add)
            {
                if (_recyclePool == null || _recyclePool.Count == 0)
                {

                    Threading.Dispatcher.UIThread.Post(PerformWarmup, Threading.DispatcherPriority.Background);
                }
            }

            // Always update special elements (focused, scroll-to) on collection changes
            UpdateSpecialElementsOnItemsChanged(e);

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
                        insertIndex -= e.OldItems!.Count - 1;
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

                    }

                    if (shouldPreserveRealizedElements)
                    {
                        // Keep realized elements - they're still valid
                        // The normal virtualization logic will handle any adjustments
                        // Suppress ScrollIntoView to prevent ListBox from interfering with scroll position
                        _suppressScrollIntoView = true;

                        // DON'T reset estimate tracking - realized elements unchanged, estimate still valid
                        // This prevents extent oscillation during infinite scroll
                    }
                    else
                    {
                        // Collection was replaced or reordered - recycle everything.
                        // First, retain containers whose DataContext matches items in the
                        // estimated viewport so they can be reused without full re-prepare.

                        if (items.Count > 0 && _realizedElements.Count > 0)
                        {
                            var orientation = Orientation;
                            var vpStart = orientation == Orientation.Horizontal ? _viewport.X : _viewport.Y;
                            var vpEnd = orientation == Orientation.Horizontal ? _viewport.Right : _viewport.Bottom;
                            var estSize = _lastEstimatedElementSizeU;
                            var startIdx = estSize > 0 ? Math.Max(0, (int)(vpStart / estSize)) : 0;
                            var endIdx = estSize > 0
                                ? Math.Min(items.Count, (int)Math.Ceiling(vpEnd / estSize) + 1)
                                : Math.Min(items.Count, 20);
                            RetainMatchingContainers(items, startIdx, endIdx);
                        }

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

                            _isWarmupComplete = false;
                            Threading.Dispatcher.UIThread.Post(PerformWarmup, Threading.DispatcherPriority.Background);
                        }
                    }

                    break;
            }
        }

        private void UpdateSpecialElementsOnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (_focusedElement is not null && e.NewStartingIndex <= _focusedIndex)
                    {
                        var oldIndex = _focusedIndex;
                        _focusedIndex += e.NewItems!.Count;
                        _updateElementIndex(_focusedElement, oldIndex, _focusedIndex);
                    }
                    if (_scrollToElement is not null && e.NewStartingIndex <= _scrollToIndex)
                    {
                        _scrollToIndex += e.NewItems!.Count;
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    if (_focusedElement is not null)
                    {
                        if (e.OldStartingIndex <= _focusedIndex && _focusedIndex < e.OldStartingIndex + e.OldItems!.Count)
                        {
                            RecycleFocusedElement();
                        }
                        else if (e.OldStartingIndex < _focusedIndex)
                        {
                            var oldIndex = _focusedIndex;
                            _focusedIndex -= e.OldItems!.Count;
                            _updateElementIndex(_focusedElement, oldIndex, _focusedIndex);
                        }
                    }
                    if (_scrollToElement is not null)
                    {
                        if (e.OldStartingIndex <= _scrollToIndex && _scrollToIndex < e.OldStartingIndex + e.OldItems!.Count)
                        {
                            RecycleScrollToElement();
                        }
                        else if (e.OldStartingIndex < _scrollToIndex)
                        {
                            _scrollToIndex -= e.OldItems!.Count;
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                    if (_focusedElement is not null && e.OldStartingIndex <= _focusedIndex && _focusedIndex < e.OldStartingIndex + e.OldItems!.Count)
                    {
                        RecycleFocusedElement();
                    }
                    if (_scrollToElement is not null && e.OldStartingIndex <= _scrollToIndex && _scrollToIndex < e.OldStartingIndex + e.OldItems!.Count)
                    {
                        RecycleScrollToElement();
                    }
                    break;
                case NotifyCollectionChangedAction.Move:
                    if (e.OldStartingIndex < 0)
                    {
                        goto case NotifyCollectionChangedAction.Reset;
                    }

                    if (_focusedElement is not null)
                    {
                        if (e.OldStartingIndex <= _focusedIndex && _focusedIndex < e.OldStartingIndex + e.OldItems!.Count)
                        {
                            var oldIndex = _focusedIndex;
                            _focusedIndex = e.NewStartingIndex + (_focusedIndex - e.OldStartingIndex);
                            _updateElementIndex(_focusedElement, oldIndex, _focusedIndex);
                        }
                        else
                        {
                            var newFocusedIndex = _focusedIndex;

                            if (e.OldStartingIndex < _focusedIndex)
                            {
                                newFocusedIndex -= e.OldItems!.Count;
                            }

                            if (e.NewStartingIndex <= newFocusedIndex)
                            {
                                newFocusedIndex += e.NewItems!.Count;
                            }

                            if (newFocusedIndex != _focusedIndex)
                            {
                                var oldIndex = _focusedIndex;
                                _focusedIndex = newFocusedIndex;
                                _updateElementIndex(_focusedElement, oldIndex, _focusedIndex);
                            }
                        }
                    }

                    if (_scrollToElement is not null)
                    {
                        if (e.OldStartingIndex <= _scrollToIndex && _scrollToIndex < e.OldStartingIndex + e.OldItems!.Count)
                        {
                            _scrollToIndex = e.NewStartingIndex + (_scrollToIndex - e.OldStartingIndex);
                        }
                        else
                        {
                            var newScrollToIndex = _scrollToIndex;

                            if (e.OldStartingIndex < _scrollToIndex)
                            {
                                newScrollToIndex -= e.OldItems!.Count;
                            }

                            if (e.NewStartingIndex <= newScrollToIndex)
                            {
                                newScrollToIndex += e.NewItems!.Count;
                            }

                            _scrollToIndex = newScrollToIndex;
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Reset:
                    if (_focusedElement is not null)
                    {
                        RecycleFocusedElement();
                    }
                    if (_scrollToElement is not null)
                    {
                        RecycleScrollToElement();
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

            _realizedElements?.ResetForReuse();
            _measureElements?.ResetForReuse();
            if (ItemsControl is not null && _focusedElement is not null)
            {
                RecycleFocusedElement();
            }
            if (ItemsControl is not null && _scrollToElement is not null)
            {
                RecycleScrollToElement();
            }
            if (ItemsControl is null)
            {
                _focusedElement = null;
                _scrollToElement = null;
            }
            _focusedIndex = -1;
            _scrollToIndex = -1;
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

            if (_isInLayout || index < 0 || index >= items.Count || _realizedElements is null || !IsEffectivelyVisible)
            {
                return null;
            }

            // Suppress ScrollIntoView temporarily after Reset to prevent viewport jumps
            if (_suppressScrollIntoView)
            {
                return GetRealizedElement(index);
            }

            if (GetRealizedElement(index) is Control element)
            {
                element.BringIntoView();
                return element;
            }
            else if (this.GetLayoutRoot() is {} root)
            {
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
                    _isWaitingForViewportUpdate = true;
                    root.LayoutManager.ExecuteLayoutPass();
                    _isWaitingForViewportUpdate = false;
                }

                // Try to bring the item into view.
                scrollToElement.BringIntoView();

                // If the viewport does not contain the item to scroll to, set _isWaitingForViewportUpdate:
                // this should cause the following chain of events:
                // - Measure is first done with the old viewport (which will be a no-op, see MeasureOverride)
                // - The viewport is then updated by the layout system which invalidates our measure
                // - Measure is then done with the new viewport.
                var viewportContainsItem = _viewport.Contains(rect);
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
            var viewport = _lastMeasuredExtendedViewport;

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
            var disjunct = anchorIndex < _realizedElements.FirstIndex ||
                anchorIndex > _realizedElements.LastIndex;

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

            // Skip re-estimation if realized range hasn't changed.
            // This prevents smoothing convergence over multiple passes when measuring the same elements.
            var firstIndex = _realizedElements.FirstIndex;
            var lastIndex = _realizedElements.LastIndex;
            if (firstIndex == _lastEstimateFirstIndex && lastIndex == _lastEstimateLastIndex)
            {
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

            // Apply smoothing when the realized range overlaps significantly with the
            // previous one. This prevents estimate oscillation when an outlier item
            // (e.g., async-loaded image at 292px vs placeholder at 84px) enters/leaves
            // the realized range on alternating passes, which would swing the estimate
            // by ~50% and cause ~2000px extent oscillation.
            // Skip smoothing when the range is mostly new (scrolled to a different region)
            // to allow fast adaptation to genuinely different item sizes.
            var overlapCount = _lastEstimateFirstIndex >= 0
                ? Math.Max(0, Math.Min(lastIndex, _lastEstimateLastIndex) -
                    Math.Max(firstIndex, _lastEstimateFirstIndex) + 1)
                : 0;
            var hasSignificantOverlap = overlapCount > (lastIndex - firstIndex + 1) / 2;
            if (hasSignificantOverlap && _lastEstimatedElementSizeU > 0)
            {
                var smoothingFactor = 0.3;
                var smoothedEstimate = (_lastEstimatedElementSizeU * (1 - smoothingFactor)) +
                                      (newAverage * smoothingFactor);

                _lastEstimateFirstIndex = firstIndex;
                _lastEstimateLastIndex = lastIndex;

                return _lastEstimatedElementSizeU = smoothedEstimate;
            }

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

                    // Use stored sizes (not DesiredSize) for positioning. When sizes are
                    // locked during extent oscillation, DesiredSize may reflect a
                    // re-measurement (e.g., 84px placeholder) while the stored size
                    // preserves the correct layout size (e.g., 306px loaded).
                    var sizeU = _realizedElements.SizeU[i];
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
                        return;
                    }
                }
            }

            // Fallback: No realized elements or unable to extrapolate, use simple estimation
            var startIndex = Math.Min((int)(viewportStartU / estimatedSize), itemCount - 1);
            index = startIndex;
            position = startIndex * estimatedSize;
        }

        /// <summary>
        /// Captures the current viewport anchor to enable scroll jump compensation.
        /// The anchor is the first element that intersects the viewport start.
        /// </summary>
        private void CaptureViewportAnchor(Orientation orientation)
        {
            if (_realizedElements == null || _realizedElements.Count == 0)
            {
                _viewportAnchorIndex = -1;
                _viewportAnchorU = double.NaN;
                return;
            }

            var viewportStartU = orientation == Orientation.Horizontal ? _viewport.X : _viewport.Y;

            var startU = _realizedElements.StartU;

            // Skip re-capture if viewport hasn't moved significantly AND StartU is stable
            // AND the cached anchor is still within the realized range.
            // All three conditions must hold — a stale anchor outside the realized range
            // would cause ValidateStartU to misclassify all realized items as "before anchor",
            // producing a massive incorrect preDelta and a visible scroll jump.
            if (!double.IsNaN(_lastCapturedViewportStart) &&
                Math.Abs(viewportStartU - _lastCapturedViewportStart) < 1.0 &&
                _viewportAnchorIndex >= 0 &&
                !double.IsNaN(startU) &&
                _viewportAnchorIndex >= _realizedElements.FirstIndex &&
                _viewportAnchorIndex <= _realizedElements.LastIndex)
            {
                return;
            }
            _lastCapturedViewportStart = viewportStartU;

            _viewportAnchorIndex = -1;
            _viewportAnchorU = double.NaN;
            var viewportEndU = orientation == Orientation.Horizontal ? _viewport.Right : _viewport.Bottom;

            if (double.IsNaN(startU))
            {
                return;
            }

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
                    return;
                }

                u = elementEndU;
            }

        }

        /// <summary>
        /// Compensates for extent changes by checking anchor stability.
        /// Relies on Avalonia's built-in scroll anchoring (IScrollAnchorProvider).
        /// </summary>
        private void CompensateForExtentChange(Orientation orientation, Size desiredSize)
        {
            var currentExtentU = orientation == Orientation.Horizontal ?
                desiredSize.Width : desiredSize.Height;

            // When extent is frozen due to oscillation, skip compensation but track
            // convergence. The frozen extent is reported to the ScrollViewer to prevent
            // drift, but we monitor the actual extent to update the frozen value when
            // measurements stabilize.
            if (!double.IsNaN(_frozenExtentU))
            {
                // Growing the frozen extent is always safe — it just reveals more
                // scrollable space and never causes viewport jumps. Apply immediately
                // so the user can always scroll to content that exists below.
                // Only shrinking requires the stabilization check below, because
                // shrinking can cause the ScrollViewer to clamp the offset and jump.
                if (currentExtentU > _frozenExtentU + 5.0)
                {
                    _frozenExtentU = currentExtentU;
                    _lastMeasuredExtentU = currentExtentU;
                    _frozenLastActualExtentU = currentExtentU;
                    return;
                }

                // Track whether the actual extent has stabilized while frozen.
                // If it stays within ±5px for 2+ consecutive passes, update the
                // frozen value to match reality (shrink toward actual). This prevents
                // the frozen value from diverging too far, which would cause scroll
                // position inaccuracy.
                if (Math.Abs(currentExtentU - _frozenLastActualExtentU) < 5.0)
                {
                    _frozenStableCount++;
                    if (_frozenStableCount >= 2 && Math.Abs(currentExtentU - _frozenExtentU) > 5.0)
                    {
                        _frozenExtentU = currentExtentU;
                        _lastMeasuredExtentU = currentExtentU;
                    }
                }
                else
                {
                    _frozenStableCount = 0;
                }
                _frozenLastActualExtentU = currentExtentU;
                return;
            }

            var isFirstMeasure = MathUtilities.AreClose(_lastMeasuredExtentU, 0);

            if (MathUtilities.AreClose(_lastMeasuredExtentU, currentExtentU))
            {
                _lastMeasuredExtentU = currentExtentU;

                return;
            }

            var extentDelta = currentExtentU - _lastMeasuredExtentU;
            var previousExtent = _lastMeasuredExtentU;

            // Skip compensation for small extent changes — normal estimation noise
            // with mixed heights. Only compensate for significant shifts.
            if (Math.Abs(extentDelta) < 2.0)
            {
                _lastMeasuredExtentU = currentExtentU;
                return;
            }

            if (isFirstMeasure)
            {
                _lastMeasuredExtentU = currentExtentU;
                return;
            }

            // Detect extent oscillation: extent alternating up/down across
            // Measure→Arrange cycles. This happens when a non-deterministic item
            // template produces different sizes each time it's measured (e.g.,
            // FileFieldViewModel measuring as 292px then 84px then 292px...).
            // Each extent swing causes CompensateForExtentChange to shift the
            // viewport, which triggers another layout cycle with different items
            // realized, perpetuating the oscillation and drifting the scroll.
            var currentSign = Math.Sign(extentDelta);
            if (_extentOscillationSign != 0 && currentSign != _extentOscillationSign)
            {
                _extentOscillationCount++;
                // Suppress immediately on first reversal if the swing is large (>100px).
                // Large swings cause proportionally large viewport drift via the
                // ScrollViewer, so we can't afford to wait for a second reversal.
                // For small swings, wait for 2 reversals to confirm the pattern.
                var freezeThreshold = Math.Abs(extentDelta) > 100 ? 1 : 2;
                if (_extentOscillationCount >= freezeThreshold)
                {
                    // Oscillation confirmed: freeze extent at the previous value
                    // to stop the ScrollViewer from seeing oscillating extents.
                    // The frozen value will converge toward reality once measurements
                    // stabilize.
                    _frozenExtentU = previousExtent;
                    _lastMeasuredExtentU = _frozenExtentU;
                    _frozenLastActualExtentU = currentExtentU;
                    _frozenStableCount = 0;

                    return;
                }
            }
            else if (currentSign == _extentOscillationSign)
            {
                // Same direction — not oscillating, reset
                _extentOscillationCount = 0;
            }
            _extentOscillationSign = currentSign;

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

                    // Dampen the extent change to prevent ScrollViewer from overshooting
                    // Use a weighted average instead of accepting the full change
                    var dampenedExtent = previousExtent + (extentDelta * 0.3);
                    _lastMeasuredExtentU = dampenedExtent;
                    return;
                }
            }

            _lastMeasuredExtentU = currentExtentU;

            if (_viewportAnchorIndex < 0 || double.IsNaN(_viewportAnchorU))
            {
                return;
            }

            // Check if anchor is still realized
            var currentAnchorU = _realizedElements?.GetElementU(_viewportAnchorIndex);

            if (currentAnchorU == null || double.IsNaN(currentAnchorU.Value))
            {
                return;
            }

            var anchorDrift = currentAnchorU.Value - _viewportAnchorU;

            // CRITICAL: If item 0 is realized at position 0, NEVER apply compensation.
            // Any anchor drift is due to estimation errors in other items, and compensating
            // would incorrectly move item 0 away from its correct position (0).
            if (_realizedElements != null &&
                _realizedElements.FirstIndex == 0 &&
                _realizedElements.StartU is { } startU &&
                !double.IsNaN(startU) &&
                MathUtilities.AreClose(startU, 0))
            {
                return;
            }

            if (!MathUtilities.AreClose(anchorDrift, 0))
            {
                // Anchor drifted - this means items BEFORE the anchor changed size
                // Compensate by shifting StartU to restore the anchor's position
                _realizedElements?.CompensateStartU(-anchorDrift);
            }

        }

        private double GetOrEstimateElementU(int index)
        {
            // Return the position of the existing element if realized.
            var u = _realizedElements?.GetElementU(index) ?? double.NaN;

            if (!double.IsNaN(u))
                return u;

            // Estimate the element size.
            var estimatedSize = EstimateElementSizeU();

            // If we have a valid StartU, use it to anchor estimates relative to the realized range.
            if (_realizedElements is { } realized && !double.IsNaN(realized.StartU))
            {
                var first = realized.FirstIndex;
                var last = realized.LastIndex;
            
                if (index < first)
                {
                    return realized.StartU - ((first - index) * estimatedSize);
                }
            
                if (index > last)
                {
                    var sizes = realized.SizeU;
                    var realizedSpan = 0.0;
            
                    for (var i = 0; i < sizes.Count; ++i)
                    {
                        var sizeU = sizes[i];
                        realizedSpan += double.IsNaN(sizeU) ? estimatedSize : sizeU;
                    }
            
                    return realized.StartU + realizedSpan + ((index - last - 1) * estimatedSize);
                }
            }

            return index * estimatedSize;
        }

        /// <summary>
        /// Called after each element is measured during realization. Override in tests
        /// to simulate non-deterministic measurement (async image loading, text wrapping)
        /// by returning a modified size. The default implementation returns the measured
        /// size unchanged.
        /// </summary>
        /// <param name="index">The item index.</param>
        /// <param name="measuredSizeU">The element's measured size in the layout orientation.</param>
        /// <returns>The size to use for layout. Defaults to <paramref name="measuredSizeU"/>.</returns>
        protected internal virtual double AdjustElementSize(int index, double measuredSizeU)
            => measuredSizeU;

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

                if (!e.IsMeasureValid)
                    e.Measure(availableSize);

                var sizeU = horizontal ? e.DesiredSize.Width : e.DesiredSize.Height;
                sizeU = AdjustElementSize(index, sizeU);
                var sizeV = horizontal ? e.DesiredSize.Height : e.DesiredSize.Width;

                // Pre-emptive fix: Force item 0 to position U=0 to prevent clipping
                // This handles the case when item 0 is the anchor element with wrong estimated position.
                // Skip when extent is frozen: the scroll offset is based on the frozen extent,
                // and forcing item 0 to 0 creates a gap between items 0 and 1 that makes the
                // next pass think the viewport is disjunct from realized items.
                if (index == 0 && !MathUtilities.AreClose(u, 0) && double.IsNaN(_frozenExtentU))
                {
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

            while (u > viewport.viewportUStart && index >= 0)
            {
                var e = GetOrCreateElement(items, index);

                if (!e.IsMeasureValid)
                    e.Measure(availableSize);
                var sizeU = horizontal ? e.DesiredSize.Width : e.DesiredSize.Height;
                sizeU = AdjustElementSize(index, sizeU);
                var sizeV = horizontal ? e.DesiredSize.Height : e.DesiredSize.Width;

                u -= sizeU;

                // Force item 0 to position U=0 to prevent clipping from estimation errors.
                // Skip when extent is frozen (see forward-loop comment for rationale).
                if (index == 0 && !MathUtilities.AreClose(u, 0) && double.IsNaN(_frozenExtentU))
                {
                   u = 0;
                }

                _measureElements!.Add(index, e, u, sizeU);
                viewport.measuredV = Math.Max(viewport.measuredV, sizeV);
                --index;
            }
            
            // Check if we reached the start of the collection
            _hasReachedStart = index < 0;

            // If we've reached the start (realized item 0), ensure item 0 is positioned correctly.
            if (_hasReachedStart && _measureElements.Count > 0 && _measureElements.FirstIndex == 0)
            {
                var firstItemU = _measureElements.StartU;

                if (double.IsNaN(_frozenExtentU))
                {
                    // Normal mode: force item 0 to position 0 to prevent clipping.
                    if (!MathUtilities.AreClose(firstItemU, 0))
                    {
                        var adjustment = -firstItemU;
                        _measureElements.CompensateStartU(adjustment);
                        viewport.realizedEndU += adjustment;
                    }
                }
                else if (firstItemU > viewport.viewportUStart)
                {
                    // Frozen extent mode: the viewport is above item 0 — there's empty space
                    // above the content. We can't force item 0 to 0 (that would create a gap
                    // between positions and the scroll offset). Instead, shift items to follow
                    // the viewport and reduce the frozen extent by the same amount. This
                    // gradually converges item 0 toward position 0 as the user scrolls up.
                    var shift = viewport.viewportUStart - firstItemU; // negative
                    _measureElements.CompensateStartU(shift);
                    viewport.realizedEndU += shift;
                    _frozenExtentU += shift; // shrink extent to remove top empty space
                }
            }

            // If we've reached the end of the collection during frozen extent, cap the frozen
            // extent at the actual content end to prevent scrolling past the last item.
            // When _hasReachedEnd is true, contentEndU is the definitive content boundary —
            // no guard on viewport position is needed. The previous guard
            // (contentEndU >= viewportUEnd) prevented capping when fast scrolling pushed
            // the viewport past all content, leaving the frozen extent inflated and the
            // ScrollViewer showing empty space.
            if (_hasReachedEnd && !double.IsNaN(_frozenExtentU) && _measureElements.Count > 0)
            {
                var contentEndU = viewport.realizedEndU;
                if (_frozenExtentU > contentEndU)
                {
                    _frozenExtentU = contentEndU;
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

            // Check retained containers first — these already have the correct DataContext
            // and only need a lightweight index update instead of full PrepareItemContainer.
            if (_retainedForReuse != null && item != null &&
                _retainedForReuse.TryGetValue(item, out var retained))
            {
                _retainedForReuse.Remove(item);
                var element = retained.element;
                var oldIndex = retained.oldIndex;
                if (oldIndex != index)
                    ItemContainerGenerator!.ItemContainerIndexChanged(element, oldIndex, index);
                return element;
            }

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
                AddInternalChild(recycled);
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
                ItemContainerGenerator!.ClearItemContainer(element);
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
                RemoveInternalChild(element);
            }
        }

        private void RecycleFocusedElement()
        {
            if (_focusedElement != null)
            {
                RecycleElementOnItemRemoved(_focusedElement);
            }
            _focusedElement = null;
            _focusedIndex = -1;
        }

        private void RecycleScrollToElement()
        {
            if (_scrollToElement != null)
            {
                RecycleElementOnItemRemoved(_scrollToElement);
            }
            _scrollToElement = null;
            _scrollToIndex = -1;
        }

        /// <summary>
        /// Retains containers whose DataContext matches items in the given index range,
        /// so they can be reused without full PrepareItemContainer + Measure overhead.
        /// Nullifies matching elements in <see cref="_realizedElements"/> so that the
        /// subsequent RecycleAll/ItemsReset skips them.
        /// </summary>
        private void RetainMatchingContainers(IReadOnlyList<object?> items, int startIndex, int endIndex)
        {
            if (_realizedElements is null || _realizedElements.Count == 0)
                return;

            startIndex = Math.Max(0, startIndex);
            endIndex = Math.Min(endIndex, items.Count);

            if (endIndex <= startIndex)
                return;

            // Build a set of DataContexts we need in the estimated viewport range
            var needed = new Dictionary<object, int>(endIndex - startIndex);
            for (var i = startIndex; i < endIndex; i++)
            {
                var item = items[i];
                if (item != null && !needed.ContainsKey(item))
                    needed[item] = i;
            }

            if (needed.Count == 0)
                return;

            _retainedForReuse ??= new Dictionary<object, (Control, int, double)>();
            _retainedForReuse.Clear();

            // Walk realized elements, nullify those whose DataContext matches a needed item
            var firstRealized = _realizedElements.FirstIndex;
            var lastRealized = _realizedElements.LastIndex;

            for (var i = firstRealized; i <= lastRealized; i++)
            {
                var element = _realizedElements.GetElement(i);
                if (element?.DataContext is not { } dc)
                    continue;

                if (needed.ContainsKey(dc))
                {
                    var nullified = _realizedElements.NullifyElement(i);
                    if (nullified.HasValue)
                    {
                        // Unregister as anchor candidate so the ScrollViewer doesn't
                        // track stale positions when the element moves to a new index.
                        _scrollAnchorProvider?.UnregisterAnchorCandidate(nullified.Value.element);
                        _retainedForReuse[dc] = (nullified.Value.element, i, nullified.Value.sizeU);
                    }
                }
            }

            if (_retainedForReuse.Count == 0)
                _retainedForReuse = null;
        }

        /// <summary>
        /// Recycles any retained containers that were not reused during realization.
        /// Must be called after RealizeElements to avoid orphaned children.
        /// </summary>
        private void RecycleUnusedRetainedContainers()
        {
            if (_retainedForReuse == null)
                return;

            foreach (var entry in _retainedForReuse)
            {
                RecycleElementOnItemRemoved(entry.Value.element);
            }

            _retainedForReuse = null;
        }

        private void RecycleElementOnItemRemoved(Control element)
        {
            Debug.Assert(ItemContainerGenerator is not null);

            _scrollAnchorProvider?.UnregisterAnchorCandidate(element);

            var recycleKey = element.GetValue(RecycleKeyProperty);
            
            if (recycleKey is null)
            {
                ItemContainerGenerator!.ClearItemContainer(element);
                RemoveInternalChild(element);
            }
            else if (recycleKey == s_itemIsItsOwnContainer)
            {
                RemoveInternalChild(element);
            }
            else
            {
                ItemContainerGenerator!.ClearItemContainer(element);
                PushToRecyclePool(recycleKey, element);
                element.SetCurrentValue(Visual.IsVisibleProperty, false);
                RemoveInternalChild(element);
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
        
        private Rect CalculateExtendedViewport(bool vertical, double viewportSize, double bufferSize)
        {

            var extendedViewportStart = vertical ?
                Math.Max(0, _viewport.Top - bufferSize) :
                Math.Max(0, _viewport.Left - bufferSize);

            var extendedViewportEnd = vertical ?
                Math.Min(Bounds.Height, _viewport.Bottom + bufferSize) :
                Math.Min(Bounds.Width, _viewport.Right + bufferSize);

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
                if (spaceLeft >= 0 && spaceRight < 0)
                    extendedViewportStart = Math.Max(0, extendedViewportStart - Math.Abs(spaceRight));
            }

            if (vertical)
            {
                return new Rect(
                    _viewport.X,
                    extendedViewportStart,
                    _viewport.Width,
                    extendedViewportEnd - extendedViewportStart);
            }
            else
            {
                return new Rect(
                    extendedViewportStart,
                    _viewport.Y,
                    extendedViewportEnd - extendedViewportStart,
                    _viewport.Height);
            }
        }

        private void OnEffectiveViewportChanged(object? sender, EffectiveViewportChangedEventArgs e)
        {
            var vertical = Orientation == Orientation.Vertical;
            var oldViewportStart = vertical ? _viewport.Top : _viewport.Left;
            var oldViewportEnd = vertical ? _viewport.Bottom : _viewport.Right;
            var oldExtendedViewportStart = vertical ? _lastMeasuredExtendedViewport.Top : _lastMeasuredExtendedViewport.Left;
            var oldExtendedViewportEnd = vertical ? _lastMeasuredExtendedViewport.Bottom : _lastMeasuredExtendedViewport.Right;

            // Update current viewport
            _viewport = e.EffectiveViewport.Intersect(new(Bounds.Size));
            _isWaitingForViewportUpdate = false;

            // Calculate buffer sizes based on viewport dimensions
            var viewportSize = vertical ? _viewport.Height : _viewport.Width;
            var bufferSize = viewportSize * _bufferFactor;

            var extendedViewPort = CalculateExtendedViewport(vertical, viewportSize, bufferSize);

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
                    // For small extended viewport shifts, skip the expensive nearingEdge check
                    var extShiftU = Math.Abs(newExtendedViewportEnd - oldExtendedViewportEnd) +
                                    Math.Abs(newExtendedViewportStart - oldExtendedViewportStart);

                    if (extShiftU < 2.0)
                    {
                        // Tiny shift, not worth measuring
                    }
                    else
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
            }

            // Supplementary check: detect viewport growth after a previous shrink.
            // The main comparison (Cases 1a/1b) uses _lastMeasuredExtendedViewport which only updates
            // on measure. When the viewport shrinks (e.g. ComboBox popup during filtering),
            // _lastMeasuredExtendedViewport stays stale-large, masking subsequent growth. Compare against
            // _lastKnownExtendedViewport (always updated) to catch this case.
            if (!needsMeasure)
            {
                var lastKnownStart = vertical ? _lastKnownExtendedViewport.Top : _lastKnownExtendedViewport.Left;
                var lastKnownEnd = vertical ? _lastKnownExtendedViewport.Bottom : _lastKnownExtendedViewport.Right;
                if (newViewportStart < lastKnownStart || newViewportEnd > lastKnownEnd)
                {
                    needsMeasure = true;
                }
            }

            _lastKnownExtendedViewport = extendedViewPort;

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
                    _lastMeasuredExtendedViewport = extendedViewPort;
                    return;
                }

                // Reset consecutive measure count so the cycle breaker allows fresh passes
                // for the new viewport position.
                _consecutiveMeasureCount = 0;
                _measurePostponed = false;

                // Extent oscillation handling:
                // - During detection phase (not frozen): do NOT reset oscillation
                //   tracking. Self-induced viewport changes from extent swings would
                //   otherwise prevent the counter from ever reaching the threshold.
                // - When frozen: do NOT lift the freeze. The freeze is permanent until
                //   OnItemsChanged. Lifting on viewport changes would restart the
                //   oscillation, causing the viewport to drift toward 0.
                //   The convergence mechanism in CompensateForExtentChange updates the
                //   frozen value toward reality when measurements stabilize.

                // Only update the measure viewport when triggering a measure. This keeps the
                // wider realization range available for externally-triggered measures (e.g. from
                // OnItemsChanged), ensuring enough items are realized.
                _lastMeasuredExtendedViewport = extendedViewPort;
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
        internal Dictionary<object, int> DiscoverTemplateKeys()
        {
            var templateKeys = new Dictionary<object, int>();
            var items = Items;

            if (items == null || items.Count == 0 || ItemContainerGenerator == null)
                return templateKeys;

            // Sample first N items to discover template types
            int sampleSize = Math.Min(WarmupSampleSize, items.Count);

            for (int i = 0; i < sampleSize; i++)
            {
                var item = items[i];

                // Use ItemContainerGenerator to determine recycle key without creating container
                if (ItemContainerGenerator.NeedsContainer(item, i, out var recycleKey) && recycleKey != null)
                    CollectionsMarshal.GetValueRefOrAddDefault(templateKeys, recycleKey, out _)++;
            }

            // Query MaxPoolSizePerKey from IVirtualizingDataTemplate if available
            if (ItemsControl?.ItemTemplate is Templates.IVirtualizingDataTemplate vdt)
            {
                foreach (var key in templateKeys.Keys.ToList())
                {
                    templateKeys[key] = vdt.MinPoolSizePerKey;
                }

            }
            else
            {
                // Default to 3 containers per type if no MaxPoolSizePerKey available
                foreach (var key in templateKeys.Keys.ToList())
                {
                    templateKeys[key] = 3;
                }
            }

            return templateKeys;
        }

        /// <summary>
        /// Pre-creates containers with their content for each discovered template type.
        /// Containers are stored in the recycle pool with their Child controls already attached,
        /// ready to be reused during scrolling. This eliminates the expensive template instantiation
        /// cost during the first scroll.
        /// </summary>
        internal void PerformWarmup()
        {
            if (_isWarmupComplete || Items == null || Items.Count == 0)
                return;

            var startTime = System.Diagnostics.Stopwatch.StartNew();
            var templateKeys = DiscoverTemplateKeys();

            if (templateKeys.Count == 0)
            {
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
                    continue;
                }

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
                        RemoveInternalChild(container);

                        totalCreated++;
                    }
                    catch
                    {
                        break;
                    }
                }
            }

            _isWarmupComplete = true;
            startTime.Stop();

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
