// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System.Collections.Generic;
using System.Diagnostics;
using System;
using System.Text;

namespace Avalonia.Controls
{
    internal class IndexToValueTable<T> : IEnumerable<Range<T>>
    {
        private List<Range<T>> _list;

        public IndexToValueTable()
        {
            _list = new List<Range<T>>();
        }

        /// <summary>
        /// Total number of indices represented in the table
        /// </summary>
        public int IndexCount
        {
            get
            {
                int indexCount = 0;
                foreach (Range<T> range in _list)
                {
                    indexCount += range.Count;
                }
                return indexCount;
            }
        }

        /// <summary>
        /// Returns true if the table is empty
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                return _list.Count == 0;
            }
        }

        /// <summary>
        /// Returns the number of index ranges in the table
        /// </summary>
        public int RangeCount
        {
            get
            {
                return _list.Count;
            }
        }

        /// <summary>
        /// Add a value with an associated index to the table
        /// </summary>
        /// <param name="index">Index where the value is to be added or updated</param>
        /// <param name="value">Value to add</param>
        public void AddValue(int index, T value)
        {
            AddValues(index, 1, value);
        }

        /// <summary>
        /// Add multiples values with an associated start index to the table 
        /// </summary>
        /// <param name="startIndex">index where first value is added</param>
        /// <param name="count">Total number of values to add (must be greater than 0)</param>
        /// <param name="value">Value to add</param>
        public void AddValues(int startIndex, int count, T value)
        {
            Debug.Assert(count > 0);
            AddValuesPrivate(startIndex, count, value, null);
        }

        /// <summary>
        /// Clears the index table
        /// </summary>
        public void Clear()
        {
            _list.Clear();
        }

        /// <summary>
        /// Returns true if the given index is contained in the table
        /// </summary>
        /// <param name="index">index to search for</param>
        /// <returns>True if the index is contained in the table</returns>
        public bool Contains(int index)
        {
            return IsCorrectRangeIndex(this.FindRangeIndex(index), index);
        }

        /// <summary>
        /// Returns true if the entire given index range is contained in the table
        /// </summary>
        /// <param name="startIndex">beginning of the range</param>
        /// <param name="endIndex">end of the range</param>
        /// <returns>True if the entire index range is present in the table</returns>
        public bool ContainsAll(int startIndex, int endIndex)
        {
            int start = -1;
            int end = -1;

            foreach (Range<T> range in _list)
            {
                if (start == -1 && range.UpperBound >= startIndex)
                {
                    if (startIndex < range.LowerBound)
                    {
                        return false;
                    }
                    start = startIndex;
                    end = range.UpperBound;
                    if (end >= endIndex)
                    {
                        return true;
                    }
                }
                else if (start != -1)
                {
                    if (range.LowerBound > end + 1)
                    {
                        return false;
                    }
                    end = range.UpperBound;
                    if (end >= endIndex)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Returns true if the given index is contained in the table with the the given value
        /// </summary>
        /// <param name="index">index to search for</param>
        /// <param name="value">value expected</param>
        /// <returns>true if the given index is contained in the table with the the given value</returns>
        public bool ContainsIndexAndValue(int index, T value)
        {
            int lowerRangeIndex = this.FindRangeIndex(index);
            return ((IsCorrectRangeIndex(lowerRangeIndex, index)) && (_list[lowerRangeIndex].ContainsValue(value)));
        }

        /// <summary>
        /// Returns a copy of this IndexToValueTable
        /// </summary>
        /// <returns>copy of this IndexToValueTable</returns>
        public IndexToValueTable<T> Copy()
        {
            IndexToValueTable<T> copy = new IndexToValueTable<T>();
            foreach (Range<T> range in this._list)
            {
                copy._list.Add(range.Copy());
            }
            return copy;
        }

        public int GetNextGap(int index)
        {
            int targetIndex = index + 1;
            int rangeIndex = FindRangeIndex(targetIndex);
            if (IsCorrectRangeIndex(rangeIndex, targetIndex))
            {
                while (rangeIndex < _list.Count - 1 && _list[rangeIndex].UpperBound == _list[rangeIndex + 1].LowerBound - 1)
                {
                    rangeIndex++;
                }
                return _list[rangeIndex].UpperBound + 1;
            }
            else
            {
                return targetIndex;
            }
        }

        public int GetNextIndex(int index)
        {
            int targetIndex = index + 1;
            int rangeIndex = FindRangeIndex(targetIndex);
            if (IsCorrectRangeIndex(rangeIndex, targetIndex))
            {
                return targetIndex;
            }
            else
            {
                rangeIndex++;
                return rangeIndex < _list.Count ? _list[rangeIndex].LowerBound : -1;
            }
        }

        public int GetPreviousGap(int index)
        {
            int targetIndex = index - 1;
            int rangeIndex = FindRangeIndex(targetIndex);
            if (IsCorrectRangeIndex(rangeIndex, targetIndex))
            {
                while (rangeIndex > 0 && _list[rangeIndex].LowerBound == _list[rangeIndex - 1].UpperBound + 1)
                {
                    rangeIndex--;
                }
                return _list[rangeIndex].LowerBound - 1;
            }
            else
            {
                return targetIndex;
            }
        }

        public int GetPreviousIndex(int index)
        {
            int targetIndex = index - 1;
            int rangeIndex = FindRangeIndex(targetIndex);
            if (IsCorrectRangeIndex(rangeIndex, targetIndex))
            {
                return targetIndex;
            }
            else
            {
                return rangeIndex >= 0 && rangeIndex < _list.Count ? _list[rangeIndex].UpperBound : -1;
            }
        }

        /// <summary>
        /// Returns the inclusive index count between lowerBound and upperBound of all indexes with the given value
        /// </summary>
        /// <param name="lowerBound">lowerBound criteria</param>
        /// <param name="upperBound">upperBound criteria</param>
        /// <param name="value">value to look for</param>
        /// <returns>Number of indexes contained in the table between lowerBound and upperBound (inclusive)</returns>
        public int GetIndexCount(int lowerBound, int upperBound, T value)
        {
            Debug.Assert(upperBound >= lowerBound);
            if (_list.Count == 0)
            {
                return 0;
            }
            int count = 0;
            int index = FindRangeIndex(lowerBound);
            if (IsCorrectRangeIndex(index, lowerBound) && _list[index].ContainsValue(value))
            {
                count += _list[index].UpperBound - lowerBound + 1;
            }
            index++;
            while (index < _list.Count && _list[index].UpperBound <= upperBound)
            {
                if (_list[index].ContainsValue(value))
                {
                    count += _list[index].Count;
                }
                index++;
            }
            if (index < _list.Count && IsCorrectRangeIndex(index, upperBound) && _list[index].ContainsValue(value))
            {
                count += upperBound - _list[index].LowerBound;
            }
            return count;
        }

        /// <summary>
        /// Returns the inclusive index count between lowerBound and upperBound
        /// </summary>
        /// <param name="lowerBound">lowerBound criteria</param>
        /// <param name="upperBound">upperBound criteria</param>
        /// <returns>Number of indexes contained in the table between lowerBound and upperBound (inclusive)</returns>
        public int GetIndexCount(int lowerBound, int upperBound)
        {
            if (upperBound < lowerBound || _list.Count == 0)
            {
                return 0;
            }
            int count = 0;
            int index = this.FindRangeIndex(lowerBound);
            if (IsCorrectRangeIndex(index, lowerBound))
            {
                count += _list[index].UpperBound - lowerBound + 1;
            }
            index++;
            while (index < _list.Count && _list[index].UpperBound <= upperBound)
            {
                count += _list[index].Count;
                index++;
            }
            if (index < _list.Count && IsCorrectRangeIndex(index, upperBound))
            {
                count += upperBound - _list[index].LowerBound;
            }
            return count;
        }

        /// <summary>
        /// Returns the number indexes in this table after a given startingIndex but before
        /// reaching a gap of indexes of a given size
        /// </summary>
        /// <param name="startingIndex">Index to start at</param>
        /// <param name="gapSize">Size of index gap</param>
        /// <returns></returns>
        public int GetIndexCountBeforeGap(int startingIndex, int gapSize)
        {
            if (_list.Count == 0)
            {
                return 0;
            }

            int count = 0;
            int currentIndex = startingIndex;
            int rangeIndex = 0;
            int gap = 0;
            while (gap <= gapSize && rangeIndex < _list.Count)
            {
                gap += _list[rangeIndex].LowerBound - currentIndex;
                if (gap <= gapSize)
                {
                    count += _list[rangeIndex].UpperBound - _list[rangeIndex].LowerBound + 1;
                    currentIndex = _list[rangeIndex].UpperBound + 1;
                    rangeIndex++;
                }
            }
            return count;
        }

        /// <summary>
        /// Returns an enumerator that goes through the indexes present in the table
        /// </summary>
        /// <returns>an enumerator that enumerates the indexes present in the table</returns>
        public IEnumerable<int> GetIndexes()
        {
            Debug.Assert(_list != null);

            foreach (Range<T> range in _list)
            {
                for (int i = range.LowerBound; i <= range.UpperBound; i++)
                {
                    yield return i;
                }
            }
        }

        /// <summary>
        /// Returns all the indexes on or after a starting index
        /// </summary>
        /// <param name="startIndex">start index</param>
        /// <returns></returns>
        public IEnumerable<int> GetIndexes(int startIndex)
        {
            Debug.Assert(_list != null);

            int rangeIndex = FindRangeIndex(startIndex);
            if (rangeIndex == -1)
            {
                rangeIndex++;
            }

            while (rangeIndex < _list.Count)
            {
                for (int i = _list[rangeIndex].LowerBound; i <= _list[rangeIndex].UpperBound; i++)
                {
                    if (i >= startIndex)
                    {
                        yield return i;
                    }
                }
                rangeIndex++;
            }
        }

        /// <summary>
        /// Return the index of the Nth element in the table.
        /// </summary>
        /// <param name="n">n</param>
        public int GetNthIndex(int n)
        {
            Debug.Assert(n >= 0 && n < this.IndexCount);
            int cumulatedEntries = 0;
            foreach (Range<T> range in _list)
            {
                if (cumulatedEntries + range.Count > n)
                {
                    return range.LowerBound + n - cumulatedEntries;
                }
                else
                {
                    cumulatedEntries += range.Count;
                }
            }
            return -1;
        }

        /// <summary>
        /// Returns the value at a given index or the default value if the index is not in the table
        /// </summary>
        /// <param name="index">index to search for</param>
        /// <returns>the value at the given index or the default value if index is not in the table</returns>
        public T GetValueAt(int index)
        {
            return GetValueAt(index, out bool found);
        }

        /// <summary>
        /// Returns the value at a given index or the default value if the index is not in the table
        /// </summary>
        /// <param name="index">index to search for</param>
        /// <param name="found">set to true by the method if the index was found; otherwise, false</param>
        /// <returns>the value at the given index or the default value if index is not in the table</returns>
        public T GetValueAt(int index, out bool found)
        {
            int rangeIndex = this.FindRangeIndex(index);
            if (this.IsCorrectRangeIndex(rangeIndex, index))
            {
                found = true;
                return _list[rangeIndex].Value;
            }
            else
            {
                found = false;
                return default(T);
            }
        }

        /// <summary>
        /// Returns an index's index within this table
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public int IndexOf(int index)
        {
            int cumulatedIndexes = 0;
            foreach (Range<T> range in _list)
            {
                if (range.UpperBound >= index)
                {
                    cumulatedIndexes += index - range.LowerBound;
                    break;
                }
                else
                {
                    cumulatedIndexes += range.Count;
                }
            }
            return cumulatedIndexes;
        }

        /// <summary>
        /// Inserts an index at the given location.  This does not alter values in the table
        /// </summary>
        /// <param name="index">index location to insert an index</param>
        public void InsertIndex(int index)
        {
            InsertIndexes(index, 1);
        }

        /// <summary>
        /// Inserts an index into the table with the given value 
        /// </summary>
        /// <param name="index">index to insert</param>
        /// <param name="value">value for the index</param>
        public void InsertIndexAndValue(int index, T value)
        {
            InsertIndexesAndValues(index, 1, value);
        }

        /// <summary>
        /// Inserts multiple indexes into the table.  This does not alter Values in the table
        /// </summary>
        /// <param name="startIndex">first index to insert</param>
        /// <param name="count">total number of indexes to insert</param>
        public void InsertIndexes(int startIndex, int count)
        {
            Debug.Assert(count > 0);
            InsertIndexesPrivate(startIndex, count, this.FindRangeIndex(startIndex));
        }

        /// <summary>
        /// Inserts multiple indexes into the table with the given value 
        /// </summary>
        /// <param name="startIndex">Index to insert first value</param>
        /// <param name="count">Total number of values to insert (must be greater than 0)</param>
        /// <param name="value">Value to insert</param>
        public void InsertIndexesAndValues(int startIndex, int count, T value)
        {
            Debug.Assert(count > 0);
            int lowerRangeIndex = this.FindRangeIndex(startIndex);
            InsertIndexesPrivate(startIndex, count, lowerRangeIndex);
            if ((lowerRangeIndex >= 0) && (_list[lowerRangeIndex].LowerBound > startIndex))
            {
                // Because of the insert, the original range no longer contains the startIndex
                lowerRangeIndex--;
            }
            AddValuesPrivate(startIndex, count, value, lowerRangeIndex);
        }

        /// <summary>
        /// Removes an index from the table.  This does not alter Values in the table
        /// </summary>
        /// <param name="index">index to remove</param>
        public void RemoveIndex(int index)
        {
            RemoveIndexes(index, 1);
        }

        /// <summary>
        /// Removes a value and its index from the table
        /// </summary>
        /// <param name="index">index to remove</param>
        public void RemoveIndexAndValue(int index)
        {
            RemoveIndexesAndValues(index, 1);
        }

        /// <summary>
        /// Removes multiple indexes from the table.  This does not alter Values in the table
        /// </summary>
        /// <param name="startIndex">first index to remove</param>
        /// <param name="count">total number of indexes to remove</param>
        public void RemoveIndexes(int startIndex, int count)
        {
            int lowerRangeIndex = this.FindRangeIndex(startIndex);
            if (lowerRangeIndex < 0)
            {
                lowerRangeIndex = 0;
            }
            int i = lowerRangeIndex;
            while (i < _list.Count)
            {
                Range<T> range = _list[i];
                if (range.UpperBound >= startIndex)
                {
                    if (range.LowerBound >= startIndex + count)
                    {
                        // Both bounds will remain after the removal
                        range.LowerBound -= count;
                        range.UpperBound -= count;
                    }
                    else
                    {
                        int currentIndex = i;
                        if (range.LowerBound <= startIndex)
                        {
                            // Range gets split up
                            if (range.UpperBound >= startIndex + count)
                            {
                                i++;
                                _list.Insert(i, new Range<T>(startIndex, range.UpperBound - count, range.Value));
                            }
                            range.UpperBound = startIndex - 1;
                        }
                        else
                        {
                            range.LowerBound = startIndex;
                            range.UpperBound -= count;
                        }
                        if (RemoveRangeIfInvalid(range, currentIndex))
                        {
                            i--;
                        }
                    }
                }
                i++;
            }
            if (!this.Merge(lowerRangeIndex))
            {
                this.Merge(lowerRangeIndex + 1);
            }
        }

        /// <summary>
        /// Removes multiple values and their indexes from the table
        /// </summary>
        /// <param name="startIndex">first index to remove</param>
        /// <param name="count">total number of indexes to remove</param>
        public void RemoveIndexesAndValues(int startIndex, int count)
        {
            RemoveValues(startIndex, count);
            RemoveIndexes(startIndex, count);
        }

        /// <summary>
        /// Removes a value from the table at the given index.  This does not alter other indexes in the table.
        /// </summary>
        /// <param name="index">index where value should be removed</param>
        public void RemoveValue(int index)
        {
            RemoveValues(index, 1);
        }

        /// <summary>
        /// Removes multiple values from the table.  This does not alter other indexes in the table.
        /// </summary>
        /// <param name="startIndex">first index where values should be removed </param>
        /// <param name="count">total number of values to remove</param>
        public void RemoveValues(int startIndex, int count)
        {
            Debug.Assert(count > 0);

            int lowerRangeIndex = this.FindRangeIndex(startIndex);
            if (lowerRangeIndex < 0)
            {
                lowerRangeIndex = 0;
            }
            while ((lowerRangeIndex < _list.Count) && (_list[lowerRangeIndex].UpperBound < startIndex))
            {
                lowerRangeIndex++;
            }
            if (lowerRangeIndex >= _list.Count || _list[lowerRangeIndex].LowerBound > startIndex + count - 1)
            {
                // If all the values are above our below our values, we have nothing to remove
                return;
            }
            if (_list[lowerRangeIndex].LowerBound < startIndex)
            {
                // Need to split this up
                _list.Insert(lowerRangeIndex, new Range<T>(_list[lowerRangeIndex].LowerBound, startIndex - 1, _list[lowerRangeIndex].Value));
                lowerRangeIndex++;
            }
            _list[lowerRangeIndex].LowerBound = startIndex + count;
            if (!RemoveRangeIfInvalid(_list[lowerRangeIndex], lowerRangeIndex))
            {
                lowerRangeIndex++;
            }
            while ((lowerRangeIndex < _list.Count) && (_list[lowerRangeIndex].UpperBound < startIndex + count))
            {
                _list.RemoveAt(lowerRangeIndex);
            }
            if ((lowerRangeIndex < _list.Count) && (_list[lowerRangeIndex].UpperBound >= startIndex + count) &&
                (_list[lowerRangeIndex].LowerBound < startIndex + count))
            {
                // Chop off the start of the remaining Range if it contains values that we're removing
                _list[lowerRangeIndex].LowerBound = startIndex + count;
                RemoveRangeIfInvalid(_list[lowerRangeIndex], lowerRangeIndex);
            }
        }

        private void AddValuesPrivate(int startIndex, int count, T value, int? startRangeIndex)
        {
            Debug.Assert(count > 0);

            int endIndex = startIndex + count - 1;
            Range<T> newRange = new Range<T>(startIndex, endIndex, value);
            if (_list.Count == 0)
            {
                _list.Add(newRange);
            }
            else
            {
                int lowerRangeIndex = startRangeIndex ?? FindRangeIndex(startIndex);
                Range<T> lowerRange = (lowerRangeIndex < 0) ? null : _list[lowerRangeIndex];
                if (lowerRange == null)
                {
                    if (lowerRangeIndex < 0)
                    {
                        lowerRangeIndex = 0;
                    }
                    _list.Insert(lowerRangeIndex, newRange);
                }
                else
                {
                    if (!lowerRange.Value.Equals(value) && (lowerRange.UpperBound >= startIndex))
                    {
                        // Split up the range
                        if (lowerRange.UpperBound > endIndex)
                        {
                            _list.Insert(lowerRangeIndex + 1, new Range<T>(endIndex + 1, lowerRange.UpperBound, lowerRange.Value));
                        }
                        lowerRange.UpperBound = startIndex - 1;
                        if (!RemoveRangeIfInvalid(lowerRange, lowerRangeIndex))
                        {
                            lowerRangeIndex++;
                        }
                        _list.Insert(lowerRangeIndex, newRange);
                    }
                    else
                    {
                        _list.Insert(lowerRangeIndex + 1, newRange);
                        if (!Merge(lowerRangeIndex))
                        {
                            lowerRangeIndex++;
                        }
                    }
                }

                // At this point the newRange has been inserted in the correct place, now we need to remove
                // any subsequent ranges that no longer make sense and possibly update the one at newRange.UpperBound
                int upperRangeIndex = lowerRangeIndex + 1;
                while ((upperRangeIndex < _list.Count) && (_list[upperRangeIndex].UpperBound < endIndex))
                {
                    _list.RemoveAt(upperRangeIndex);
                }
                if (upperRangeIndex < _list.Count)
                {
                    Range<T> upperRange = _list[upperRangeIndex];
                    if (upperRange.LowerBound <= endIndex)
                    {
                        // Update the range
                        upperRange.LowerBound = endIndex + 1;
                        RemoveRangeIfInvalid(upperRange, upperRangeIndex);
                    }
                    Merge(lowerRangeIndex);
                }
            }
        }

        // Returns the index of the range that contains the input or the range before if the input is not found
        private int FindRangeIndex(int index)
        {
            if (_list.Count == 0)
            {
                return -1;
            }

            // Do a binary search for the index
            int front = 0;
            int end = _list.Count - 1;
            Range<T> range = null;
            while (end > front)
            {
                int median = (front + end) / 2;
                range = _list[median];
                if (range.UpperBound < index)
                {
                    front = median + 1;
                }
                else if (range.LowerBound > index)
                {
                    end = median - 1;
                }
                else
                {
                    // we found it
                    return median;
                }
            }

            if (front == end)
            {
                range = _list[front];
                if (range.ContainsIndex(index) || (range.UpperBound < index))
                {
                    // we found it or the index isn't there and we're one range before
                    return front;
                }
                else
                {
                    // not found and we're one range after
                    return front - 1;
                }
            }
            else
            {
                // end is one index before front in this case so it's the range before
                return end;
            }
        }

        private bool Merge(int lowerRangeIndex)
        {
            int upperRangeIndex = lowerRangeIndex + 1;
            if ((lowerRangeIndex >= 0) && (upperRangeIndex < _list.Count))
            {
                Range<T> lowerRange = _list[lowerRangeIndex];
                Range<T> upperRange = _list[upperRangeIndex];
                if ((lowerRange.UpperBound + 1 >= upperRange.LowerBound) && (lowerRange.Value.Equals(upperRange.Value)))
                {
                    lowerRange.UpperBound = Math.Max(lowerRange.UpperBound, upperRange.UpperBound);
                    _list.RemoveAt(upperRangeIndex);
                    return true;
                }
            }
            return false;
        }

        private void InsertIndexesPrivate(int startIndex, int count, int lowerRangeIndex)
        {
            Debug.Assert(count > 0);

            // Same as AddRange after we fix the indicies affected by the insertion
            int startRangeIndex = (lowerRangeIndex >= 0) ? lowerRangeIndex : 0;
            for (int i = startRangeIndex; i < _list.Count; i++)
            {
                Range<T> range = _list[i];
                if (range.LowerBound >= startIndex)
                {
                    range.LowerBound += count;
                }
                else
                {
                    if (range.UpperBound >= startIndex)
                    {
                        // Split up this range
                        i++;
                        _list.Insert(i, new Range<T>(startIndex, range.UpperBound + count, range.Value));
                        range.UpperBound = startIndex - 1;
                        continue;
                    }
                }

                if (range.UpperBound >= startIndex)
                {
                    range.UpperBound += count;
                }
            }
        }

        private bool IsCorrectRangeIndex(int rangeIndex, int index)
        {
            return (-1 != rangeIndex) && (_list[rangeIndex].ContainsIndex(index));
        }

        private bool RemoveRangeIfInvalid(Range<T> range, int rangeIndex)
        {
            if (range.UpperBound < range.LowerBound)
            {
                _list.RemoveAt(rangeIndex);
                return true;
            }
            return false;
        }

        public IEnumerator<Range<T>> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _list.GetEnumerator();
        }

#if DEBUG

        public void PrintIndexes()
        {
            Debug.WriteLine(this.IndexCount + " indexes");
            foreach (Range<T> range in _list)
            {
                Debug.WriteLine(String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0} - {1}", range.LowerBound, range.UpperBound));
            }
        }

#endif
    }
}
