using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using Avalonia.Controls.Utils;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Utilities;
using Avalonia.VisualTree;

namespace Avalonia.Controls
{
    /// <summary>
    /// A <see cref="VirtualizingPanel"/> with uniform column and row sizes.
    /// </summary>
    public class VirtualizingUniformGrid : VirtualizingPanel
    {
        private static readonly Rect s_invalidViewport = new(double.PositiveInfinity, double.PositiveInfinity, 0, 0);

        /// <summary>
        /// Defines the <see cref="Rows"/> property.
        /// </summary>
        public static readonly StyledProperty<int> RowsProperty =
            AvaloniaProperty.Register<VirtualizingUniformGrid, int>(nameof(Rows));

        /// <summary>
        /// Defines the <see cref="Columns"/> property.
        /// </summary>
        public static readonly StyledProperty<int> ColumnsProperty =
            AvaloniaProperty.Register<VirtualizingUniformGrid, int>(nameof(Columns));

        /// <summary>
        /// Defines the <see cref="FirstColumn"/> property.
        /// </summary>
        public static readonly StyledProperty<int> FirstColumnProperty =
            AvaloniaProperty.Register<VirtualizingUniformGrid, int>(nameof(FirstColumn));

        private static readonly AttachedProperty<object?> RecycleKeyProperty =
            AvaloniaProperty.RegisterAttached<VirtualizingStackPanel, Control, object?>("RecycleKey");

        private readonly Action<Control, int> _recycleElement;
        private readonly Action<Control> _recycleElementOnItemRemoved;
        private readonly Action<Control, int, int> _updateElementIndex;
        private int _rows;
        private int _columns;
        private int _scrollToIndex = -1;
        private Control? _scrollToElement;
        private bool _isInLayout;
        private bool _isWaitingForViewportUpdate;
        private RealizedGridElements? _measureElements;
        private RealizedGridElements? _realizedElements;
        private ScrollViewer? _scrollViewer;
        private Rect _viewport = s_invalidViewport;
        private Dictionary<object, Stack<Control>>? _recyclePool;
        private Control? _unrealizedFocusedElement;
        private int _unrealizedFocusedIndex = -1;
        private object s_itemIsItsOwnContainer = new object();
        private Size _lastEstimatedElementSize = new Size(25, 25);
        private MeasureViewport _measuredViewport;

        static VirtualizingUniformGrid()
        {
            AffectsMeasure<VirtualizingUniformGrid>(RowsProperty, ColumnsProperty, FirstColumnProperty);
        }

        public VirtualizingUniformGrid()
        {
            _recycleElement = RecycleElement;
            _recycleElementOnItemRemoved = RecycleElementOnItemRemoved;
            _updateElementIndex = UpdateElementIndex;
            EffectiveViewportChanged += OnEffectiveViewportChanged;
        }

        /// <summary>
        /// Specifies the row count. If set to 0, row count will be calculated automatically.
        /// </summary>
        public int Rows
        {
            get => GetValue(RowsProperty);
            set => SetValue(RowsProperty, value);
        }

        /// <summary>
        /// Specifies the column count. If set to 0, column count will be calculated automatically.
        /// </summary>
        public int Columns
        {
            get => GetValue(ColumnsProperty);
            set => SetValue(ColumnsProperty, value);
        }

        /// <summary>
        /// Specifies, for the first row, the column where the items should start.
        /// </summary>
        public int FirstColumn
        {
            get => GetValue(FirstColumnProperty);
            set => SetValue(FirstColumnProperty, value);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            UpdateRowsAndColumns();

            var items = Items;

            if (items.Count == 0)
                return default;

            // If we're bringing an item into view, ignore any layout passes until we receive a new
            // effective viewport.
            if (_isWaitingForViewportUpdate)
                return DesiredSize;

            _isInLayout = true;

            try
            {
                _realizedElements ??= new();
                _measureElements ??= new();

                var viewport = CalculateMeasureViewport(items);

                if (_viewport.Size == default)
                    return DesiredSize;

                if (viewport.viewportIsDisjunct)
                    _realizedElements.RecycleAllElements(_recycleElement);

                RealizeElements(items, availableSize, ref viewport);

                // Now swap the measureElements and realizedElements collection.
                (_measureElements, _realizedElements) = (_realizedElements, _measureElements);
                _measureElements.ResetForReuse();

                _measuredViewport = viewport;

                return CalculateDesiredSize();
            }
            finally
            {
                _isInLayout = false;
            }
        }

