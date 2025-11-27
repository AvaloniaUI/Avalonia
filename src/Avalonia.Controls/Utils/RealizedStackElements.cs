using System;
using System.Collections.Generic;
using Avalonia.Layout;
using Avalonia.Logging;
using Avalonia.Utilities;

namespace Avalonia.Controls.Utils
{
    /// <summary>
    /// Stores the realized element state for a virtualizing panel that arranges its children
    /// in a stack layout, such as <see cref="VirtualizingStackPanel"/>.
    /// </summary>
    internal class RealizedStackElements
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
        /// Gets the position of the first element on the primary axis, or NaN if the position is
        /// unstable.
        /// </summary>
        public double StartU => _startUUnstable ? double.NaN : _startU;

        /// <summary>
        /// Adds a newly realized element to the collection.
        /// </summary>
        /// <param name="index">The index of the element.</param>
        /// <param name="element">The element.</param>
        /// <param name="u">The position of the element on the primary axis.</param>
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
        /// Gets the position of the element with the requested index on the primary axis, if realized.
        /// </summary>
        /// <returns>
        /// The position of the element, or NaN if the element is not realized.
        /// </returns>
        public double GetElementU(int index)
        {
            if (index < FirstIndex || _sizes is null)
                return double.NaN;

            var endIndex = index - FirstIndex;

            if (endIndex >= _sizes.Count)
                return double.NaN;

            var u = StartU;

            for (var i = 0; i < endIndex; ++i)
                u += _sizes[i];

            return u;
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

                for (var i = start; i < elementCount; ++i)
                {
                    if (_elements[i] is not Control element)
                        continue;
                    var oldIndex = i + first;
                    updateElementIndex(element, oldIndex, oldIndex + count);
                }

                if (realizedIndex < 0)
                {
                    // The insertion point was before the first element, update the first index.
                    _firstIndex += count;
                    _startUUnstable = true;
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
                        updateElementIndex(element, newIndex + count, newIndex);
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
        /// Updates the elements in response to items being replaced in the source collection.
        /// </summary>
        /// <param name="index">The index in the source collection of the remove.</param>
        /// <param name="count">The number of items removed.</param>
        /// <param name="recycleElement">A method used to recycle elements.</param>
        public void ItemsReplaced(int index, int count, Action<Control> recycleElement)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (_elements is null || _elements.Count == 0)
                return;

            // Get the index within the realized _elements collection.
            var startIndex = index - FirstIndex;
            var endIndex = Math.Min(startIndex + count, Count);

            if (startIndex >= 0 && endIndex > startIndex)
            {
                for (var i = startIndex; i < endIndex; ++i)
                {
                    if (_elements[i] is { } element)
                    {
                        recycleElement(element);
                        _elements[i] = null;
                        _sizes![i] = double.NaN;
                    }
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

        /// <summary>
        /// Nullifies an element at the specified index without recycling it.
        /// The element slot becomes null so that RecycleAllElements will skip it.
        /// </summary>
        /// <param name="index">The index in the source collection.</param>
        /// <returns>The element and its size, or null if not found.</returns>
        public (Control element, double sizeU)? NullifyElement(int index)
        {
            if (_elements is null || _elements.Count == 0)
                return null;

            var i = index - FirstIndex;
            if (i < 0 || i >= _elements.Count)
                return null;

            if (_elements[i] is not Control element)
                return null;

            var sizeU = _sizes![i];
            _elements[i] = null;
            return (element, sizeU);
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

        /// <summary>
        /// Validates that <see cref="StartU"/> is still valid.
        /// </summary>
        /// <param name="orientation">The panel orientation.</param>
        /// <remarks>
        /// Checks if any realized element's DesiredSize differs from the stored size.
        /// - Small changes (&lt; 1px): absorbed silently by updating stored sizes. StartU stays stable.
        ///   This prevents layout cycles with complex items that produce slightly different sizes each measure.
        /// - Large changes (&gt;= 1px): genuine resize — marks StartU as unstable.
        /// </remarks>
        /// <summary>
        /// Checks whether any realized element's DesiredSize has changed from the stored size.
        /// Returns true if a significant (>= 1px) change was detected.
        /// When <paramref name="anchorIndex"/> is provided and only items OUTSIDE the viewport
        /// changed size (the anchor item itself is stable), compensates StartU by the accumulated
        /// pre-anchor delta to prevent scroll jumping from async content loading.
        /// If the anchor item itself changed, marks StartU as unstable (global resize scenario).
        /// Updates stored sizes in-place unless <paramref name="lockSizes"/> is true.
        /// When lockSizes is true (during extent oscillation), stored sizes are NOT updated for
        /// significant changes, preventing item position shifts within the viewport.
        /// </summary>
        public bool ValidateStartU(Orientation orientation, int anchorIndex, out double preDelta, bool lockSizes = false)
        {
            preDelta = 0;

            if (_elements is null || _sizes is null || _startUUnstable)
                return false;

            var hasSignificantChange = false;
            var anchorChanged = false;
            var anchorMeasurePending = false;
            var otherItemsChanged = false;
            var otherItemsPendingMeasure = false;

            for (var i = 0; i < _elements.Count; ++i)
            {
                if (_elements[i] is not { } element)
                    continue;

                var itemIndex = _firstIndex + i;

                // Detect partial layout manager state: elements whose data changed
                // but the layout manager hasn't re-measured them yet.
                if (!element.IsMeasureValid)
                {
                    if (itemIndex == anchorIndex)
                        anchorMeasurePending = true;
                    else
                        otherItemsPendingMeasure = true;
                }

                var sizeU = orientation == Orientation.Horizontal ?
                    element.DesiredSize.Width : element.DesiredSize.Height;

                var diff = sizeU - _sizes[i];
                if (diff == 0)
                    continue;

                if (Math.Abs(diff) >= 1.0)
                {
                    if (Logger.TryGet(LogEventLevel.Warning, LogArea.Control) is { } log)
                    {
                        var dc = (element as StyledElement)?.DataContext;
                        log.Log(element,
                            "Item template size changed significantly during layout. " +
                            "This typically means the item template produces non-deterministic sizes " +
                            "(e.g., async image loading, text wrapping). Consider using fixed-size templates. " +
                            "DataContext='{DataContext}', OldSize='{OldSize}', NewSize='{NewSize}', Diff='{Diff}' " +
                            "(#{HashCode} idx={ItemIndex})",
                            dc?.GetType().FullName ?? "(null)", _sizes[i], sizeU, diff,
                            element.GetHashCode(), itemIndex);
                    }

                    // During extent oscillation (lockSizes=true), still track size
                    // changes for position compensation — if an item before the
                    // viewport anchor shrinks/grows, StartU must be adjusted to
                    // keep visible items at their current positions. Update stored
                    // sizes so future passes don't re-detect the same change.
                    // But don't set hasSignificantChange — that would invalidate
                    // the estimate cache and cause extent oscillation.
                    if (lockSizes)
                    {
                        if (anchorIndex >= 0 && itemIndex <= anchorIndex)
                            preDelta += diff;
                        _sizes[i] = sizeU;
                        continue;
                    }

                    hasSignificantChange = true;

                    if (anchorIndex >= 0 && itemIndex < anchorIndex)
                    {
                        preDelta += diff;
                        otherItemsChanged = true;
                    }
                    else if (itemIndex == anchorIndex)
                    {
                        anchorChanged = true;
                    }
                    else
                    {
                        otherItemsChanged = true;
                    }

                    // Update stored size so the next pass won't re-detect this change
                    _sizes[i] = sizeU;
                }
                else
                {
                    // Minor fluctuation (< 1px) — absorb by updating stored size.
                    _sizes[i] = sizeU;
                }
            }

            if (!hasSignificantChange && !anchorMeasurePending)
            {
                // No significant (non-locked) changes detected. But we may have
                // accumulated preDelta from locked-size items before the anchor.
                // Compensate StartU to keep visible items stable.
                if (Math.Abs(preDelta) >= 1.0)
                {
                    _startU -= preDelta;
                    return true;
                }
                return false;
            }

            if (anchorMeasurePending ||
                (anchorChanged && (otherItemsChanged || otherItemsPendingMeasure)))
            {
                // Either the anchor hasn't been re-measured yet (partial layout state),
                // or the anchor changed AND other items also changed or are pending
                // re-measure (uniform resize scenario). Mark unstable so the layout
                // re-evaluates positions from scratch.
                _startUUnstable = true;
            }
            else if (anchorChanged)
            {
                // Only the anchor itself changed size and no other items are affected
                // (e.g., async content loading on the visible item). The anchor's
                // START position is still correct — only items after it shift.
                // Return false: the stored size is already updated (preventing
                // re-detection), and the normal realization flow will handle
                // the shifted positions. Returning false avoids resetting the
                // estimate cache in MeasureOverride, which would cause the estimate
                // to oscillate when items alternate between loaded/unloaded sizes
                // (e.g., async images cycling between 84px placeholder and 306px loaded).
                return false;
            }
            else if (Math.Abs(preDelta) >= 1.0)
            {
                // Only items before the anchor changed (async content loading).
                // Subtract preDelta from StartU to keep the anchor at its visual position:
                //   anchor_pos = startU + sum_of_sizes_before_anchor
                // If sizes_before grew by preDelta, decrease startU by the same amount.
                _startU -= preDelta;
            }

            return true;
        }

        /// <summary>
        /// Adjusts StartU to compensate for extent changes outside the realized range.
        /// This prevents scroll jumping by maintaining the visual position of realized elements.
        /// </summary>
        public void CompensateStartU(double delta)
        {
            if (_startUUnstable || double.IsNaN(_startU))
                return;

            _startU += delta;
        }
    }
}
