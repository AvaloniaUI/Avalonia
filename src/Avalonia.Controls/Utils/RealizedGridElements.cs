using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Utilities;

namespace Avalonia.Controls.Utils
{
    /// <summary>
    /// Stores the realized element state for a virtualizing panel that arranges its children
    /// in a stack layout, such as <see cref="VirtualizingUniformGrid"/>.
    /// </summary>
    internal class RealizedGridElements
    {
        private int _firstIndex;
        private List<Control?>? _elements;
        private List<Size>? _sizes;

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
        public IReadOnlyList<Size> Sizes => _sizes ??= new List<Size>();

        public int RowCount { get; set; }
        public int ColumnCount { get; set; }
        public int FirstColumn { get; set; }

        /// <summary>
        /// Adds a newly realized element to the collection.
        /// </summary>
        /// <param name="index">The index of the element.</param>
        /// <param name="element">The element.</param>
        /// <param name="size">The coordinates of the element.</param>
        public void Add(int index, Control element, Size size)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));

            _elements ??= new List<Control?>();
            _sizes ??= new List<Size>();

            if (Count == 0)
            {
                _elements.Add(element);
                _sizes.Add(size);
                _firstIndex = index;
            }
            else if (index == LastIndex + 1)
            {
                _elements.Add(element);
                _sizes.Add(size);
            }
            else if (index == FirstIndex - 1)
            {
                --_firstIndex;
                _elements.Insert(0, element);
                _sizes.Insert(0, size);
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
        /// Gets or estimates the index and start U position of the anchor element for the
        /// specified viewport.
        /// </summary>
        /// <param name="viewportStart">The position of the start of the viewport.</param>
        /// <param name="viewportEnd">The position of the end of the viewport.</param>
        /// <param name="itemCount">The number of items in the list.</param>
        /// <param name="estimatedElementSize">The current estimated element size.</param>
        /// <returns>
        /// A tuple containing:
        /// - The index of the anchor element, or -1 if an anchor could not be determined
        /// - The U position of the start of the anchor element, if determined
        /// </returns>
        /// <remarks>
        /// This method tries to find an existing element in the specified viewport from which
        /// element realization can start. Failing that it estimates the first element in the
        /// viewport.
        /// </remarks>
        public (int index, Vector coord, Vector lastCoord) GetOrEstimateAnchorElementForViewport(
            Point viewportStart,
            Point viewportEnd,
            int itemCount,
            ref Size estimatedElementSize)
        {
            // We have no elements, nothing to do here.
            if (itemCount <= 0)
                return (-1, Vector.Zero, Vector.Zero);

            if (_sizes is not null)
            {
                var maxWidth = _sizes.Max(x => x.Width);
                var maxHeight = _sizes.Max(x => x.Height);

                estimatedElementSize = new Size(maxWidth, maxHeight);
            }

            var MaxWidth = ColumnCount * estimatedElementSize.Width;
            var MaxHeight = RowCount * estimatedElementSize.Height;

            var x = Math.Min((int)(Math.Min(viewportStart.X, MaxWidth) / estimatedElementSize.Width), ColumnCount);
            var y = Math.Min((int)(Math.Min(viewportStart.Y, MaxHeight) / estimatedElementSize.Height), RowCount);

            var lastY = Math.Min((int)(Math.Min(viewportEnd.Y, MaxHeight) / estimatedElementSize.Height), RowCount);
            var lastX = lastY > 0 ? ColumnCount : Math.Min((int)(Math.Min(viewportEnd.X, MaxWidth) / estimatedElementSize.Width), ColumnCount);

            return (Math.Max(y * ColumnCount + x - FirstColumn, 0), new Vector(x, y), new Vector(lastX, lastY));
        }

        /// <summary>
        /// Gets the position of the element with the requested index on the primary axis, if realized.
        /// </summary>
        /// <returns>
        /// The position of the element, or NaN if the element is not realized.
        /// </returns>
        public Vector GetElementCoord(int index)
        {
            var div = Math.DivRem(index + FirstColumn, ColumnCount, out int rem);

            return new Vector(rem, div);

        }

        public Vector GetOrEstimateElementU(int index)
        {
           return GetElementCoord(index);
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
                var newIndex = realizedIndex + count;

                for (var i = start; i < elementCount; ++i)
                {
                    if (_elements[i] is Control element)
                        updateElementIndex(element, newIndex - count, newIndex);
                    ++newIndex;
                }

                if (realizedIndex < 0)
                {
                    // The insertion point was before the first element, update the first index.
                    _firstIndex += count;
                }
                else
                {
                    // The insertion point was within the realized elements, insert an empty space
                    // in _elements and _sizes.
                    _elements!.InsertMany(realizedIndex, null, count);
                    _sizes!.InsertMany(realizedIndex, Size.Infinity, count);
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
                    {
                        _elements[i] = null;
                        recycleElement(element);
                    }
                }

                _elements.RemoveRange(start, end - start);
                _sizes!.RemoveRange(start, end - start);

                // If the remove started before and ended within our realized elements, then our new
                // first index will be the index where the remove started. Mark StartU as unstable
                // because we can't rely on it now to estimate element heights.
                if (startIndex <= 0 && end < last)
                {
                    _firstIndex = first = index;
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

            for (var i = 0; i < _elements.Count; i++)
            {
                if (_elements[i] is Control e)
                {
                    _elements[i] = null;
                    recycleElement(e);
                }
            }
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
                    {
                        _elements[i] = null;
                        recycleElement(e, i + FirstIndex);
                    }
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
                    {
                        _elements[i] = null;
                        recycleElement(e, i + FirstIndex);
                    }
                }

                _elements.RemoveRange(startIndex, _elements.Count - startIndex);
                _sizes!.RemoveRange(startIndex, _sizes.Count - startIndex);
            }
        }

        public void RecycleElement(int index, Action<Control, int> recycleElement)
        {
            if (index >= LastIndex || _elements is null || _elements.Count == 0)
                return;

            var startIndex = index - FirstIndex;
            var count = 1;

            for (var i = startIndex; i < count; ++i)
            {
                if (_elements[i] is Control e)
                {
                    _elements[i] = null;
                    recycleElement(e, i + FirstIndex);
                }
            }

            _elements.RemoveRange(startIndex, 1);
            _sizes!.RemoveRange(startIndex, 1);
        }

        /// <summary>
        /// Recycles all realized elements.
        /// </summary>
        /// <param name="recycleElement">A method used to recycle elements.</param>
        public void RecycleAllElements(Action<Control, int> recycleElement)
        {
            if (_elements is null || _elements.Count == 0)
                return;

            for (var i = 0; i < _elements.Count; i++)
            {
                if (_elements[i] is Control e)
                {
                    _elements[i] = null;
                    recycleElement(e, i + FirstIndex);
                }
            }

            _elements?.Clear();
            _sizes?.Clear();
        }

        /// <summary>
        /// Resets the element list and prepares it for reuse.
        /// </summary>
        public void ResetForReuse()
        {
            _elements?.Clear();
            _sizes?.Clear();
        }
    }

}
