using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia.Controls.Utils;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Utilities;
using Avalonia.VisualTree;

#nullable enable

namespace Avalonia.Controls.Primitives
{
    public abstract class VirtualizingStackBase : Control
    {
        private static readonly Rect s_invalidViewport = new(double.PositiveInfinity, double.PositiveInfinity, 0, 0);
        private readonly Action<IControl, int> _unrealizeElement;
        private readonly Action<IControl, int> _updateElementIndex;
        private int _anchorIndex = -1;
        private IControl? _anchorElement;
        private bool _isWaitingForViewportUpdate;
        private ItemsSourceView _items = ItemsSourceView.Empty;
        private RealizedElementList _measureElements = new();
        private RealizedElementList _realizedElements = new();
        private double _lastEstimatedElementSizeU = 25;

        public VirtualizingStackBase()
        {
            Children.CollectionChanged += OnChildrenChanged;
            _unrealizeElement = UnrealizeElement;
            _updateElementIndex = UpdateElementIndex;
            EffectiveViewportChanged += OnEffectiveViewportChanged;
        }

        protected Controls Children { get; } = new();

        protected virtual ItemsSourceView Items
        {
            get => _items;
            set
            {
                _ = _items ?? throw new ArgumentNullException(nameof(value));

                if (_items != value)
                {
                    _items.CollectionChanged -= OnItemsCollectionChanged;
                    _items = value;
                    _items.CollectionChanged += OnItemsCollectionChanged;
                    OnItemsCollectionChanged(null, CollectionExtensions.ResetEvent);
                }
            }
        }

        protected IReadOnlyList<IControl?> RealizedElements => _realizedElements.Elements;
        protected Rect Viewport { get; private set; }

        /// <summary>
        /// When overridden in a derived class, returns the stack orientation.
        /// </summary>
        /// <returns></returns>
        protected abstract Orientation GetOrientation();

        /// <summary>
        /// When overridden in a derived class, creates or recycles a control for the specified
        /// item index.
        /// </summary>
        /// <param name="index">The item index.</param>
        /// <returns>The realized control.</returns>
        /// <remarks>
        /// The implementation of this method should return a new or recycled control set up
        /// to display the item specified by the index, and added to the <see cref="Controls"/>
        /// collection.
        /// </remarks>
        protected abstract IControl RealizeElement(int index);

        /// <summary>
        /// When overridden in a derived class, unrealizes a control by removing it from the
        /// <see cref="Controls"/> collection or marking it available for recycling and making it
        /// invisible in some manner.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="index"></param>
        protected abstract void UnrealizeElement(IControl element, int index);
        protected abstract void UpdateElementIndex(IControl element, int index);

        protected void BringIntoView(int index)
        {
            if (index < 0 || index >= _items.Count)
                return;

            if (GetRealizedElement(index) is IControl element)
            {
                element.BringIntoView();
            }
            else if (this.GetVisualRoot() is ILayoutRoot root)
            {
                // Create and measure the element to be brought into view. Store it in a field so that
                // it can be re-used in the layout pass.
                _anchorElement = GetOrCreateElement(index);
                _anchorElement.Measure(Size.Infinity);
                _anchorIndex = index;

                // Get the expected position of the elment and put it in place.
                var anchorU = GetOrEstimateElementPosition(index);
                var rect = GetOrientation() == Orientation.Horizontal ?
                    new Rect(anchorU, 0, _anchorElement.DesiredSize.Width, _anchorElement.DesiredSize.Height) :
                    new Rect(0, anchorU, _anchorElement.DesiredSize.Width, _anchorElement.DesiredSize.Height);
                _anchorElement.Arrange(rect);

                // Try to bring the item into view and do a layout pass.
                _anchorElement.BringIntoView();

                _isWaitingForViewportUpdate = !Viewport.Contains(rect);
                root.LayoutManager.ExecuteLayoutPass();
                _isWaitingForViewportUpdate = false;

                _anchorElement = null;
                _anchorIndex = -1;
            }
        }

        protected virtual Rect ArrangeElement(int index, IControl element, Rect rect)
        {
            element.Arrange(rect);
            return rect;
        }

        protected virtual Size MeasureElement(int index, IControl element, Size availableSize)
        {
            element.Measure(availableSize);
            return element.DesiredSize;
        }

        protected virtual (int index, double position) GetElementAt(double position) => (-1, -1);
        protected virtual double GetElementPosition(int index) => -1;

        protected virtual double CalculateExtentU(Size availableSize)
        {
            // Return the estimated size of all items based on the elements currently realized.
            return EstimateElementSizeU() * Items.Count;
        }

