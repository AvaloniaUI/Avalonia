using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Utils;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
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

        private static readonly AttachedProperty<bool> ItemIsOwnContainerProperty =
            AvaloniaProperty.RegisterAttached<VirtualizingStackPanel, Control, bool>("ItemIsOwnContainer");

        private static readonly Rect s_invalidViewport = new(double.PositiveInfinity, double.PositiveInfinity, 0, 0);
        private readonly Action<Control, int> _recycleElement;
        private readonly Action<Control> _recycleElementOnItemRemoved;
        private readonly Action<Control, int, int> _updateElementIndex;
        private int _anchorIndex = -1;
        private Control? _anchorElement;
        private bool _isInLayout;
        private bool _isWaitingForViewportUpdate;
        private double _lastEstimatedElementSizeU = 25;
        private RealizedElementList? _measureElements;
        private RealizedElementList? _realizedElements;
        private Rect _viewport = s_invalidViewport;
        private Stack<Control>? _recyclePool;
        private Control? _unrealizedFocusedElement;
        private int _unrealizedFocusedIndex = -1;

        public VirtualizingStackPanel()
        {
            _recycleElement = RecycleElement;
            _recycleElementOnItemRemoved = RecycleElementOnItemRemoved;
            _updateElementIndex = UpdateElementIndex;
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
            get { return GetValue(AreHorizontalSnapPointsRegularProperty); }
            set { SetValue(AreHorizontalSnapPointsRegularProperty, value); }
        }

        /// <summary>
        /// Gets or sets whether the vertical snap points for the <see cref="VirtualizingStackPanel"/> are equidistant from each other.
        /// </summary>
        public bool AreVerticalSnapPointsRegular
        {
            get { return GetValue(AreVerticalSnapPointsRegularProperty); }
            set { SetValue(AreVerticalSnapPointsRegularProperty, value); }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            if (!IsEffectivelyVisible)
                return default;

            _isInLayout = true;

            try
            {
                var items = Items;
                var orientation = Orientation;

                _realizedElements ??= new();
                _measureElements ??= new();

                // If we're bringing an item into view, ignore any layout passes until we receive a new
                // effective viewport.
                if (_isWaitingForViewportUpdate)
                {
                    var sizeV = orientation == Orientation.Horizontal ? DesiredSize.Height : DesiredSize.Width;
                    return CalculateDesiredSize(orientation, items, sizeV);
                }

                // We handle horizontal and vertical layouts here so X and Y are abstracted to:
                // - Horizontal layouts: U = horizontal, V = vertical
                // - Vertical layouts: U = vertical, V = horizontal
                var viewport = CalculateMeasureViewport(items);

                // Recycle elements outside of the expected range.
                _realizedElements.RecycleElementsBefore(viewport.firstIndex, _recycleElement);
                _realizedElements.RecycleElementsAfter(viewport.lastIndex, _recycleElement);

                // Do the measure, creating/recycling elements as necessary to fill the viewport. Don't
                // write to _realizedElements yet, only _measureElements.
                GenerateElements(availableSize, ref viewport);

                // Now we know what definitely fits, recycle anything left over.
                _realizedElements.RecycleElementsAfter(_measureElements.LastIndex, _recycleElement);

                // And swap the measureElements and realizedElements collection.
                (_measureElements, _realizedElements) = (_realizedElements, _measureElements);
                _measureElements.ResetForReuse();

                return CalculateDesiredSize(orientation, items, viewport.measuredV);
            }
            finally
            {
                _isInLayout = false;
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
                        u += orientation == Orientation.Horizontal ? rect.Width : rect.Height;
                    }
                }

                return finalSize;
            }
            finally
            {
                _isInLayout = false;

                RaiseEvent(new RoutedEventArgs(Orientation == Orientation.Horizontal ? HorizontalSnapPointsChangedEvent : VerticalSnapPointsChangedEvent));
            }
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

        protected override IInputElement? GetControl(NavigationDirection direction, IInputElement? from, bool wrap)
        {
            var count = Items.Count;

            if (count == 0 || from is not Control fromControl)
                return null;

            var horiz = Orientation == Orientation.Horizontal;
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

        protected internal override Control? ContainerFromIndex(int index) => _realizedElements?.GetElement(index);
        protected internal override int IndexFromContainer(Control container) => _realizedElements?.GetIndex(container) ?? -1;

        protected internal override Control? ScrollIntoView(int index)
        {
            var items = Items;

            if (_isInLayout || index < 0 || index >= items.Count)
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
                _anchorElement = GetOrCreateElement(items, index);
                _anchorElement.Measure(Size.Infinity);
                _anchorIndex = index;

                // Get the expected position of the elment and put it in place.
                var anchorU = GetOrEstimateElementPosition(index);
                var rect = Orientation == Orientation.Horizontal ?
                    new Rect(anchorU, 0, _anchorElement.DesiredSize.Width, _anchorElement.DesiredSize.Height) :
                    new Rect(0, anchorU, _anchorElement.DesiredSize.Width, _anchorElement.DesiredSize.Height);
                _anchorElement.Arrange(rect);

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
                _anchorElement.BringIntoView();

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

                var result = _anchorElement;
                _anchorElement = null;
                _anchorIndex = -1;
                return result;
            }

            return null;
        }

        internal IReadOnlyList<Control?> GetRealizedElements()
        {
            return _realizedElements?.Elements ?? Array.Empty<Control>();
        }

        private MeasureViewport CalculateMeasureViewport(IReadOnlyList<object?> items)
        {
            Debug.Assert(_realizedElements is not null);

            // If the control has not yet been laid out then the effective viewport won't have been set.
            // Try to work it out from an ancestor control.
            var viewport = _viewport != s_invalidViewport ? _viewport : EstimateViewport();

            // Get the viewport in the orientation direction.
            var viewportStart = Orientation == Orientation.Horizontal ? viewport.X : viewport.Y;
            var viewportEnd = Orientation == Orientation.Horizontal ? viewport.Right : viewport.Bottom;

            var (firstIndex, firstIndexU) = _realizedElements.GetIndexAt(viewportStart);
            var (lastIndex, _) = _realizedElements.GetIndexAt(viewportEnd);
            var estimatedElementSize = -1.0;
            var itemCount = items?.Count ?? 0;
            var maxIndex = Math.Max(itemCount - 1, 0);

            if (firstIndex == -1)
            {
                estimatedElementSize = EstimateElementSizeU();
                firstIndex = Math.Min((int)(viewportStart / estimatedElementSize), maxIndex);
                firstIndexU = firstIndex * estimatedElementSize;
            }

            if (lastIndex == -1)
            {
                if (estimatedElementSize == -1)
                    estimatedElementSize = EstimateElementSizeU();
                lastIndex = Math.Min((int)(viewportEnd / estimatedElementSize), maxIndex);
            }

            return new MeasureViewport
            {
                firstIndex = firstIndex,
                lastIndex = lastIndex,
                viewportUStart = viewportStart,
                viewportUEnd = viewportEnd,
                startU = firstIndexU,
            };
        }

        private Size CalculateDesiredSize(Orientation orientation, IReadOnlyList<object?> items, double sizeV)
        {
            var sizeU = EstimateElementSizeU() * items.Count;

            if (double.IsInfinity(sizeU) || double.IsNaN(sizeU))
                throw new InvalidOperationException("Invalid calculated size.");

            return orientation == Orientation.Horizontal ?
                new Size(sizeU, sizeV) :
                new Size(sizeV, sizeU);
        }

        private double EstimateElementSizeU()
        {
            if (_realizedElements is null)
                return _lastEstimatedElementSizeU;

            var result = _realizedElements.EstimateElementSizeU();
            if (result >= 0)
                _lastEstimatedElementSizeU = result;
            return _lastEstimatedElementSizeU;
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

        private void GenerateElements(Size availableSize, ref MeasureViewport viewport)
        {
            Debug.Assert(_measureElements is not null);

            var items = Items;
            var horizontal = Orientation == Orientation.Horizontal;
            var index = viewport.firstIndex;
            var u = viewport.startU;

            // The layout is likely invalid. Don't create any elements and instead rely on our previous
            // element size estimates to calculate a new desired size and trigger a new layout pass.
            if (index >= items.Count)
                return;
            do
            {
                var e = GetOrCreateElement(items, index);
                e.Measure(availableSize);

                var sizeU = horizontal ? e.DesiredSize.Width : e.DesiredSize.Height;
                var sizeV = horizontal ? e.DesiredSize.Height : e.DesiredSize.Width;

                _measureElements!.Add(index, e, u, sizeU);
                viewport.measuredV = Math.Max(viewport.measuredV, sizeV);

                u += sizeU;
                ++index;
            } while (u < viewport.viewportUEnd && index < items.Count);
        }

        private Control GetOrCreateElement(IReadOnlyList<object?> items, int index)
        {
            var e = GetRealizedElement(index) ??
                GetItemIsOwnContainer(items, index) ??
                GetRecycledElement(items, index) ??
                CreateElement(items, index);
            InvalidateHack(e);
            return e;
        }

        private Control? GetRealizedElement(int index)
        {
            if (_anchorIndex == index)
                return _anchorElement;
            return _realizedElements?.GetElement(index);
        }

        private Control? GetItemIsOwnContainer(IReadOnlyList<object?> items, int index)
        {
            var item = items[index];

            if (item is Control controlItem)
            {
                var generator = ItemContainerGenerator!;

                if (controlItem.IsSet(ItemIsOwnContainerProperty))
                {
                    controlItem.IsVisible = true;
                    generator.ItemContainerPrepared(controlItem, item, index);
                    return controlItem;
                }
                else if (generator.IsItemItsOwnContainer(controlItem))
                {
                    generator.PrepareItemContainer(controlItem, controlItem, index);
                    AddInternalChild(controlItem);
                    controlItem.SetValue(ItemIsOwnContainerProperty, true);
                    generator.ItemContainerPrepared(controlItem, item, index);
                    return controlItem;
                }
            }

            return null;
        }

        private Control? GetRecycledElement(IReadOnlyList<object?> items, int index)
        {
            Debug.Assert(ItemContainerGenerator is not null);

            var generator = ItemContainerGenerator!;
            var item = items[index];

            if (_unrealizedFocusedIndex == index)
            {
                var element = _unrealizedFocusedElement;
                _unrealizedFocusedElement = null;
                _unrealizedFocusedIndex = -1;
                return element;
            }
            if (_recyclePool?.Count > 0)
            {
                var recycled = _recyclePool.Pop();
                recycled.IsVisible = true;
                generator.PrepareItemContainer(recycled, item, index);
                generator.ItemContainerPrepared(recycled, item, index);
                return recycled;
            }

            return null;
        }

        private Control CreateElement(IReadOnlyList<object?> items, int index)
        {
            Debug.Assert(ItemContainerGenerator is not null);

            var generator = ItemContainerGenerator!;
            var item = items[index];
            var container = generator.CreateContainer();

            generator.PrepareItemContainer(container, item, index);
            AddInternalChild(container);
            generator.ItemContainerPrepared(container, item, index);

            return container;
        }

        private double GetOrEstimateElementPosition(int index)
        {
            var estimatedElementSize = EstimateElementSizeU();
            return index * estimatedElementSize;
        }

        private void RecycleElement(Control element, int index)
        {
            Debug.Assert(ItemContainerGenerator is not null);
            
            if (element.IsSet(ItemIsOwnContainerProperty))
            {
                element.IsVisible = false;
            }
            else if (element.IsKeyboardFocusWithin)
            {
                _unrealizedFocusedElement = element;
                _unrealizedFocusedIndex = index;
            }
            else
            {
                ItemContainerGenerator!.ClearItemContainer(element);
                _recyclePool ??= new();
                _recyclePool.Push(element);
                element.IsVisible = false;
            }
        }

        private void RecycleElementOnItemRemoved(Control element)
        {
            Debug.Assert(ItemContainerGenerator is not null);

            if (element.IsSet(ItemIsOwnContainerProperty))
            {
                RemoveInternalChild(element);
            }
            else
            {
                ItemContainerGenerator!.ClearItemContainer(element);
                _recyclePool ??= new();
                _recyclePool.Push(element);
                element.IsVisible = false;
            }
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

            _viewport = e.EffectiveViewport;
            _isWaitingForViewportUpdate = false;

            var newViewportStart = vertical ? _viewport.Top : _viewport.Left;
            var newViewportEnd = vertical ? _viewport.Bottom : _viewport.Right;

            if (!MathUtilities.AreClose(oldViewportStart, newViewportStart) ||
                !MathUtilities.AreClose(oldViewportEnd, newViewportEnd))
            {
                InvalidateMeasure();
            }
        }

        private static void InvalidateHack(Control c)
        {
            bool HasInvalidations(Control c)
            {
                if (!c.IsMeasureValid)
                    return true;

                for (var i = 0; i < c.VisualChildren.Count; ++i)
                {
                    if (c.VisualChildren[i] is Control child)
                    {
                        if (!child.IsMeasureValid || HasInvalidations(child))
                            return true;
                    }
                }

                return false;
            }

            void Invalidate(Control c)
            {
                c.InvalidateMeasure();
                for (var i = 0; i < c.VisualChildren.Count; ++i)
                {
                    if (c.VisualChildren[i] is Control child)
                        Invalidate(child);
                }
            }

            if (HasInvalidations(c))
                Invalidate(c);
        }

        /// <inheritdoc/>
        public IReadOnlyList<double> GetIrregularSnapPoints(Orientation orientation, SnapPointsAlignment snapPointsAlignment)
        {
            var snapPoints = new List<double>();

            switch (orientation)
            {
                case Orientation.Horizontal:
                    if (AreHorizontalSnapPointsRegular)
                        throw new InvalidOperationException();
                    if (Orientation == Orientation.Horizontal)
                    {
                        var averageElementSize = EstimateElementSizeU();
                        double snapPoint = 0;
                        for (var i = 0; i < Items.Count; i++)
                        {
                            var container = ContainerFromIndex(i);
                            if (container != null)
                            {
                                switch (snapPointsAlignment)
                                {
                                    case SnapPointsAlignment.Near:
                                        snapPoint = container.Bounds.Left;
                                        break;
                                    case SnapPointsAlignment.Center:
                                        snapPoint = container.Bounds.Center.X;
                                        break;
                                    case SnapPointsAlignment.Far:
                                        snapPoint = container.Bounds.Right;
                                        break;
                                }
                            }
                            else
                            {
                                if (snapPoint == 0)
                                {
                                    switch (snapPointsAlignment)
                                    {
                                        case SnapPointsAlignment.Center:
                                            snapPoint = averageElementSize / 2;
                                            break;
                                        case SnapPointsAlignment.Far:
                                            snapPoint = averageElementSize;
                                            break;
                                    }
                                }
                                else
                                    snapPoint += averageElementSize;
                            }

                            snapPoints.Add(snapPoint);
                        }
                    }
                    break;
                case Orientation.Vertical:
                    if (AreVerticalSnapPointsRegular)
                        throw new InvalidOperationException();
                    if (Orientation == Orientation.Vertical)
                    {
                        var averageElementSize = EstimateElementSizeU();
                        double snapPoint = 0;
                        for (var i = 0; i < Items.Count; i++)
                        {
                            var container = ContainerFromIndex(i);
                            if (container != null)
                            {
                                switch (snapPointsAlignment)
                                {
                                    case SnapPointsAlignment.Near:
                                        snapPoint = container.Bounds.Top;
                                        break;
                                    case SnapPointsAlignment.Center:
                                        snapPoint = container.Bounds.Center.Y;
                                        break;
                                    case SnapPointsAlignment.Far:
                                        snapPoint = container.Bounds.Bottom;
                                        break;
                                }
                            }
                            else
                            {
                                if (snapPoint == 0)
                                {
                                    switch (snapPointsAlignment)
                                    {
                                        case SnapPointsAlignment.Center:
                                            snapPoint = averageElementSize / 2;
                                            break;
                                        case SnapPointsAlignment.Far:
                                            snapPoint = averageElementSize;
                                            break;
                                    }
                                }
                                else
                                    snapPoint += averageElementSize;
                            }

                            snapPoints.Add(snapPoint);
                        }
                    }
                    break;
            }

            return snapPoints;
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
        /// Stores the realized element state for a <see cref="VirtualizingStackPanel"/>.
        /// </summary>
        internal class RealizedElementList
        {
            private int _firstIndex;
            private List<Control?>? _elements;
            private List<double>? _sizes;
            private double _startU;
            private bool _startUUnstable;

            /// <summary>
            /// Gets the number of realized elements.
            /// </summary>
            public int Count => _elements?.Count ?? 0;

            /// <summary>
            /// Gets the index of the first realized element, or -1 if no elements are realized.
            /// </summary>
            public int FirstIndex => _elements?.Count > 0 ? _firstIndex : -1;

            /// <summary>
            /// Gets the index of the last realized element, or -1 if no elements are realized.
            /// </summary>
            public int LastIndex => _elements?.Count > 0 ? _firstIndex + _elements.Count - 1 : -1;

            /// <summary>
            /// Gets the elements.
            /// </summary>
            public IReadOnlyList<Control?> Elements => _elements ??= new List<Control?>();

            /// <summary>
            /// Gets the sizes of the elements on the primary axis.
            /// </summary>
            public IReadOnlyList<double> SizeU => _sizes ??= new List<double>();

            /// <summary>
            /// Gets the position of the first element on the primary axis.
            /// </summary>
            public double StartU => _startU;

            /// <summary>
            /// Adds a newly realized element to the collection.
            /// </summary>
            /// <param name="index">The index of the element.</param>
            /// <param name="element">The element.</param>
            /// <param name="u">The position of the elemnt on the primary axis.</param>
            /// <param name="sizeU">The size of the element on the primary axis.</param>
            public void Add(int index, Control element, double u, double sizeU)
            {
                if (index < 0)
                    throw new ArgumentOutOfRangeException(nameof(index));

                _elements ??= new List<Control?>();
                _sizes ??= new List<double>();

                if (Count == 0)
                {
                    _elements.Add(element);
                    _sizes.Add(sizeU);
                    _startU = u;
                    _firstIndex = index;
                }
                else if (index == LastIndex + 1)
                {
                    _elements.Add(element);
                    _sizes.Add(sizeU);
                }
                else if (index == FirstIndex - 1)
                {
                    --_firstIndex;
                    _elements.Insert(0, element);
                    _sizes.Insert(0, sizeU);
                    _startU = u;
                }
                else
                {
                    throw new NotSupportedException("Can only add items to the beginning or end of realized elements.");
                }
            }

            /// <summary>
            /// Gets the element at the specified index, if realized.
            /// </summary>
            /// <param name="index">The index in the source collection of the element to get.</param>
            /// <returns>The element if realized; otherwise null.</returns>
            public Control? GetElement(int index)
            {
                var i = index - FirstIndex;
                if (i >= 0 && i < _elements?.Count)
                    return _elements[i];
                return null;
            }

            /// <summary>
            /// Gets the index and start U position of the element at the specified U position.
            /// </summary>
            /// <param name="u">The U position.</param>
            /// <returns>
            /// A tuple containing:
            /// - The index of the item at the specified U position, or -1 if the item could not be
            ///   determined
            /// - The U position of the start of the item, if determined
            /// </returns>
            public (int index, double position) GetIndexAt(double u)
            {
                if (_elements is null || _sizes is null || _startU > u || _startUUnstable)
                    return (-1, 0);

                var index = 0;
                var position = _startU;

                while (index < _elements.Count)
                {
                    var size = _sizes[index];
                    if (double.IsNaN(size))
                        break;
                    if (u >= position && u < position + size)
                        return (index + FirstIndex, position);
                    position += size;
                    ++index;
                }

                return (-1, 0);
            }

            /// <summary>
            /// Gets the element at the specified position on the primary axis, if realized.
            /// </summary>
            /// <param name="position">The position.</param>
            /// <returns>
            /// A tuple containing the index of the element (or -1 if not found) and the position of the element on the
            /// primary axis.
            /// </returns>
            public (int index, double position) GetElementAt(double position)
            {
                if (_sizes is null || position < StartU)
                    return (-1, 0);

                var u = StartU;
                var i = FirstIndex;

                foreach (var size in _sizes)
                {
                    var endU = u + size;
                    if (position < endU)
                        return (i, u);
                    u += size;
                    ++i;
                }

                return (-1, 0);
            }

            /// <summary>
            /// Estimates the average U size of all elements in the source collection based on the
            /// realized elements.
            /// </summary>
            /// <returns>
            /// The estimated U size of an element, or -1 if not enough information is present to make
            /// an estimate.
            /// </returns>
            public double EstimateElementSizeU()
            {
                var total = 0.0;
                var divisor = 0.0;

                // Start by averaging the size of the elements before the first realized element.
                if (FirstIndex >= 0 && !_startUUnstable)
                {
                    total += _startU;
                    divisor += FirstIndex;
                }

                // Average the size of the realized elements.
                if (_sizes is not null)
                {
                    foreach (var size in _sizes)
                    {
                        if (double.IsNaN(size))
                            continue;
                        total += size;
                        ++divisor;
                    }
                }

                // We don't have any elements on which to base our estimate.
                if (divisor == 0 || total == 0)
                    return -1;

                return total / divisor;
            }

            /// <summary>
            /// Gets the index of the specified element.
            /// </summary>
            /// <param name="element">The element.</param>
            /// <returns>The index or -1 if the element is not present in the collection.</returns>
            public int GetIndex(Control element)
            {
                return _elements?.IndexOf(element) is int index && index >= 0 ? index + FirstIndex : -1;
            }

            /// <summary>
            /// Updates the elements in response to items being inserted into the source collection.
            /// </summary>
            /// <param name="index">The index in the source collection of the insert.</param>
            /// <param name="count">The number of items inserted.</param>
            /// <param name="updateElementIndex">A method used to update the element indexes.</param>
            public void ItemsInserted(int index, int count, Action<Control, int, int> updateElementIndex)
            {
                if (index < 0)
                    throw new ArgumentOutOfRangeException(nameof(index));
                if (_elements is null || _elements.Count == 0)
                    return;

                // Get the index within the realized _elements collection.
                var first = FirstIndex;
                var realizedIndex = index - first;

                if (realizedIndex < Count)
                {
                    // The insertion point affects the realized elements. Update the index of the
                    // elements after the insertion point.
                    var elementCount = _elements.Count;
                    var start = Math.Max(realizedIndex, 0);
                    var newIndex = first + count;

                    for (var i = start; i < elementCount; ++i)
                    {
                        if (_elements[i] is Control element)
                            updateElementIndex(element, newIndex - count, newIndex);
                        ++newIndex;
                    }

                    if (realizedIndex <= 0)
                    {
                        // The insertion point was before the first element, update the first index.
                        _firstIndex += count;
                    }
                    else
                    {
                        // The insertion point was within the realized elements, insert an empty space
                        // in _elements and _sizes.
                        _elements!.InsertMany(realizedIndex, null, count);
                        _sizes!.InsertMany(realizedIndex, double.NaN, count);
                    }
                }
            }

            /// <summary>
            /// Updates the elements in response to items being removed from the source collection.
            /// </summary>
            /// <param name="index">The index in the source collection of the remove.</param>
            /// <param name="count">The number of items removed.</param>
            /// <param name="updateElementIndex">A method used to update the element indexes.</param>
            /// <param name="recycleElement">A method used to recycle elements.</param>
            public void ItemsRemoved(
                int index,
                int count,
                Action<Control, int, int> updateElementIndex,
                Action<Control> recycleElement)
            {
                if (index < 0)
                    throw new ArgumentOutOfRangeException(nameof(index));
                if (_elements is null || _elements.Count == 0)
                    return;

                // Get the removal start and end index within the realized _elements collection.
                var first = FirstIndex;
                var last = LastIndex;
                var startIndex = index - first;
                var endIndex = (index + count) - first;

                if (endIndex < 0)
                {
                    // The removed range was before the realized elements. Update the first index and
                    // the indexes of the realized elements.
                    _firstIndex -= count;
                    _startUUnstable = true;

                    var newIndex = _firstIndex;
                    for (var i = 0; i < _elements.Count; ++i)
                    {
                        if (_elements[i] is Control element)
                            updateElementIndex(element, newIndex - count, newIndex);
                        ++newIndex;
                    }
                }
                else if (startIndex < _elements.Count)
                {
                    // Recycle and remove the affected elements.
                    var start = Math.Max(startIndex, 0);
                    var end = Math.Min(endIndex, _elements.Count);

                    for (var i = start; i < end; ++i)
                    {
                        if (_elements[i] is Control element)
                            recycleElement(element);
                    }

                    _elements.RemoveRange(start, end - start);
                    _sizes!.RemoveRange(start, end - start);

                    // If the remove started before and ended within our realized elements, then our new
                    // first index will be the index where the remove started. Mark StartU as unstable
                    // because we can't rely on it now to estimate element heights.
                    if (startIndex <= 0 && end < last)
                    {
                        _firstIndex = first = index;
                        _startUUnstable = true;
                    }

                    // Update the indexes of the elements after the removed range.
                    end = _elements.Count;
                    var newIndex = first + start;
                    for (var i = start; i < end; ++i)
                    {
                        if (_elements[i] is Control element)
                            updateElementIndex(element, newIndex + count, newIndex);
                        ++newIndex;
                    }
                }
            }

            /// <summary>
            /// Recycles all elements in response to the source collection being reset.
            /// </summary>
            /// <param name="recycleElement">A method used to recycle elements.</param>
            public void ItemsReset(Action<Control> recycleElement)
            {
                if (_elements is null || _elements.Count == 0)
                    return;

                foreach (var e in _elements)
                {
                    if (e is not null)
                        recycleElement(e);
                }

                _startU = _firstIndex = 0;
                _elements?.Clear();
                _sizes?.Clear();

            }

            /// <summary>
            /// Recycles elements before a specific index.
            /// </summary>
            /// <param name="index">The index in the source collection of new first element.</param>
            /// <param name="recycleElement">A method used to recycle elements.</param>
            public void RecycleElementsBefore(int index, Action<Control, int> recycleElement)
            {
                if (index <= FirstIndex || _elements is null || _elements.Count == 0)
                    return;

                if (index > LastIndex)
                {
                    RecycleAllElements(recycleElement);
                }
                else
                {
                    var endIndex = index - FirstIndex;

                    for (var i = 0; i < endIndex; ++i)
                    {
                        if (_elements[i] is Control e)
                            recycleElement(e, i + FirstIndex);
                    }

                    _elements.RemoveRange(0, endIndex);
                    _sizes!.RemoveRange(0, endIndex);
                    _firstIndex = index;
                }
            }

            /// <summary>
            /// Recycles elements after a specific index.
            /// </summary>
            /// <param name="index">The index in the source collection of new last element.</param>
            /// <param name="recycleElement">A method used to recycle elements.</param>
            public void RecycleElementsAfter(int index, Action<Control, int> recycleElement)
            {
                if (index >= LastIndex || _elements is null || _elements.Count == 0)
                    return;

                if (index < FirstIndex)
                {
                    RecycleAllElements(recycleElement);
                }
                else
                {
                    var startIndex = (index + 1) - FirstIndex;
                    var count = _elements.Count;

                    for (var i = startIndex; i < count; ++i)
                    {
                        if (_elements[i] is Control e)
                            recycleElement(e, i + FirstIndex);
                    }

                    _elements.RemoveRange(startIndex, _elements.Count - startIndex);
                    _sizes!.RemoveRange(startIndex, _sizes.Count - startIndex);
                }
            }

            /// <summary>
            /// Recycles all realized elements.
            /// </summary>
            /// <param name="recycleElement">A method used to recycle elements.</param>
            public void RecycleAllElements(Action<Control, int> recycleElement)
            {
                if (_elements is null || _elements.Count == 0)
                    return;

                var i = FirstIndex;

                foreach (var e in _elements)
                {
                    if (e is not null)
                        recycleElement(e, i);
                    ++i;
                }

                _startU = _firstIndex = 0;
                _elements?.Clear();
                _sizes?.Clear();
            }

            /// <summary>
            /// Resets the element list and prepares it for reuse.
            /// </summary>
            public void ResetForReuse()
            {
                _startU = _firstIndex = 0;
                _startUUnstable = false;
                _elements?.Clear();
                _sizes?.Clear();
            }
        }

        private struct MeasureViewport
        {
            public int firstIndex;
            public int lastIndex;
            public double viewportUStart;
            public double viewportUEnd;
            public double measuredV;
            public double startU;
        }
    }
}