        private Size CalculateDesiredSize()
        {
            return new Size(_lastEstimatedElementSize.Width * _columns, _lastEstimatedElementSize.Height * _rows);
        }

        private Rect EstimateViewport()
        {
            var c = this.GetVisualParent();
            var viewport = new Rect();

            if (c is null)
            {
                return viewport;
            }

            while (c is not null)
            {
                if ((c.Bounds.Width != 0 || c.Bounds.Height != 0) &&
                    c.TransformToVisual(this) is Matrix transform)
                {
                    viewport = new Rect(0, 0, c.Bounds.Width, c.Bounds.Height)
                        .TransformToAABB(transform);
                    break;
                }

                c = c?.GetVisualParent();
            }


            return viewport;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var div = Math.DivRem(_measuredViewport.anchorIndex + FirstColumn, _columns, out var rem);
            var x = rem;
            var y = div;

            var width = finalSize.Width / _columns;
            var height = finalSize.Height / _rows;

            foreach (var child in _realizedElements!.Elements)
            {
                while (true)
                {
                    if (child is not null)
                    {
                        var coord = new Vector(x, y);

                        x++;

                        if (x >= _columns)
                        {
                            x = 0;
                            y++;
                        }

                        if (IsCoordVisible(coord, _measuredViewport.anchorCoord, _measuredViewport.endCoord))
                        {
                            child.Arrange(new Rect(coord.X * width, coord.Y * height, width, height));

                            _scrollViewer?.RegisterAnchorCandidate(child);

                            break;
                        }

                        if (coord == new Vector(_columns - 1, _rows - 1))
                            break;
                    }
                    else
                        break;
                }
            }

            return finalSize;
        }

        private void UpdateRowsAndColumns()
        {
            _rows = Rows;
            _columns = Columns;

            if (FirstColumn >= Columns)
            {
                SetCurrentValue(FirstColumnProperty, 0);
            }

            var itemCount = FirstColumn + Items.Count;

            if (_rows == 0)
            {
                if (_columns == 0)
                {
                    _rows = _columns = (int)Math.Ceiling(Math.Sqrt(itemCount));
                }
                else
                {
                    _rows = Math.DivRem(itemCount, _columns, out int rem);

                    if (rem != 0)
                    {
                        _rows++;
                    }
                }
            }
            else if (_columns == 0)
            {
                _columns = Math.DivRem(itemCount, _rows, out int rem);

                if (rem != 0)
                {
                    _columns++;
                }
            }

            if (_realizedElements != null)
            {
                _realizedElements.RowCount = _rows;
                _realizedElements.ColumnCount = _columns;
                _realizedElements.FirstColumn = FirstColumn;
            }

            if (_measureElements != null)
            {
                _measureElements.RowCount = _rows;
                _measureElements.ColumnCount = _columns;
                _measureElements.FirstColumn = FirstColumn;
            }
        }

        private void OnUnrealizedFocusedElementLostFocus(object? sender, RoutedEventArgs e)
        {
            if (_unrealizedFocusedElement is null || sender != _unrealizedFocusedElement)
                return;

            _unrealizedFocusedElement.LostFocus -= OnUnrealizedFocusedElementLostFocus;
            RecycleElement(_unrealizedFocusedElement, _unrealizedFocusedIndex);
            _unrealizedFocusedElement = null;
            _unrealizedFocusedIndex = -1;
        }

        private void OnEffectiveViewportChanged(object? sender, EffectiveViewportChangedEventArgs e)
        {
            var oldViewportStart = new Point(_viewport.Top, _viewport.Left);
            var oldViewportEnd = new Point(_viewport.Bottom, _viewport.Right);

            _viewport = e.EffectiveViewport.Intersect(new(Bounds.Size));
            _isWaitingForViewportUpdate = false;

            var newViewportStart = new Point(_viewport.Top, _viewport.Left);
            var newViewportEnd = new Point(_viewport.Bottom, _viewport.Right);

            if (!MathUtilities.AreClose(oldViewportStart.X, newViewportStart.X) ||
                !MathUtilities.AreClose(oldViewportEnd.X, newViewportEnd.X) || 
                !MathUtilities.AreClose(oldViewportStart.Y, newViewportStart.Y) ||
                !MathUtilities.AreClose(oldViewportEnd.Y, newViewportEnd.Y))
            {
                InvalidateMeasure();
            }
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
            _realizedElements.RecycleElementsBefore(viewport.anchorIndex, _recycleElement);
            _realizedElements.RecycleElementsAfter(viewport.lastIndex, _recycleElement);

            // Start at the anchor element and move forwards, realizing elements.
            do
            {
                if (!IsIndexVisible(index, viewport.anchorCoord, viewport.endCoord))
                {
                    _realizedElements.RecycleElement(index, _recycleElement);
                    ++index;
                    continue;
                }

                var e = GetOrCreateElement(items, index);
                e.Measure(availableSize);

                var size = new Size(e.DesiredSize.Width, e.DesiredSize.Height);

                _measureElements!.Add(index, e, size);

                _lastEstimatedElementSize = size;

                // Calculate the last index and coordinates again, as the first child size is known.
                if (index == 0)
                {
                    (_, int last, _, var lastCoord) = _measureElements.GetOrEstimateAnchorElementForViewport(_viewport.TopLeft, _viewport.BottomRight, items.Count, ref _lastEstimatedElementSize);
                    viewport.lastIndex = last;
                    viewport.endCoord = lastCoord;
                }

                ++index;
            } while (index < items.Count && index <= viewport.lastIndex);
        }