        protected int GetIndexForRealizedElement(IControl element)
        {
            return _realizedElements.GetModelIndexForElement(element);
        }

        protected IControl? GetRealizedElement(int index)
        {
            if (_anchorIndex == index)
                return _anchorElement;
            return _realizedElements.GetElementByModelIndex(index);
        }

        protected void UnrealizeAllElements() => _realizedElements.UnrealizeAllElements(_unrealizeElement);

        protected override Size MeasureOverride(Size availableSize)
        {
            if (!IsEffectivelyVisible)
                return default;

            if (Items.Count == 0)
            {
                Children.Clear();
                return default;
            }

            // If we're bringing an item into view, ignore any layout passes until we receive a new
            // effective viewport.
            if (_isWaitingForViewportUpdate)
                return DesiredSize;

            // We handle horizontal and vertical layouts here so X and Y are abstracted to:
            // - Horizontal layouts: U = horizontal, V = vertical
            // - Vertical layouts: U = vertical, V = horizontal
            var viewport = CalculateMeasureViewport();

            // Unrealize elements outside of the expected range.
            UnrealizeElementsBefore(viewport.firstIndex);
            UnrealizeElementsAfter(viewport.estimatedLastIndex);

            // Do the measure, realizing/unrealizing elements as necessary to fill the viewport. Don't
            // write to _realizedElements yet, only _measureElements.
            GenerateElements(availableSize, ref viewport);

            // Now we know what definitely fits, unrealizing anything left over.
            UnrealizeElementsAfter(_measureElements.LastModelIndex);

            // And swap the measureElements and realizedElements collection.
            (_measureElements, _realizedElements) = (_realizedElements, _measureElements);
            _measureElements.ResetForReuse();

            var sizeU = CalculateExtentU(availableSize);

            if (double.IsInfinity(sizeU) || double.IsNaN(sizeU))
                throw new InvalidOperationException("Invalid calculated size.");

            return GetOrientation() == Orientation.Horizontal ?
                new Size(sizeU, viewport.measuredV) :
                new Size(viewport.measuredV, sizeU);
        }

        private void GenerateElements(Size availableSize, ref MeasureViewport viewport)
        {
            _ = Items ?? throw new AvaloniaInternalException("Items may not be null.");

            var horizontal = GetOrientation() == Orientation.Horizontal;
            var index = viewport.firstIndex;
            var u = viewport.startU;

            do
            {
                var e = GetOrCreateElement(index);
                var slot = MeasureElement(index, e, availableSize);
                var sizeU = horizontal ? slot.Width : slot.Height;

                _measureElements.Add(index, e, u, sizeU);
                viewport.measuredV = Math.Max(viewport.measuredV, horizontal ? slot.Height : slot.Width);

                u += sizeU;
                ++index;
            } while (u < viewport.viewportUEnd && index < Items.Count);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var orientation = GetOrientation();
            var u = _realizedElements.StartU;

            for (var i = 0; i < _realizedElements.Count; ++i)
            {
                var e = _realizedElements.Elements[i];

                if (e is object)
                {
                    var sizeU = _realizedElements.SizeU[i];
                    var rect = orientation == Orientation.Horizontal ?
                        new Rect(u, 0, sizeU, finalSize.Height) :
                        new Rect(0, u, finalSize.Width, sizeU);
                    rect = ArrangeElement(i + _realizedElements.FirstModelIndex, e, rect);
                    u += orientation == Orientation.Horizontal ? rect.Width : rect.Height;
                }
            }

            return finalSize;
        }

        protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromLogicalTree(e);
            UnrealizeAllElements();
        }

        protected virtual void OnEffectiveViewportChanged(object? sender, EffectiveViewportChangedEventArgs e)
        {
            Viewport = e.EffectiveViewport;
            _isWaitingForViewportUpdate = false;
            InvalidateMeasure();
        }

        private MeasureViewport CalculateMeasureViewport()
        {
            // If the control has not yet been laid out then the effective viewport won't have been set.
            // Try to work it out from an ancestor control.
            var viewport = Viewport != s_invalidViewport ? Viewport : EstimateViewport();

            // Get the viewport in the orientation direction.
            var orientation = GetOrientation();
            var viewportStart = orientation == Orientation.Horizontal ? viewport.X : viewport.Y;
            var viewportEnd = orientation == Orientation.Horizontal ? viewport.Right : viewport.Bottom;

            var (firstIndex, firstIndexU) = GetElementAt(viewportStart);
            var (lastIndex, _) = GetElementAt(viewportEnd);
            var estimatedElementSize = -1.0;
            var itemCount = Items?.Count ?? 0;

            if (firstIndex == -1)
            {
                estimatedElementSize = EstimateElementSizeU();
                firstIndex = (int)(viewportStart / estimatedElementSize);
                firstIndexU = firstIndex * estimatedElementSize;
            }

            if (lastIndex == -1)
            {
                if (estimatedElementSize == -1)
                    estimatedElementSize = EstimateElementSizeU();
                lastIndex = (int)(viewportEnd / estimatedElementSize);
            }

            return new MeasureViewport
            {
                firstIndex = MathUtilities.Clamp(firstIndex, 0, itemCount - 1),
                estimatedLastIndex = MathUtilities.Clamp(lastIndex, 0, itemCount - 1),
                viewportUStart = viewportStart,
                viewportUEnd = viewportEnd,
                startU = firstIndexU,
            };
        }

