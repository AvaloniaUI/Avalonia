using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using Avalonia.Controls.Utils;
using Avalonia.Layout;
using Avalonia.Utilities;
using Avalonia.VisualTree;

namespace Avalonia.Controls
{
    public partial class VirtualizingStackPanel : VirtualizingPanel
    {
        private static readonly Rect s_invalidViewport = new(double.PositiveInfinity, double.PositiveInfinity, 0, 0);
        private readonly Action<Control> _recycleElement;
        private readonly Action<Control, int> _updateElementIndex;
        private int _anchorIndex = -1;
        private Control? _anchorElement;
        private bool _isWaitingForViewportUpdate;
        private double _lastEstimatedElementSizeU = 25;
        private RealizedElementList? _measureElements;
        private RealizedElementList? _realizedElements;
        private Rect _viewport = s_invalidViewport;

        private Size MeasureOverrideSmooth(Size availableSize)
        {
            if (!IsEffectivelyVisible)
                return default;

            var items = ItemsControl?.Items as IList;

            if (items is null || items.Count == 0)
            {
                Children.Clear();
                return default;
            }

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
            _realizedElements.RecycleElementsAfter(viewport.estimatedLastIndex, _recycleElement);

            // Do the measure, creating/recycling elements as necessary to fill the viewport. Don't
            // write to _realizedElements yet, only _measureElements.
            GenerateElements(availableSize, ref viewport);

            // Now we know what definitely fits, recycle anything left over.
            _realizedElements.RecycleElementsAfter(_measureElements.LastModelIndex, _recycleElement);

            // And swap the measureElements and realizedElements collection.
            (_measureElements, _realizedElements) = (_realizedElements, _measureElements);
            _measureElements.ResetForReuse();

            return CalculateDesiredSize(orientation, items, viewport.measuredV);
        }