        private bool IsIndexVisible(int index, Vector start, Vector end)
        {
            var div = Math.DivRem(index + FirstColumn, _columns, out var rem);
            var coord = new Vector(rem, div);

            return coord.X >= start.X && coord.Y >= start.Y &&
                coord.X <= end.X && coord.Y <= end.Y;
        }

        private bool IsCoordVisible(Vector coord, Vector start, Vector end)
        {
            return coord.X >= start.X && coord.Y >= start.Y &&
                coord.X <= end.X && coord.Y <= end.Y;
        }

        private MeasureViewport CalculateMeasureViewport(IReadOnlyList<object?> items)
        {
            Debug.Assert(_realizedElements is not null);

            // If the control has not yet been laid out then the effective viewport won't have been set.
            // Try to work it out from an ancestor control.
            var viewport = _viewport != s_invalidViewport ? _viewport : EstimateViewport();

            // Get the viewport in the orientation direction.
            var viewportStart = new Point(viewport.X, viewport.Y);
            var viewportEnd = new Point(viewport.Right, viewport.Bottom);

            // Get or estimate the anchor element from which to start realization.
            var itemCount = items?.Count ?? 0;
            var (anchorIndex, lastIndex, anchor, end) = _realizedElements.GetOrEstimateAnchorElementForViewport(
                viewportStart,
                viewportEnd,
                itemCount,
                ref _lastEstimatedElementSize);

            // Check if the anchor element is not within the currently realized elements.
            var disjunct = anchorIndex < _realizedElements.FirstIndex ||
                anchorIndex > _realizedElements.LastIndex;

            return new MeasureViewport
            {
                anchorIndex = anchorIndex,
                anchorCoord = anchor,
                viewportStart = viewportStart,
                viewportEnd = viewportEnd,
                viewportIsDisjunct = disjunct,
                endCoord = end,
                lastIndex = lastIndex
            };
        }

        private Control GetOrCreateElement(IReadOnlyList<object?> items, int index)
        {
            Debug.Assert(ItemContainerGenerator is not null);

            var e = GetRealizedElement(index);

            if (e is null)
            {
                var item = items[index];
                var generator = ItemContainerGenerator!;

                if (generator.NeedsContainer(item, index, out var recycleKey))
                {
                    e = GetRecycledElement(item, index, recycleKey) ??
                        CreateElement(item, index, recycleKey);
                }
                else
                {
                    e = GetItemAsOwnContainer(item, index);
                }
            }

            return e;
        }

        private Control? GetRealizedElement(int index)
        {
            if (_scrollToIndex == index)
                return _scrollToElement;
            return _realizedElements?.GetElement(index);
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

            controlItem.IsVisible = true;
            return controlItem;
        }

