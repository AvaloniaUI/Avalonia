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
        /// How many containers warmup keeps ready per template key when the item template does not
        /// specify a size itself (see <see cref="Templates.IVirtualizingDataTemplate"/>). Enough to
        /// cover the containers in flight while scrolling; the pool grows no further on its own.
        /// </summary>
        private const int DefaultWarmupPoolSizePerKey = 3;

        private static readonly AttachedProperty<object?> RecycleKeyProperty =
            AvaloniaProperty.RegisterAttached<VirtualizingStackPanel, Control, object?>("RecycleKey");

        private static readonly object s_itemIsItsOwnContainer = new object();
        private readonly Action<Control, int> _recycleElement;
        private readonly Action<Control> _recycleElementOnItemRemoved;
        private readonly Action<Control, int, int> _updateElementIndex;
        private readonly Func<Control, int, double> _getElementSizeU;
        private int _scrollToIndex = -1;
        private Control? _scrollToElement;
        private bool _isInLayout;
        private bool _isWaitingForViewportUpdate;
        private double _lastEstimatedElementSizeU = 25;

        // Persistent per-item size model: maps item index -> last measured sizeU.
        // Upserted whenever a realized element is measured (see EstimateElementSizeU).
        // The estimate for an un-measured item is the mean of ALL recorded sizes, so it
        // depends on every item ever measured rather than on which items happen to be
        // realized right now — scrolling the realized window into a large/small-item
        // region no longer swings the scalar estimate (and thus the reported extent).
        // Indices are remapped on structural collection changes so an entry never points
        // at the wrong item's size. Memory is bounded by the number of distinct item
        // indices ever measured (no artificial cap; stock has none).
        private readonly Dictionary<int, double> _measuredSizes = new();

        // Running sum of _measuredSizes.Values, maintained incrementally by the accessors
        // below — every mutation of the record adjusts it by the delta, so the record is
        // never swept (an O(items ever measured) sweep per measure pass would be worse than
        // stock's O(realized window) average). Together with _measuredSizes.Count it lets
        // the extent be computed as knownSum + (itemCount - knownCount) * mean: a cumulative
        // estimate that depends only on what has EVER been measured, not on the current
        // realized window. That makes the reported extent reproducible when an offset is
        // revisited. _measuredSizesSumError is the Neumaier compensation term (see
        // AddToMeasuredSizesSum): read the sum through MeasuredSizesSum, never directly.
        private double _measuredSizesSum;
        private double _measuredSizesSumError;
        internal bool TryGetMeasuredSizeForTesting(int index, out double size) => _measuredSizes.TryGetValue(index, out size);
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

        // Template keys the panel has actually needed a container for, each mapped to an item index
        // known to use it. Drives container warmup (see NoteEncounteredRecycleKey).
        private Dictionary<object, int>? _encounteredRecycleKeys;

        private bool _hasReachedStart = false;
        private bool _hasReachedEnd = false;

        private Rect _lastMeasuredViewport;
        private bool _suppressScrollIntoView = false;  // Suppress ScrollIntoView after Reset
        private Rect _lastMeasuredExtendedViewport;
        private Rect _lastKnownExtendedViewport;

        // Index of the first item intersecting the viewport start. Captured before
        // ValidateStartU so a resize of items *before* the visible area can be compensated
        // for without moving what the user is looking at.
        private int _viewportAnchorIndex = -1;

        // Cache for CaptureViewportAnchor to avoid redundant O(n) scans
        private double _lastCapturedViewportStart = double.NaN;

        // Retained containers for smart reuse during disjunct recycle
        private Dictionary<object, (Control element, int oldIndex, double sizeU)>? _retainedForReuse;
        static VirtualizingStackPanel()
        {
            CacheLengthProperty.Changed.AddClassHandler<VirtualizingStackPanel>((x, e) => x.OnCacheLengthChanged(e));
        }

        public VirtualizingStackPanel()
        {
            _recycleElement = RecycleElement;
            _recycleElementOnItemRemoved = RecycleElementOnItemRemoved;
            _updateElementIndex = UpdateElementIndex;
            _getElementSizeU = GetElementSizeU;

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
            // If we're bringing an item into view, ignore any layout passes until we receive a new
            // effective viewport.
            if (_isWaitingForViewportUpdate)
            {
                return EstimateDesiredSize(orientation, items.Count);
            }

            _isInLayout = true;

            try
            {
                _realizedElements ??= new();
                _measureElements ??= new();

                // Capture viewport anchor BEFORE ValidateStartU so we know which items
                // are before/after the visible area for scroll position compensation.
                CaptureViewportAnchor(orientation);

                // Reconcile the stored element sizes with what the elements now desire. When only
                // content above the anchor changed size, StartU is shifted so the anchor keeps
                // its position — this is what stops async content (e.g. an image finishing
                // loading above the viewport) from yanking the scroll position.
                if (_realizedElements.ValidateStartU(_viewportAnchorIndex, _getElementSizeU, out _) &&
                    double.IsNaN(_realizedElements.StartU))
                {
                    // StartU went unstable, meaning positions are being re-derived from scratch
                    // after a resize that spans the anchor. The recorded per-item sizes describe
                    // the old layout, so drop them and let the estimate rebuild from the new
                    // measurements (stock likewise adapts instantly to a uniform resize).
                    ClearMeasuredSizes();
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

                return CalculateDesiredSize(orientation, items.Count, viewport);
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

            try
            {
                var orientation = Orientation;
                var u = _realizedElements!.StartU;

                // Collection changes before the realized range make its exact position unstable
                // until the next measure. ScrollIntoView can intentionally defer that measure
                // while waiting for an updated viewport, so use the same position estimate used
                // when realizing an element instead of arranging the range at NaN.
                if (double.IsNaN(u))
                    u = GetOrEstimateElementU(_realizedElements.FirstIndex);

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

        internal override void Refresh()
        {
            // A refresh means the ItemTemplate / ItemContainerTheme / DisplayMemberBinding changed:
            // the collection is untouched, but every container must be re-prepared so the new
            // template or theme is applied. It must therefore never be treated as a preservable
            // Reset. Because nothing changed, the Reset path below would find every realized
            // element still valid at its index and keep it as-is, and the non-preserving branch
            // would hand matching containers to RetainMatchingContainers — neither calls
            // PrepareItemContainer. So recycle every realized element up front: the base Reset
            // handling then sees an empty realized set (no preservation, nothing to retain) and the
            // next measure re-prepares every container.
            _realizedElements?.ItemsReset(_recycleElementOnItemRemoved);
            base.Refresh();
        }

        protected override void OnItemsChanged(IReadOnlyList<object?> items, NotifyCollectionChangedEventArgs e)
        {
            _lastCapturedViewportStart = double.NaN;
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
                    RemapMeasuredSizesForInsert(e.NewStartingIndex, e.NewItems!.Count);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    _realizedElements.ItemsRemoved(e.OldStartingIndex, e.OldItems!.Count, _updateElementIndex, _recycleElementOnItemRemoved);
                    RemapMeasuredSizesForRemove(e.OldStartingIndex, e.OldItems!.Count);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    _realizedElements.ItemsReplaced(e.OldStartingIndex, e.OldItems!.Count, _recycleElementOnItemRemoved);
                    // The items at these indices are now different objects, so their recorded
                    // sizes are stale — drop them (they'll be re-measured on the next pass).
                    for (var i = 0; i < e.OldItems!.Count; ++i)
                        ForgetMeasuredSize(e.OldStartingIndex + i);
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
                    // A move shifts an arbitrary index range; rather than track the permutation,
                    // clear the record (conservative but always correct — no entry can point at
                    // the wrong item). Sizes rebuild as items are re-measured.
                    ClearMeasuredSizes();
                    break;
                case NotifyCollectionChangedAction.Reset:
                    // Try to preserve scroll position during Reset
                    // Strategy: Validate that realized items still exist in the new collection
                    // If they do, keep them realized to maintain scroll stability
                    // If they don't, recycle everything (collection replacement scenario)

                    var shouldPreserveRealizedElements = false;

                    if (_realizedElements.Count > 0)
                    {
                        // Check whether every realized item still exists at its current index.
                        var preservedCount = 0;
                        var realizedCount = 0;
                        for (var i = 0; i < _realizedElements.Count; i++)
                        {
                            if (_realizedElements.Elements[i] == null)
                                continue;

                            realizedCount++;

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

                        // Preserve realized elements ONLY when EVERY one is still valid at its
                        // current index (the pure append / infinite-scroll case, where all
                        // realized items keep their indices). A partial match is unsafe: when a
                        // mid-list insert or remove is coalesced into a single Reset (e.g. by
                        // DynamicData's Bind reset-threshold), the elements before the edit point
                        // still match while everything at/after it has shifted. A bare-majority
                        // test would then preserve the whole stale mapping, leaving the shifted
                        // items pinned to the wrong containers and rendered at the wrong position.
                        // In that case fall through to the full reset path, which re-realizes
                        // (and reuses matching containers via RetainMatchingContainers) correctly.
                        shouldPreserveRealizedElements = realizedCount > 0 && preservedCount == realizedCount;

                    }

                    if (shouldPreserveRealizedElements)
                    {
                        // Keep the realized elements — every one is still valid at its index, and
                        // normal realization handles any adjustment. The recorded per-item sizes are
                        // deliberately kept too: nothing about the items realized here changed, so
                        // re-deriving the estimate from scratch would only make the reported extent
                        // move for no reason. Suppress ScrollIntoView so the ListBox does not pull
                        // the scroll position to the selected item.
                        _suppressScrollIntoView = true;
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

                        // All elements were recycled and item identities/indices are no longer
                        // known — clear the per-item size record so no entry points at a stale item.
                        ClearMeasuredSizes();
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

            // If the collection is now empty, remove any pooled recycle containers from the
            // visual tree. Containers recycled when the collection is cleared (e.g. ItemsReset
            // above) are pushed to the recycle pool but kept parented for reuse. Because
            // MeasureOverride early-returns on an empty collection, the normal measure-time
            // cleanup never runs, so without this they would linger as invisible "ghost"
            // children. Recycle any still-realized elements first, then drop the pool.
            if (items.Count == 0)
            {
                if (_realizedElements is { Count: > 0 })
                    _realizedElements.RecycleAllElements(_recycleElement);
                RemoveRecyclePoolChildren();
            }
        }

        /// <summary>
        /// Removes all pooled recycle containers from the visual tree and clears the pool.
        /// Used when the collection becomes empty to avoid leaving invisible "ghost" children.
        /// </summary>
        private void RemoveRecyclePoolChildren()
        {
            if (_recyclePool is null || _recyclePool.Count == 0)
                return;

            foreach (var pool in _recyclePool.Values)
            {
                for (var i = pool.Count - 1; i >= 0; i--)
                    RemoveInternalChild(pool[i]);
                pool.Clear();
            }

            _recyclePool.Clear();
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
                // Window-independent extent from the persistent per-item size record:
                //   extent = knownSum + (itemCount - knownCount) * mean
                // knownSum/knownCount are what has EVER been measured (published by the
                // EstimateElementSizeU call that precedes this in MeasureOverride), not the
                // current realized window, so revisiting an offset reproduces the same extent.
                // Stock instead blends realizedEndU (the current window's accumulated
                // positions) with the estimate, which swings the reported extent by region.
                sizeU = CacheBasedExtentU(itemCount);

                // Reconciliation: the realized block is still positioned by the anchor/StartU
                // logic, whose bottom is realizedEndU. If the mean (dragged down by many small
                // items elsewhere) put the cache extent below the current block's actual
                // bottom, the scrollbar couldn't reach the realized content — take the max so
                // the extent always covers what is on screen. When every item is known this is
                // a no-op (knownSum == realizedEndU at the bottom edge).
                sizeU = Math.Max(sizeU, viewport.realizedEndU);
            }

            return orientation == Orientation.Horizontal ? new(sizeU, sizeV) : new(sizeV, sizeU);
        }

        private Size EstimateDesiredSize(Orientation orientation, int itemCount)
        {
            if (_scrollToIndex >= 0 && _scrollToElement is not null)
            {
                // We have an element to scroll to, so we can estimate the desired size based on the
                // element's position and the remaining elements.
                var u = orientation == Orientation.Horizontal ?
                    _scrollToElement.Bounds.Right :
                    _scrollToElement.Bounds.Bottom;
                // Same cache-based tail as CalculateDesiredSize so the scroll-to-element
                // extent uses the window-independent mean consistently; reconcile against the
                // scroll target's actual bottom so the extent always covers it.
                var sizeU = Math.Max(CacheBasedExtentU(itemCount), u);
                return orientation == Orientation.Horizontal ?
                    new(sizeU, DesiredSize.Height) :
                    new(DesiredSize.Width, sizeU);
            }

            return DesiredSize;
        }

        /// <summary>
        /// Computes the total extent along U from the persistent per-item size record:
        /// <c>knownSum + (itemCount - knownCount) * mean</c>, where <c>knownSum</c> and
        /// <c>knownCount</c> are the sum and count of every item ever measured (published by
        /// <see cref="EstimateElementSizeU"/>) and <c>mean = knownSum / knownCount</c>. This
        /// depends only on what has been measured, not on the current realized window, so the
        /// reported extent is reproducible when an offset is revisited. When every item has
        /// been measured, <c>itemCount - knownCount == 0</c> and the extent is exactly the
        /// true total (<c>knownSum</c>) — the correct bottom edge. Falls back to the scalar
        /// estimate when the record is empty.
        /// </summary>
        private double CacheBasedExtentU(int itemCount)
        {
            var knownCount = _measuredSizes.Count;
            if (knownCount == 0)
                return itemCount * _lastEstimatedElementSizeU;

            var knownSum = MeasuredSizesSum;
            var mean = knownSum / knownCount;
            var unknownCount = itemCount - knownCount;
            if (unknownCount < 0)
                unknownCount = 0;
            return knownSum + (unknownCount * mean);
        }

        private double EstimateElementSizeU()
        {
            if (_realizedElements is null)
                return _lastEstimatedElementSizeU;

            // Upsert every currently-realized, measured element's size into the persistent
            // per-item size record, keyed by item index. This is the only update point for
            // the record: the estimate is then the mean over ALL recorded sizes, not just
            // the elements realized on this pass. For uniform items every recorded size is
            // equal, so the mean equals that size — identical to stock's realized average
            // (provable no-op for the uniform/deterministic case).
            var firstIndex = _realizedElements.FirstIndex;
            var elements = _realizedElements.Elements;
            for (var i = 0; i < elements.Count; ++i)
            {
                if (elements[i] is not { IsMeasureValid: true } element)
                    continue;
                RecordMeasuredSize(firstIndex + i, GetElementSizeU(element, firstIndex + i));
            }

            // Not enough information yet: keep the last estimate (stock's seed until the
            // first measurement).
            var knownCount = _measuredSizes.Count;
            if (knownCount == 0)
                return _lastEstimatedElementSizeU;

            // The running sum is maintained by the upsert above, so this pass costs
            // O(realized window) — the record itself is never swept.
            var total = MeasuredSizesSum;

            // Guard against a degenerate all-zero record (matches stock's total == 0 guard).
            if (total == 0)
                return _lastEstimatedElementSizeU;

            // Store and return the estimate: the mean of all recorded sizes.
            return _lastEstimatedElementSizeU = total / knownCount;
        }

        /// <summary>
        /// The sum of every value in <see cref="_measuredSizes"/>, maintained incrementally.
        /// </summary>
        private double MeasuredSizesSum => _measuredSizesSum + _measuredSizesSumError;

        /// <summary>
        /// The single upsert point for the persistent per-item size record: records
        /// <paramref name="size"/> for <paramref name="index"/> and keeps the running sum in
        /// agreement by applying only the delta.
        /// </summary>
        private void RecordMeasuredSize(int index, double size)
        {
            if (_measuredSizes.TryGetValue(index, out var previous))
            {
                // Re-measuring an unchanged item is the common case and must not touch the
                // sum: no write means no rounding, so a scrolling session over settled
                // content accumulates no error at all.
                if (previous == size)
                    return;

                _measuredSizes[index] = size;
                AddToMeasuredSizesSum(size - previous);
            }
            else
            {
                _measuredSizes[index] = size;
                AddToMeasuredSizesSum(size);
            }
        }

        /// <summary>
        /// Drops the recorded size for <paramref name="index"/>, keeping the running sum in
        /// agreement.
        /// </summary>
        private void ForgetMeasuredSize(int index)
        {
            if (!_measuredSizes.Remove(index, out var previous))
                return;

            if (_measuredSizes.Count == 0)
                ResetMeasuredSizesSum();
            else
                AddToMeasuredSizesSum(-previous);
        }

        /// <summary>
        /// Drops the whole record (used when the index-to-item mapping is no longer
        /// trustworthy), keeping the running sum in agreement.
        /// </summary>
        private void ClearMeasuredSizes()
        {
            _measuredSizes.Clear();
            ResetMeasuredSizesSum();
        }

        private void ResetMeasuredSizesSum()
        {
            _measuredSizesSum = 0;
            _measuredSizesSumError = 0;
        }

        /// <summary>
        /// Adds <paramref name="delta"/> to the running sum using Neumaier compensated
        /// summation: the rounding error of each accumulation is carried in
        /// <see cref="_measuredSizesSumError"/> and folded back in by
        /// <see cref="MeasuredSizesSum"/>, so an incrementally maintained sum stays within one
        /// rounding of a freshly computed full sum however many updates it has seen. This is
        /// what makes the reported extent reproducible without periodically re-summing the
        /// record (which would need a rebuild interval, i.e. a tuning constant).
        /// </summary>
        private void AddToMeasuredSizesSum(double delta)
        {
            var sum = _measuredSizesSum + delta;

            _measuredSizesSumError += Math.Abs(_measuredSizesSum) >= Math.Abs(delta)
                ? (_measuredSizesSum - sum) + delta
                : (delta - sum) + _measuredSizesSum;

            _measuredSizesSum = sum;
        }

        /// <summary>
        /// Remaps the persistent per-item size record after <paramref name="count"/> items were
        /// inserted at <paramref name="index"/>: entries at or after the insertion point shift up
        /// by <paramref name="count"/>. The inserted slots are left unrecorded (unknown size).
        /// Mirrors <see cref="RealizedStackElements.ItemsInserted"/> so an entry never points at
        /// the wrong item. Remapped in place: an insert at or past the highest recorded index
        /// — the append case an infinite-scroll list pays on every batch — moves nothing and
        /// allocates nothing, and a mid-list insert only touches the entries after it.
        /// </summary>
        private void RemapMeasuredSizesForInsert(int index, int count)
        {
            if (count <= 0 || _measuredSizes.Count == 0)
                return;

            // Snapshot the entries that have to move; the record is enumerated but not
            // rebuilt, and nothing is allocated when none of them do.
            List<KeyValuePair<int, double>>? shifted = null;
            foreach (var entry in _measuredSizes)
            {
                if (entry.Key >= index)
                    (shifted ??= new List<KeyValuePair<int, double>>()).Add(entry);
            }

            if (shifted is null)
                return;

            // Remove every moving entry before writing any of them back, so a shifted key can
            // never overwrite an entry that has not been moved yet (which iterating the
            // snapshot in dictionary order otherwise would). A pure shift leaves the sum
            // unchanged.
            foreach (var entry in shifted)
                _measuredSizes.Remove(entry.Key);
            foreach (var entry in shifted)
                _measuredSizes[entry.Key + count] = entry.Value;
        }

        /// <summary>
        /// Remaps the persistent per-item size record after <paramref name="count"/> items were
        /// removed at <paramref name="index"/>: entries in the removed range are dropped and
        /// entries after it shift down by <paramref name="count"/>. Mirrors
        /// <see cref="RealizedStackElements.ItemsRemoved"/>. Remapped in place, like
        /// <see cref="RemapMeasuredSizesForInsert"/>: a remove past the highest recorded index
        /// moves nothing and allocates nothing.
        /// </summary>
        private void RemapMeasuredSizesForRemove(int index, int count)
        {
            if (count <= 0 || _measuredSizes.Count == 0)
                return;

            var end = index + count;
            List<KeyValuePair<int, double>>? shifted = null;
            List<int>? dropped = null;
            foreach (var entry in _measuredSizes)
            {
                if (entry.Key >= end)
                    (shifted ??= new List<KeyValuePair<int, double>>()).Add(entry);
                else if (entry.Key >= index)
                    (dropped ??= new List<int>()).Add(entry.Key);
            }

            if (dropped is not null)
            {
                foreach (var key in dropped)
                    ForgetMeasuredSize(key);
            }

            if (shifted is null)
                return;

            // As in the insert case: clear the moving entries first so a shifted key cannot
            // land on one that has not moved yet. The entries that stay put are all below
            // index, and every shifted key lands at or above it, so the two never collide.
            foreach (var entry in shifted)
                _measuredSizes.Remove(entry.Key);
            foreach (var entry in shifted)
                _measuredSizes[entry.Key - count] = entry.Value;
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
                for (var i = 0; i < _realizedElements.Elements.Count; ++i)
                {
                    if (_realizedElements.Elements[i] is not { } element)
                        continue;

                    // Walk the *stored* sizes, not DesiredSize: these are the sizes the elements
                    // were last laid out at, so they describe where things currently are on
                    // screen. DesiredSize may already have moved on (content that has just
                    // settled), and using it here would look for the anchor in a layout that has
                    // not happened yet.
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
        /// Captures the index of the item that intersects the start of the viewport — the item
        /// the user is looking at. <see cref="RealizedStackElements.ValidateStartU"/> uses it to
        /// tell a resize of already-scrolled-past content (which must be compensated for, so the
        /// anchor does not move) from a resize at or after the anchor (which legitimately pushes
        /// later content down).
        /// </summary>
        private void CaptureViewportAnchor(Orientation orientation)
        {
            if (_realizedElements == null || _realizedElements.Count == 0)
            {
                _viewportAnchorIndex = -1;
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
                    return;
                }

                u = elementEndU;
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

        /// <summary>
        /// The panel's single view of an element's size along the layout axis. Every place that
        /// records or re-checks a size must go through here: if size *recording* applied
        /// <see cref="AdjustElementSize"/> but size *checking* did not, the two would disagree by
        /// the adjustment on every pass and each pass would look like a fresh resize.
        /// </summary>
        private double GetElementSizeU(Control element, int index)
        {
            var sizeU = Orientation == Orientation.Horizontal
                ? element.DesiredSize.Width
                : element.DesiredSize.Height;
            return AdjustElementSize(index, sizeU);
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

                if (!e.IsMeasureValid)
                    e.Measure(availableSize);

                var sizeU = GetElementSizeU(e, index);
                var sizeV = horizontal ? e.DesiredSize.Height : e.DesiredSize.Width;

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
                var sizeU = GetElementSizeU(e, index);
                var sizeV = horizontal ? e.DesiredSize.Height : e.DesiredSize.Width;

                u -= sizeU;

                _measureElements!.Add(index, e, u, sizeU);
                viewport.measuredV = Math.Max(viewport.measuredV, sizeV);
                --index;
            }
            
            // Check if we reached the start of the collection
            _hasReachedStart = index < 0;

            // Item 0 sits at u == 0 by definition, so whenever the realized range reaches it the
            // whole block's position is known exactly: StartU must be 0. Realization walks
            // backwards from an *estimated* anchor position, so it can arrive at item 0 with a
            // non-zero u; that is accumulated estimation error, not a real offset, and leaving it
            // in would either clip item 0 above the viewport or leave a gap above it. Re-basing
            // the block here also feeds an exact position back into the estimates that follow.
            if (_hasReachedStart && _measureElements.Count > 0 && _measureElements.FirstIndex == 0)
            {
                var firstItemU = _measureElements.StartU;

                if (!MathUtilities.AreClose(firstItemU, 0))
                {
                    var adjustment = -firstItemU;
                    _measureElements.CompensateStartU(adjustment);
                    viewport.realizedEndU += adjustment;
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

                // Force the reused subtree to re-measure. The container itself may still be
                // IsMeasureValid==true while a descendant's measure was invalidated (e.g. a
                // data-bound size changed on the SAME item while it was realized). Avalonia's
                // InvalidateMeasure does not walk up, so a stale descendant is only honored once
                // it is actually re-measured — but RealizeElements skips e.Measure when the
                // container is measure-valid, leaving it arranged at the previous size. Mirror
                // the recycle-for-different-item path (see GetRecycledElement) so re-realization
                // via the retained path always re-measures.
                // Only when a descendant's measure was actually invalidated (e.g. a data-bound
                // size changed on the SAME item while it was realized). In that case the container
                // itself is still IsMeasureValid==true and RealizeElements would skip e.Measure,
                // arranging it at the stale size — so force the whole subtree to re-measure. When
                // nothing changed the subtree is fully valid and this is a no-op, preserving the
                // reuse-without-re-measure optimization.
                if (AnyMeasureInvalidInSubtree(element))
                    InvalidateMeasureRecursive(element);
                return element;
            }

            var generator = ItemContainerGenerator!;

            if (generator.NeedsContainer(item, index, out var recycleKey))
            {
                NoteEncounteredRecycleKey(recycleKey, index);
                return GetRecycledElement(item, index, recycleKey) ??
                       CreateElement(item, index, recycleKey);
            }
            else
            {
                return GetItemAsOwnContainer(item, index);
            }
        }

        /// <summary>
        /// Records that the panel needed a container for <paramref name="recycleKey"/>, remembering
        /// one item index that uses it so warmup can build more containers of that kind later.
        /// </summary>
        /// <remarks>
        /// This is what makes warmup's pool track the template keys actually in use. An index is
        /// stored rather than the item itself so the panel never keeps a data item alive; the index
        /// is re-checked against the current collection at warmup time. When a key turns up that
        /// warmup has not seen, warmup is scheduled again so the pool grows to cover it — the pool
        /// therefore follows where the user actually goes, instead of a guess made from the first
        /// N items of the collection.
        /// </remarks>
        private void NoteEncounteredRecycleKey(object? recycleKey, int index)
        {
            if (recycleKey is null)
                return;

            _encounteredRecycleKeys ??= new();
            if (_encounteredRecycleKeys.TryAdd(recycleKey, index) && EnableWarmup && _isWarmupComplete)
            {
                // A kind of item we have never pooled for. Top the pool up for it, off the layout
                // pass that discovered it.
                _isWarmupComplete = false;
                Threading.Dispatcher.UIThread.Post(PerformWarmup, Threading.DispatcherPriority.Background);
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

                // Detect whether this pooled container is being reused for a *different* item.
                // For IVirtualizingDataTemplate the container is not cleared on recycle and the
                // same child instance is reused, so a container reused for a new item keeps its
                // previous content's cached layout. Making the container visible invalidates the
                // container's own measure, but its content subtree is still IsMeasureValid at the
                // same available size and would short-circuit re-measure — leaving the new item
                // arranged with the previous item's size (content rendered blank / clipped, or
                // text not re-wrapped). Force the reused subtree to re-measure in that case.
                // When reused for the SAME item, the cached layout is still correct — skip the work.
                var dataContextChanged = !ReferenceEquals(recycled.DataContext, item);

                generator.PrepareItemContainer(recycled, item, index);
                generator.ItemContainerPrepared(recycled, item, index);

                if (dataContextChanged)
                    InvalidateMeasureRecursive(recycled);

                return recycled;
            }

            return null;
        }

        /// <summary>
        /// Invalidates the measure of <paramref name="visual"/> and its entire visual subtree.
        /// Needed when a recycled container is reused for a different item: the container's content
        /// is not re-created (IVirtualizingDataTemplate reuses the child), so descendants would
        /// otherwise short-circuit re-measure at the unchanged available size and keep the previous
        /// item's layout. This forces the new item's data to be measured before the arrange pass.
        /// </summary>
        private static void InvalidateMeasureRecursive(Visual visual)
        {
            if (visual is Layoutable layoutable)
                layoutable.InvalidateMeasure();

            foreach (var child in visual.GetVisualChildren())
                InvalidateMeasureRecursive(child);
        }

        /// <summary>
        /// Returns true if <paramref name="visual"/> or any descendant has an invalid measure.
        /// Used by the retained-container reuse path to decide whether a stale descendant (e.g. a
        /// data-bound size that changed on the same item while it was realized) needs the reused
        /// subtree to be re-measured. Avalonia's <see cref="Layoutable.InvalidateMeasure"/> does
        /// not propagate up, so a still-valid container would otherwise short-circuit re-measure
        /// and arrange the item at its previous size.
        /// </summary>
        private static bool AnyMeasureInvalidInSubtree(Visual visual)
        {
            if (visual is Layoutable { IsMeasureValid: false })
                return true;

            foreach (var child in visual.GetVisualChildren())
                if (AnyMeasureInvalidInSubtree(child))
                    return true;

            return false;
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

            // Respect MaxPoolSizePerKey, but only for keys an IVirtualizingDataTemplate handed out.
            // Containers under DefaultRecycleKey are pooled uncapped, as in stock Avalonia.
            if (ItemsControl?.GetMaxPoolSizePerKey(recycleKey) is { } maxPoolSize &&
                pool.Count >= maxPoolSize)
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

            var newViewport = e.EffectiveViewport.Intersect(new(Bounds.Size));

            // Ignore a collapsed (empty) viewport: it carries no information about where the
            // user is looking, so it must not be allowed to overwrite the state that does.
            // A window or page being hidden (navigating to another activity, a picker, an
            // unselected tab) reports a 0x0 effective viewport. Accepting it would make the
            // viewport disjunct from every realized element, so the next measure would recycle
            // them all and re-anchor to index 0 at StartU=0; on return the scroll anchor is gone
            // and the ScrollViewer clamps the now out-of-range offset into a large scroll jump.
            // Dropping the update keeps _viewport, the extended viewports and the realized range
            // intact, so the scroll position survives the round trip unchanged.
            //
            // Covered by Collapsing_Viewport_To_Empty_And_Restoring_Preserves_Scroll_Position.
            if (newViewport.Width <= 0 || newViewport.Height <= 0)
            {
                return;
            }

            // Update current viewport
            _viewport = newViewport;
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
        /// The template keys the panel has actually needed a container for so far, mapped to the
        /// number of containers warmup should keep available for each. Grows as the user reaches
        /// items of new kinds, so a collection whose kinds are not all present at its start (a
        /// grouped or sorted list) is covered just as well as one where they are.
        /// </summary>
        internal Dictionary<object, int> DiscoverTemplateKeys()
        {
            var templateKeys = new Dictionary<object, int>();
            var items = Items;

            if (_encounteredRecycleKeys is null || items == null || items.Count == 0)
                return templateKeys;

            // How many containers to keep per kind. The template says so when it knows (it is the
            // thing that knows how expensive it is to build); otherwise keep a small pool.
            var targetCount = ItemsControl?.EffectiveVirtualizingItemTemplate is { } vdt
                ? vdt.MinPoolSizePerKey
                : DefaultWarmupPoolSizePerKey;

            // Forget kinds the collection no longer contains — after the items are replaced, a key
            // encountered under the old collection is not a kind we should keep containers for.
            List<object>? vanished = null;
            foreach (var key in _encounteredRecycleKeys.Keys)
            {
                if (FindItemForRecycleKey(key, items) is null)
                    (vanished ??= new()).Add(key);
                else
                    templateKeys[key] = targetCount;
            }

            if (vanished is not null)
            {
                foreach (var key in vanished)
                    _encounteredRecycleKeys.Remove(key);
            }

            return templateKeys;
        }

        /// <summary>
        /// Finds an item currently in the collection whose container would use
        /// <paramref name="recycleKey"/>, so warmup has something to build a container from.
        /// Starts at the index where the key was first seen and, because the collection may have
        /// changed since, falls back to scanning.
        /// </summary>
        private object? FindItemForRecycleKey(object recycleKey, IReadOnlyList<object?> items)
        {
            var generator = ItemContainerGenerator;
            if (generator is null)
                return null;

            bool Matches(int i) =>
                items[i] is not null &&
                generator.NeedsContainer(items[i], i, out var key) &&
                Equals(key, recycleKey);

            if (_encounteredRecycleKeys!.TryGetValue(recycleKey, out var rememberedIndex) &&
                rememberedIndex >= 0 && rememberedIndex < items.Count &&
                Matches(rememberedIndex))
            {
                return items[rememberedIndex];
            }

            for (var i = 0; i < items.Count; ++i)
            {
                if (Matches(i))
                {
                    _encounteredRecycleKeys[recycleKey] = i;
                    return items[i];
                }
            }

            return null;
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

            var templateKeys = DiscoverTemplateKeys();

            if (templateKeys.Count == 0)
            {
                _isWarmupComplete = true;
                return;
            }

            var items = Items;
            _recyclePool ??= new Dictionary<object, List<Control>>();

            var orientation = Orientation;
            var availableSize = orientation == Orientation.Horizontal
                ? new Size(double.PositiveInfinity, Bounds.Height > 0 ? Bounds.Height : _lastEstimatedElementSizeU)
                : new Size(Bounds.Width > 0 ? Bounds.Width : double.PositiveInfinity, double.PositiveInfinity);

            // Containers already realized count towards the target: they will land in the pool when
            // they are recycled, and the point of the pool is to have containers ready, not to have
            // idle ones.
            var realizedPerKey = new Dictionary<object, int>();
            if (_realizedElements is { Elements: not null } realizedElements)
            {
                foreach (var element in realizedElements.Elements)
                {
                    if (element?.GetValue(RecycleKeyProperty) is { } key)
                        CollectionsMarshal.GetValueRefOrAddDefault(realizedPerKey, key, out _)++;
                }
            }

            foreach (var kvp in templateKeys)
            {
                var recycleKey = kvp.Key;
                var targetCount = kvp.Value;

                var existingCount = _recyclePool.TryGetValue(recycleKey, out var existingPool)
                    ? existingPool.Count
                    : 0;
                if (realizedPerKey.TryGetValue(recycleKey, out var realizedCount))
                    existingCount += realizedCount;

                var neededCount = Math.Max(0, targetCount - existingCount);
                if (neededCount == 0)
                    continue;

                // Any item of this kind will do — building a container is about instantiating the
                // template, and the data is replaced on reuse.
                if (FindItemForRecycleKey(recycleKey, items) is not { } sampleItem)
                    continue;

                var sampleIndex = _encounteredRecycleKeys![recycleKey];

                for (var i = 0; i < neededCount; i++)
                {
                    try
                    {
                        // Creates the container *and* its content, which is the expensive part we
                        // are moving off the first scroll. The content is deliberately left
                        // attached: reuse then only rebinds data.
                        var container = CreateElement(sampleItem, sampleIndex, recycleKey);
                        container.Measure(availableSize);
                        PushToRecyclePool(recycleKey, container);
                        container.SetCurrentValue(Visual.IsVisibleProperty, false);
                    }
                    catch
                    {
                        break;
                    }
                }
            }

            _isWarmupComplete = true;
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