        private Size ArrangeOverrideSmooth(Size finalSize)
        {
            Debug.Assert(_realizedElements is not null);

            var orientation = Orientation;
            var u = _realizedElements!.StartU;

            for (var i = 0; i < _realizedElements.Count; ++i)
            {
                var e = _realizedElements.Elements[i];

                if (e is object)
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

        private MeasureViewport CalculateMeasureViewport(IList items)
        {
            Debug.Assert(_realizedElements is not null);

            // If the control has not yet been laid out then the effective viewport won't have been set.
            // Try to work it out from an ancestor control.
            var viewport = _viewport != s_invalidViewport ? _viewport : EstimateViewport();

            // Get the viewport in the orientation direction.
            var viewportStart = Orientation == Orientation.Horizontal ? viewport.X : viewport.Y;
            var viewportEnd = Orientation == Orientation.Horizontal ? viewport.Right : viewport.Bottom;

            var (firstIndex, firstIndexU) = _realizedElements!.GetElementAt(viewportStart);
            var (lastIndex, _) = _realizedElements.GetElementAt(viewportEnd);
            var estimatedElementSize = -1.0;
            var itemCount = items?.Count ?? 0;

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

        private Size CalculateDesiredSize(Orientation orientation, IList items, double sizeV)
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
            var count = _realizedElements!.Count;
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

            if (c is null)
            {
                return viewport;
            }

            while (c is not null)
            {
                if (!c.Bounds.IsEmpty && c.TransformToVisual(this) is Matrix transform)
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
            Debug.Assert(ItemsControl?.Items is IList);
            Debug.Assert(_measureElements is not null);

            var items = (IList)ItemsControl!.Items;
            var horizontal = Orientation == Orientation.Horizontal;
            var index = viewport.firstIndex;
            var u = viewport.startU;

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

        private Control GetOrCreateElement(IList items, int index)
        {
            var e = GetRealizedElement(index) ?? GetRecycledOrCreateElement(items, index);
            InvalidateHack(e);
            return e;
        }

        private Control? GetRealizedElement(int index)
        {
            Debug.Assert(_realizedElements is not null);

            if (_anchorIndex == index)
                return _anchorElement;
            return _realizedElements!.GetElement(index);
        }

        private Control GetRecycledOrCreateElement(IList items, int index)
        {
            Debug.Assert(ItemsControl is not null);

            var c = ItemsControl!.ItemContainerGenerator.Materialize(index, items[index]!).ContainerControl;
            Children.Add(c);
            return c;
        }

        private void RecycleElement(Control element)
        {
            Debug.Assert(ItemsControl is not null);

            var index = ItemsControl.ItemContainerGenerator!.IndexFromContainer(element);
            ItemsControl!.ItemContainerGenerator.Dematerialize(index, 1);
            Children.Remove(element);
        }

        private void UpdateElementIndex(Control element, int index)
        {
            // TODO: Implement this after we refactor ItemContainerGenerator.
        }

        private void OnEffectiveViewportChanged(object? sender, EffectiveViewportChangedEventArgs e)
        {
            _viewport = e.EffectiveViewport;
            _isWaitingForViewportUpdate = false;
            InvalidateMeasure();
        }

        private void OnItemsChangedSmooth(NotifyCollectionChangedEventArgs e)
        {
            if (_realizedElements is null)
                return;

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    _realizedElements.ItemsInserted(e.NewStartingIndex, e.NewItems!.Count, _updateElementIndex);
                    ItemsControl!.ItemContainerGenerator.InsertSpace(e.NewStartingIndex, e.NewItems!.Count);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    _realizedElements.ItemsRemoved(e.OldStartingIndex, e.OldItems!.Count, _updateElementIndex, _recycleElement);
                    ItemsControl!.ItemContainerGenerator.RemoveRange(e.OldStartingIndex, e.OldItems!.Count);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    ////RecycleAllElements();
                    break;
            }

            InvalidateMeasure();
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

        /// <summary>
        /// Stores the realized element state for a <see cref="VirtualizingStackPanel"/> in smooth mode.
        /// </summary>
        internal class RealizedElementList
        {
            private int _firstIndex;
            private List<Control?>? _elements;
            private List<double>? _sizes;
            private double _startU;

            /// <summary>
            /// Gets the number of realized elements.
            /// </summary>
            public int Count => _elements?.Count ?? 0;

            /// <summary>
            /// Gets the model index of the first realized element, or -1 if no elements are realized.
            /// </summary>
            public int FirstModelIndex => _elements?.Count > 0 ? _firstIndex : -1;

            /// <summary>
            /// Gets the model index of the last realized element, or -1 if no elements are realized.
            /// </summary>
            public int LastModelIndex => _elements?.Count > 0 ? _firstIndex + _elements.Count - 1 : -1;

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
            /// <param name="modelIndex">The model index of the element.</param>
            /// <param name="element">The element.</param>
            /// <param name="u">The position of the elemnt on the primary axis.</param>
            /// <param name="sizeU">The size of the element on the primary axis.</param>
            public void Add(int modelIndex, Control element, double u, double sizeU)
            {
                if (modelIndex < 0)
                    throw new ArgumentOutOfRangeException(nameof(modelIndex));

                _elements ??= new List<Control?>();
                _sizes ??= new List<double>();

                if (Count == 0)
                {
                    _elements.Add(element);
                    _sizes.Add(sizeU);
                    _startU = u;
                    _firstIndex = modelIndex;
                }
                else if (modelIndex == LastModelIndex + 1)
                {
                    _elements.Add(element);
                    _sizes.Add(sizeU);
                }
                else if (modelIndex == FirstModelIndex - 1)
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
            /// Gets the element at the specified model index, if realized.
            /// </summary>
            /// <param name="modelIndex">The index in the source collection of the element to get.</param>
            /// <returns>The element if realized; otherwise null.</returns>
            public Control? GetElement(int modelIndex)
            {
                var index = modelIndex - FirstModelIndex;
                if (index >= 0 && index < _elements?.Count)
                    return _elements[index];
                return null;
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
                var i = FirstModelIndex;

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
            /// Updates the elements in response to items being inserted into the source collection.
            /// </summary>
            /// <param name="modelIndex">The index in the source collection of the insert.</param>
            /// <param name="count">The number of items inserted.</param>
            /// <param name="updateElementIndex">A method used to update the element indexes.</param>
            public void ItemsInserted(int modelIndex, int count, Action<Control, int> updateElementIndex)
            {
                if (modelIndex < 0)
                    throw new ArgumentOutOfRangeException(nameof(modelIndex));
                if (_elements is null || _elements.Count == 0)
                    return;

                // Get the index within the realized _elements collection.
                var first = FirstModelIndex;
                var index = modelIndex - first;

                if (index < Count)
                {
                    // The insertion point affects the realized elements. Update the index of the
                    // elements after the insertion point.
                    var elementCount = _elements.Count;
                    var start = Math.Max(index, 0);

                    for (var i = start; i < elementCount; ++i)
                    {
                        if (_elements[i] is Control element)
                            updateElementIndex(element, first + i + count);
                    }

                    if (index <= 0)
                    {
                        // The insertion point was before the first element, update the first index.
                        _firstIndex += count;
                    }
                    else
                    {
                        // The insertion point was within the realized elements, insert an empty space
                        // in _elements and _sizes.
                        _elements!.InsertMany(index, null, count);
                        _sizes!.InsertMany(index, 0.0, count);
                    }
                }
            }

            /// <summary>
            /// Updates the elements in response to items being removed from the source collection.
            /// </summary>
            /// <param name="modelIndex">The index in the source collection of the remove.</param>
            /// <param name="count">The number of items removed.</param>
            /// <param name="updateElementIndex">A method used to update the element indexes.</param>
            /// <param name="recycleElement">A method used to recycle elements.</param>
            public void ItemsRemoved(
                int modelIndex,
                int count,
                Action<Control, int> updateElementIndex,
                Action<Control> recycleElement)
            {
                if (modelIndex < 0)
                    throw new ArgumentOutOfRangeException(nameof(modelIndex));
                if (_elements is null || _elements.Count == 0)
                    return;

                // Get the removal start and end index within the realized _elements collection.
                var first = FirstModelIndex;
                var last = LastModelIndex;
                var startIndex = modelIndex - first;
                var endIndex = (modelIndex + count) - first;

                if (endIndex < 0)
                {
                    // The removed range was before the realized elements. Update the first index and
                    // the indexes of the realized elements.
                    _firstIndex -= count;

                    for (var i = 0; i < _elements.Count; ++i)
                    {
                        if (_elements[i] is Control element)
                            updateElementIndex(element, _firstIndex + i);
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
                    // first index will be the index where the remove started.
                    if (startIndex <= 0 && end < last)
                        _firstIndex = first = modelIndex;

                    // Update the indexes of the elements after the removed range.
                    end = _elements.Count;
                    for (var i = start; i < end; ++i)
                    {
                        if (_elements[i] is Control element)
                            updateElementIndex(element, first + i);
                    }
                }
            }

            /// <summary>
            /// Recycles elements before a specific index.
            /// </summary>
            /// <param name="modelIndex">The index in the source collection of new first element.</param>
            /// <param name="recycleElement">A method used to recycle elements.</param>
            public void RecycleElementsBefore(int modelIndex, Action<Control> recycleElement)
            {
                if (modelIndex <= FirstModelIndex || _elements is null || _elements.Count == 0)
                    return;

                if (modelIndex > LastModelIndex)
                {
                    RecycleAllElements(recycleElement);
                }
                else
                {
                    var endIndex = modelIndex - FirstModelIndex;

                    for (var i = 0; i < endIndex; ++i)
                    {
                        if (_elements[i] is Control e)
                            recycleElement(e);
                    }

                    _elements.RemoveRange(0, endIndex);
                    _sizes!.RemoveRange(0, endIndex);
                    _firstIndex = modelIndex;
                }
            }

            /// <summary>
            /// Recycles elements after a specific index.
            /// </summary>
            /// <param name="modelIndex">The index in the source collection of new last element.</param>
            /// <param name="recycleElement">A method used to recycle elements.</param>
            public void RecycleElementsAfter(int modelIndex, Action<Control> recycleElement)
            {
                if (modelIndex >= LastModelIndex || _elements is null || _elements.Count == 0)
                    return;

                if (modelIndex < FirstModelIndex)
                {
                    RecycleAllElements(recycleElement);
                }
                else
                {
                    var startIndex = (modelIndex + 1) - FirstModelIndex;
                    var count = _elements.Count;

                    for (var i = startIndex; i < count; ++i)
                    {
                        if (_elements[i] is Control e)
                            recycleElement(e);
                    }

                    _elements.RemoveRange(startIndex, _elements.Count - startIndex);
                    _sizes!.RemoveRange(startIndex, _sizes.Count - startIndex);
                }
            }

            /// <summary>
            /// Recycles all realized elements.
            /// </summary>
            /// <param name="recycleElement">A method used to recycle elements.</param>
            public void RecycleAllElements(Action<Control> recycleElement)
            {
                if (_elements is null || _elements.Count == 0)
                    return;

                foreach (var e in _elements)
                {
                    if (e is object)
                        recycleElement(e);
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
                _elements?.Clear();
                _sizes?.Clear();
            }
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