        private Control? GetRecycledElement(object? item, int index, object? recycleKey)
        {
            Debug.Assert(ItemContainerGenerator is not null);

            if (recycleKey is null)
                return null;

            var generator = ItemContainerGenerator!;

            if (_unrealizedFocusedIndex == index && _unrealizedFocusedElement is not null)
            {
                var element = _unrealizedFocusedElement;
                _unrealizedFocusedElement.LostFocus -= OnUnrealizedFocusedElementLostFocus;
                _unrealizedFocusedElement = null;
                _unrealizedFocusedIndex = -1;
                return element;
            }

            if (_recyclePool?.TryGetValue(recycleKey, out var recyclePool) == true && recyclePool.Count > 0)
            {
                var recycled = recyclePool.Pop();
                recycled.IsVisible = true;
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
            Debug.Assert(ItemContainerGenerator is not null);

            _scrollViewer?.UnregisterAnchorCandidate(element);

            var recycleKey = element.GetValue(RecycleKeyProperty);
            Debug.Assert(recycleKey is not null);

            if (recycleKey == s_itemIsItsOwnContainer)
            {
                element.IsVisible = false;
            }
            else if (element.IsKeyboardFocusWithin)
            {
                _unrealizedFocusedElement = element;
                _unrealizedFocusedIndex = index;
                _unrealizedFocusedElement.LostFocus += OnUnrealizedFocusedElementLostFocus;
            }
            else
            {
                ItemContainerGenerator!.ClearItemContainer(element);
                PushToRecyclePool(recycleKey, element);
                element.IsVisible = false;
            }
        }

        private void RecycleElementOnItemRemoved(Control element)
        {
            Debug.Assert(ItemContainerGenerator is not null);

            var recycleKey = element.GetValue(RecycleKeyProperty);
            Debug.Assert(recycleKey is not null);

            if (recycleKey == s_itemIsItsOwnContainer)
            {
                RemoveInternalChild(element);
            }
            else
            {
                ItemContainerGenerator!.ClearItemContainer(element);
                PushToRecyclePool(recycleKey, element);
                element.IsVisible = false;
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
                case NotifyCollectionChangedAction.Move:
                    _realizedElements.ItemsRemoved(e.OldStartingIndex, e.OldItems!.Count, _updateElementIndex, _recycleElementOnItemRemoved);
                    _realizedElements.ItemsInserted(e.NewStartingIndex, e.NewItems!.Count, _updateElementIndex);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    _realizedElements.ItemsReset(_recycleElementOnItemRemoved);
                    break;
            }
        }

        private void UpdateElementIndex(Control element, int oldIndex, int newIndex)
        {
            Debug.Assert(ItemContainerGenerator is not null);

            ItemContainerGenerator.ItemContainerIndexChanged(element, oldIndex, newIndex);
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            _scrollViewer = this.FindAncestorOfType<ScrollViewer>();
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            _scrollViewer = null;
        }

        protected internal override Control? ScrollIntoView(int index)
        {
            var items = Items;

            if (_isInLayout || index < 0 || index >= items.Count || _realizedElements is null)
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
                _scrollToElement = GetOrCreateElement(items, index);
                _scrollToElement.Measure(Size.Infinity);
                _scrollToIndex = index;

                // Get the expected position of the elment and put it in place.
                var anchor = _realizedElements.GetElementCoord(index);
                var rect = new Rect(anchor.X, anchor.Y, _scrollToElement.DesiredSize.Width, _scrollToElement.DesiredSize.Height);
                _scrollToElement.Arrange(rect);

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
                _scrollToElement.BringIntoView();

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

                var result = _scrollToElement;
                _scrollToElement = null;
                _scrollToIndex = -1;
                return result;
            }

            return null;
        }

        protected internal override Control? ContainerFromIndex(int index)
        {
            if (index < 0 || index >= Items.Count)
                return null;
            if (_realizedElements?.GetElement(index) is { } realized)
                return realized;
            if (Items[index] is Control c && c.GetValue(RecycleKeyProperty) == s_itemIsItsOwnContainer)
                return c;
            return null;
        }

        protected internal override int IndexFromContainer(Control container) => _realizedElements?.GetIndex(container) ?? -1;

        protected internal override IEnumerable<Control>? GetRealizedContainers()
        {
            return _realizedElements?.Elements.Where(x => x is not null)!;
        }

        protected override IInputElement? GetControl(NavigationDirection direction, IInputElement? from, bool wrap)
        {
            var count = Items.Count;

            if (count == 0 || from is not Control fromControl)
                return null;

            var fromIndex = from != null ? IndexFromContainer(fromControl) : -1;
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
                        --toIndex;
                    break;
                case NavigationDirection.Right:
                        ++toIndex;
                    break;
                case NavigationDirection.Up:
                        toIndex -= _columns;
                    break;
                case NavigationDirection.Down:
                        toIndex += _columns;
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

        private struct MeasureViewport
        {
            public int anchorIndex;
            public Vector anchorCoord;
            public Point viewportStart;
            public Point viewportEnd;
            public int lastIndex;
            public bool viewportIsDisjunct;
            public Vector endCoord;
        }
    }
}