        private IControl GetOrCreateElement(int index)
        {
            var e = GetRealizedElement(index) ?? RealizeElement(index);
            InvalidateChildrenIfNecessary(e);
            return e;
        }

        private double GetOrEstimateElementPosition(int index)
        {
            var u = GetElementPosition(index);

            if (u >= 0)
                return u;

            var estimatedElementSize = EstimateElementSizeU();
            return index * estimatedElementSize;
        }

        private double EstimateElementSizeU()
        {
            var count = _realizedElements.Count;
            var divisor = 0.0;
            var total = 0.0;

            for (var i = 0; i < count; ++i)
            {
                if (_realizedElements.Elements[i] is object)
                {
                    total += _realizedElements.SizeU[i];
                    ++divisor;
                }
            }

            if (divisor == 0 || total == 0)
                return _lastEstimatedElementSizeU;

            _lastEstimatedElementSizeU = total / divisor;
            return _lastEstimatedElementSizeU;
        }

        private Rect EstimateViewport()
        {
            var c = this.GetVisualParent();
            var viewport = new Rect();

            while (c is object)
            {
                if (!c.Bounds.IsEmpty && c?.TransformToVisual(this) is Matrix transform)
                {
                    viewport = new Rect(0, 0, c.Bounds.Width, c.Bounds.Height)
                        .TransformToAABB(transform);
                    break;
                }

                c = c.GetVisualParent();
            }


            return viewport;
        }

        private void UnrealizeElementsAfter(int index) => _realizedElements.UnrealizeElementsAfter(index, _unrealizeElement);
        private void UnrealizeElementsBefore(int index) => _realizedElements.UnrealizeElementsBefore(index, _unrealizeElement);

        private void OnChildrenChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            void Add(IList items)
            {
                foreach (var i in items)
                {
                    if (i is IControl c)
                    {
                        LogicalChildren.Add(c);
                        VisualChildren.Add(c);
                    }
                }
            }

            void Remove(IList items)
            {
                foreach (var i in items)
                {
                    if (i is IControl c)
                    {
                        LogicalChildren.Remove(c);
                        VisualChildren.Remove(c);
                    }
                }
            }

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    Add(e.NewItems!);
                    break;

                case NotifyCollectionChangedAction.Remove:
                    Remove(e.OldItems!);
                    break;

                case NotifyCollectionChangedAction.Replace:
                    Remove(e.OldItems!);
                    Add(e.NewItems!);
                    break;

                case NotifyCollectionChangedAction.Reset:
                    throw new NotSupportedException();
            }
        }

        private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    _realizedElements.ItemsInserted(e.NewStartingIndex, e.NewItems!.Count, _updateElementIndex);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    _realizedElements.ItemsRemoved(e.OldStartingIndex, e.OldItems!.Count, _updateElementIndex, _unrealizeElement);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    UnrealizeAllElements();
                    break;
            }

            InvalidateMeasure();
        }

        private static void InvalidateChildrenIfNecessary(IControl c)
        {
            bool HasInvalidations(IControl c)
            {
                if (!c.IsMeasureValid)
                    return true;

                for (var i = 0; i < c.VisualChildren.Count; ++i)
                {
                    if (c.VisualChildren[i] is IControl child)
                    {
                        if (!child.IsMeasureValid || HasInvalidations(child))
                            return true;
                    }
                }

                return false;
            }

            void Invalidate(IControl c)
            {
                c.InvalidateMeasure();
                for (var i = 0; i < c.VisualChildren.Count; ++i)
                {
                    if (c.VisualChildren[i] is IControl child)
                        Invalidate(child);
                }
            }

            if (HasInvalidations(c))
                Invalidate(c);
        }

        private struct MeasureViewport
        {
            public int firstIndex;
            public int estimatedLastIndex;
            public double viewportUStart;
            public double viewportUEnd;
            public double measuredV;
            public double startU;
        }
    }
}
