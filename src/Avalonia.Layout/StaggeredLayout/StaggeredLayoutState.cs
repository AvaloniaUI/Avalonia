// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Avalonia.Layout
{
    internal class StaggeredLayoutState
    {
        private List<StaggeredItem> _items = new List<StaggeredItem>();
        private VirtualizingLayoutContext _context;
        private Dictionary<int, StaggeredColumnLayout> _columnLayout = new Dictionary<int, StaggeredColumnLayout>();
        private double _lastAverageHeight;

        public StaggeredLayoutState(VirtualizingLayoutContext context)
        {
            _context = context;
        }

        public double ColumnWidth { get; internal set; }

        public int NumberOfColumns
        {
            get
            {
                return _columnLayout.Count;
            }
        }

        public double RowSpacing { get; internal set; }

        internal void AddItemToColumn(StaggeredItem item, int columnIndex)
        {
            if (_columnLayout.TryGetValue(columnIndex, out StaggeredColumnLayout columnLayout) == false)
            {
                columnLayout = new StaggeredColumnLayout();
                _columnLayout[columnIndex] = columnLayout;
            }

            if (columnLayout.Contains(item) == false)
            {
                columnLayout.Add(item);
            }
        }

        internal StaggeredItem GetItemAt(int index)
        {
            if (index < 0)
            {
                throw new IndexOutOfRangeException();
            }

            if (index <= (_items.Count - 1))
            {
                return _items[index];
            }
            else
            {
                StaggeredItem item = new StaggeredItem(index);
                _items.Add(item);
                return item;
            }
        }

        internal StaggeredColumnLayout GetColumnLayout(int columnIndex)
        {
            _columnLayout.TryGetValue(columnIndex, out StaggeredColumnLayout columnLayout);
            return columnLayout;
        }

        /// <summary>
        /// Clear everything that has been calculated.
        /// </summary>
        internal void Clear()
        {
            _columnLayout.Clear();
            _items.Clear();
        }

        /// <summary>
        /// Clear the layout columns so they will be recalculated.
        /// </summary>
        internal void ClearColumns()
        {
            _columnLayout.Clear();
        }

        /// <summary>
        /// Gets the estimated height of the layout.
        /// </summary>
        /// <returns>The estimated height of the layout.</returns>
        /// <remarks>
        /// If all of the items have been calculated then the actual height will be returned.
        /// If all of the items have not been calculated then an estimated height will be calculated based on the average height of the items.
        /// </remarks>
        internal double GetHeight()
        {
            double desiredHeight = Enumerable.Max(_columnLayout.Values, c => c.Height);

            var itemCount = Enumerable.Sum(_columnLayout.Values, c => c.Count);
            if (itemCount == _context.ItemCount)
            {
                return desiredHeight;
            }

            double averageHeight = 0;
            foreach (var kvp in _columnLayout)
            {
                averageHeight += kvp.Value.Height / kvp.Value.Count;
            }

            averageHeight /= _columnLayout.Count;
            double estimatedHeight = (averageHeight * _context.ItemCount) / _columnLayout.Count;
            if (estimatedHeight > desiredHeight)
            {
                desiredHeight = estimatedHeight;
            }

            if (Math.Abs(desiredHeight - _lastAverageHeight) < 5)
            {
                return _lastAverageHeight;
            }

            _lastAverageHeight = desiredHeight;
            return desiredHeight;
        }

        internal void RecycleElementAt(int index)
        {
            var element = _context.GetOrCreateElementAt(index);
            _context.RecycleElement(element);
        }

        internal void RemoveFromIndex(int index)
        {
            if (index >= _items.Count)
            {
                // Item was added/removed but we haven't realized that far yet
                return;
            }

            int numToRemove = _items.Count - index;
            _items.RemoveRange(index, numToRemove);

            foreach (var kvp in _columnLayout)
            {
                StaggeredColumnLayout layout = kvp.Value;
                for (int i = 0; i < layout.Count; i++)
                {
                    if (layout[i].Index >= index)
                    {
                        numToRemove = layout.Count - i;
                        layout.RemoveRange(i, numToRemove);
                        break;
                    }
                }
            }
        }

        internal void RemoveRange(int startIndex, int endIndex)
        {
            for (int i = startIndex; i <= endIndex; i++)
            {
                if (i > _items.Count)
                {
                    break;
                }

                StaggeredItem item = _items[i];
                item.Height = 0;
                item.Top = 0;

                // We must recycle all elements to ensure that it gets the correct context
                RecycleElementAt(i);
            }

            foreach (var kvp in _columnLayout)
            {
                StaggeredColumnLayout layout = kvp.Value;
                for (int i = 0; i < layout.Count; i++)
                {
                    if ((startIndex <= layout[i].Index) && (layout[i].Index <= endIndex))
                    {
                        int numToRemove = layout.Count - i;
                        layout.RemoveRange(i, numToRemove);
                        break;
                    }
                }
            }
        }
    }
}
