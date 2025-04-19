﻿using System;
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
        /// Defines the <see cref="BufferFactor"/> property.
        /// </summary>
        public static readonly StyledProperty<double> BufferFactorProperty =
            AvaloniaProperty.Register<VirtualizingStackPanel, double>(nameof(BufferFactor), 0.0, 
                validate: v => v is >= 0 and <= 2);

        /// <summary>
        /// Gets or sets the factor to determine how much additional space to maintain above and below the viewport.
        /// A value of 0.5 means half the viewport size will be buffered on each side (up-down or left-right)
        /// This uses more memory as more UI elements are realized, but greatly reduces the number of Measure-Arrange
        /// cycles which can cause heavy GC pressure depending on the complexity of the item layouts.
        /// </summary>
        public double BufferFactor
        {
            get => GetValue(BufferFactorProperty);
            set => SetValue(BufferFactorProperty, value);
        }
        
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
        private Dictionary<object, Stack<Control>>? _recyclePool;
        private Control? _focusedElement;
        private int _focusedIndex = -1;
        private Control? _realizingElement;
        private int _realizingIndex = -1;

        /// <summary>
        /// The factor to determine additional space to maintain above and below the viewport.
        /// </summary>
        private double _bufferFactor; 
        
        private bool _hasReachedStart = false;
        private bool _hasReachedEnd = false;
        private bool _cacheElementMeasurements = false;
        private Size _previousSize;
        private readonly HashSet<Control> _unchangedElements = new();
        private readonly bool _traceVirtualization = false;

        private Rect _extendedViewport;

        static VirtualizingStackPanel()
        {
            BufferFactorProperty.Changed.AddClassHandler<VirtualizingStackPanel>((x, e) => x.OnBufferFactorChanged(e));
        }

        public VirtualizingStackPanel()
        {
            _recycleElement = RecycleElement;
            _recycleElementOnItemRemoved = RecycleElementOnItemRemoved;
            _updateElementIndex = UpdateElementIndex;

            _bufferFactor = Math.Max(0, BufferFactor);
            // Initialize extended viewport with empty rect
            _extendedViewport = new Rect();
            
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
        /// Returns the extended viewport that contains any visible elements and the additional elements for fast scrolling (viewport * BufferFactor * 2)
        /// </summary>
        internal Rect ExtendedViewPort => _extendedViewport;

        protected override Size MeasureOverride(Size availableSize)
        {
            var items = Items;

            if (items.Count == 0)
                return default;

            var orientation = Orientation;

            // If we're bringing an item into view, ignore any layout passes until we receive a new
            // effective viewport.
            if (_isWaitingForViewportUpdate)
                return EstimateDesiredSize(orientation, items.Count);

            _isInLayout = true;

            try
            {
                _realizedElements?.ValidateStartU(Orientation);
                _realizedElements ??= new();
                _measureElements ??= new();

                // We need to set the lastEstimatedElementSizeU before calling CalculateDesiredSize()
                _ = EstimateElementSizeU();

                // We handle horizontal and vertical layouts here so X and Y are abstracted to:
                // - Horizontal layouts: U = horizontal, V = vertical
                // - Vertical layouts: U = vertical, V = horizontal
                var viewport = CalculateMeasureViewport(orientation, items);

                // If the viewport is disjunct then we can recycle everything.
                if (viewport.viewportIsDisjunct)
                    _realizedElements.RecycleAllElements(_recycleElement);

                // Do the measure, creating/recycling elements as necessary to fill the viewport. Don't
                // write to _realizedElements yet, only _measureElements.
                RealizeElements(items, availableSize, ref viewport);

                // Now swap the measureElements and realizedElements collection.
                (_measureElements, _realizedElements) = (_realizedElements, _measureElements);
                _measureElements.ResetForReuse();

                // If there is a focused element is outside the visible viewport (i.e.
                // _focusedElement is non-null), ensure it's measured.
                MeasureElement(_focusedElement, availableSize);

                return CalculateDesiredSize(orientation, items.Count, viewport);
            }
            finally
            {
                _cacheElementMeasurements = false;
                _isInLayout = false;
                _previousSize = Bounds.Size;
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

                        // Always arrange the element, even if it's outside the visible viewport
                        // but not if it is an unchanged element
                        if(!_unchangedElements.Contains(e))
                            ArrangeElement(e, rect);
                        else if (_traceVirtualization)
                            Debug.WriteLine($"Skipped arranging {e.DataContext}");
                
                        // Register as anchor candidate only if it's in the actual viewport (not just the extended one)
                        var elementBounds = orientation == Orientation.Horizontal ?
                            new Rect(u, 0, sizeU, finalSize.Height) :
                            new Rect(0, u, finalSize.Width, sizeU);
                    
                        if (_viewport.Intersects(elementBounds))
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

                    ArrangeElement(_focusedElement, rect);
                }

                return finalSize;
            }
            finally
            {
                _isInLayout = false;

                // this collection is only valid for one single Measure/Arrange cycle
                _unchangedElements.Clear();
                
                RaiseEvent(new RoutedEventArgs(Orientation == Orientation.Horizontal ? HorizontalSnapPointsChangedEvent : VerticalSnapPointsChangedEvent));
            }
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            _scrollAnchorProvider = this.FindAncestorOfType<IScrollAnchorProvider>();
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            _scrollAnchorProvider = null;
        }

        protected override void OnItemsChanged(IReadOnlyList<object?> items, NotifyCollectionChangedEventArgs e)
        {
            InvalidateMeasure();

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
                    _realizedElements.ItemsReset(_recycleElementOnItemRemoved);
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

            if (_isInLayout || index < 0 || index >= items.Count || _realizedElements is null || !IsEffectivelyVisible)
                return null;

            if (GetRealizedElement(index) is Control element)
            {
                element.BringIntoView();
                return element;
            }
            else if (this.GetVisualRoot() is ILayoutRoot root)
            {
                // Create and measure the element to be brought into view. Store it in a field so that
                // it can be re-used in the layout pass.
                var (scrollToElement, _) = GetOrCreateElement(items, index);

                MeasureElement(scrollToElement, Size.Infinity);

                // Get the expected position of the element and put it in place.
                var anchorU = GetOrEstimateElementU(index);
                var rect = Orientation == Orientation.Horizontal ?
                    new Rect(anchorU, 0, scrollToElement.DesiredSize.Width, scrollToElement.DesiredSize.Height) :
                    new Rect(0, anchorU, scrollToElement.DesiredSize.Width, scrollToElement.DesiredSize.Height);
                ArrangeElement(scrollToElement, rect);

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
                _isWaitingForViewportUpdate = !_viewport.Contains(rect);
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

            if (_scrollToIndex >= 0 && _scrollToElement is not null)
            {
                anchorIndex = _scrollToIndex;
                anchorU = orientation == Orientation.Horizontal ? _scrollToElement.Bounds.Left : _scrollToElement.Bounds.Top;
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

            // Store and return the estimate.
            return _lastEstimatedElementSizeU = total / divisor;
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
            // StartU being valid. Estimate the index using only the estimated element size.
            var estimatedSize = EstimateElementSizeU();

            // Estimate the element at the start of the viewport.
            var startIndex = Math.Min((int)(viewportStartU / estimatedSize), itemCount - 1);
            index = startIndex;
            position = startIndex * estimatedSize;
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
                var (e, existed) = GetOrCreateElement(items, index);
                _realizingElement = e;
                
                // Skip re-measuring when an in-view element with correct item
                // only needs measurement due to scroll-based viewport changes
                if (!existed || !_cacheElementMeasurements)
                    MeasureElement(e, availableSize);
                else
                    _unchangedElements.Add(e);
                
                if (_traceVirtualization && existed && _cacheElementMeasurements)
                {
                    Debug.WriteLine($"Skipped measuring {items[index]}");
                }

                var sizeU = horizontal ? e.DesiredSize.Width : e.DesiredSize.Height;
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
                var (e, existed) = GetOrCreateElement(items, index);
                
                // Skip re-measuring when an in-view element with correct item
                // only needs measurement due to scroll-based viewport changes
                if(!existed || !_cacheElementMeasurements)
                    MeasureElement(e, availableSize);
                else
                    _unchangedElements.Add(e);

                if (_traceVirtualization && existed && _cacheElementMeasurements)
                {
                    Debug.WriteLine($"Skipped measuring {items[index]}");
                }

                var sizeU = horizontal ? e.DesiredSize.Width : e.DesiredSize.Height;
                var sizeV = horizontal ? e.DesiredSize.Height : e.DesiredSize.Width;
                u -= sizeU;

                _measureElements!.Add(index, e, u, sizeU);
                viewport.measuredV = Math.Max(viewport.measuredV, sizeV);
                --index;
            }
            
            // Check if we reached the start of the collection
            _hasReachedStart = index < 0;

            // We can now recycle elements before the first element.
            _realizedElements.RecycleElementsBefore(index + 1, _recycleElement);
        }

        private (Control e, bool existed) GetOrCreateElement(IReadOnlyList<object?> items, int index)
        {
            Debug.Assert(ItemContainerGenerator is not null);

            if ((GetRealizedElement(index) ??
                 GetRealizedElement(index, ref _focusedIndex, ref _focusedElement) ??
                 GetRealizedElement(index, ref _scrollToIndex, ref _scrollToElement)) is { } realized)
                return (realized, true);

            var item = items[index];
            var generator = ItemContainerGenerator!;

            if (generator.NeedsContainer(item, index, out var recycleKey))
            {
                return (GetRecycledElement(item, index, recycleKey) ??
                       CreateElement(item, index, recycleKey), false);
            }
            else
            {
                return (GetItemAsOwnContainer(item, index), false);
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
                var recycled = recyclePool.Pop();
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

            pool.Push(element);
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
            // If we are at the start of the list, append 2 * BufferFactor additional items
            // If we are at the end of the list, prepend 2 * BufferFactor additional items
            // - this way we always maintain "2 * BufferFactor * element" items. 
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
            
            if(_traceVirtualization) Debug.Write($"old xVP:{oldExtendedViewportStart}.-{oldExtendedViewportEnd} - new VP:{newViewportStart}-{newViewportEnd}");

            // Case 1: Viewport has changed significantly
            if (!MathUtilities.AreClose(oldViewportStart, newViewportStart) ||
                !MathUtilities.AreClose(oldViewportEnd, newViewportEnd))
            {
                // Case 1a: The new viewport exceeds the old extended viewport
                if (newViewportStart < oldExtendedViewportStart || 
                    newViewportEnd > oldExtendedViewportEnd)
                {
                    needsMeasure = true;
                    if(_traceVirtualization) Debug.Write(" Case 1a");
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
                            if(_traceVirtualization) Debug.Write(" Case 1b UP");
                        }
                        
                        // If scrolling down/right and nearing the bottom/right edge of realized elements
                        if (newViewportEnd > oldViewportEnd && 
                            newExtendedViewportEnd - newViewportEnd < bufferSize)
                        {
                            // Edge case: We're at the last item with excess measurement space.
                            // Skip re-measuring since we're at the list end and it won't change the result.
                            // This prevents redundant Measure-Arrange cycles when at list beginning.
                            nearingEdge = !_hasReachedEnd;
                            if(_traceVirtualization) Debug.Write(" Case 1b DOWN");
                        }
                    }
                    else
                    {
                        nearingEdge = true;
                    }
                    
                    needsMeasure = nearingEdge;
                }
            }

            if (needsMeasure)
            {
                if(_traceVirtualization) Debug.WriteLine(" => InvalidateMeasure");
                
                // only store the new "old" extended viewport if we _did_ actually measure
                _extendedViewport = extendedViewPort;
                
                // Cache element measurements when only scroll position changes
                // Re-measure only when the cross-axis dimension changes
                // Example: In vertical scrolling, cache if only height changes; re-measure if width changes
                bool sizeChanged = vertical
                    ? Math.Abs(Bounds.Width - _previousSize.Width) > 0.1               
                    : Math.Abs(Bounds.Height - _previousSize.Height) > 0.1 ;
                
                _cacheElementMeasurements = !sizeChanged;
                InvalidateMeasure();
            }
            else
            {
                if(_traceVirtualization) Debug.WriteLine("");
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
        }

        private void OnBufferFactorChanged(AvaloniaPropertyChangedEventArgs e)
        {
            var newValue = (double)e.NewValue!;
            _bufferFactor = newValue;
    
            // Force a recalculation of the extended viewport on the next layout pass
            InvalidateMeasure();
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

        /// <summary>
        /// Used to call Measure on elements - allows to be overridden in unit tests
        /// </summary>
        /// <param name="element"></param>
        /// <param name="availableSize"></param>
        protected internal virtual void MeasureElement(Control? element, Size availableSize) => element?.Measure(availableSize);
        
        /// <summary>
        /// Used to call Arrange on elements - allows to be overridden in unit tests
        /// </summary>
        /// <param name="element"></param>
        /// <param name="rect"></param>
        protected internal virtual void ArrangeElement(Control? element, Rect rect) => element?.Arrange(rect);
        
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
