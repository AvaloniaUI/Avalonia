﻿// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Avalonia.Controls
{
    internal class DataGridColumnCollection : ObservableCollection<DataGridColumn>
    {
        private readonly DataGrid _owningGrid;

        public DataGridColumnCollection(DataGrid owningGrid)
        {
            _owningGrid = owningGrid;
            ItemsInternal = new List<DataGridColumn>();
            FillerColumn = new DataGridFillerColumn(owningGrid);
            RowGroupSpacerColumn = new DataGridFillerColumn(owningGrid);
            DisplayIndexMap = new List<int>();
        }

        internal int AutogeneratedColumnCount
        {
            get;
            set;
        }

        internal List<int> DisplayIndexMap
        {
            get;
            set;
        }

        internal DataGridFillerColumn FillerColumn
        {
            get;
            private set;
        }

        internal DataGridColumn FirstColumn
        {
            get
            {
                return GetFirstColumn(null /*isVisible*/, null /*isFrozen*/, null /*isReadOnly*/);
            }
        }

        internal DataGridColumn FirstVisibleColumn
        {
            get
            {
                return GetFirstColumn(true /*isVisible*/, null /*isFrozen*/, null /*isReadOnly*/);
            }
        }

        internal DataGridColumn FirstVisibleNonFillerColumn
        {
            get
            {
                DataGridColumn dataGridColumn = FirstVisibleColumn;
                if (dataGridColumn == RowGroupSpacerColumn)
                {
                    dataGridColumn = GetNextVisibleColumn(dataGridColumn);
                }
                return dataGridColumn;
            }
        }

        internal DataGridColumn FirstVisibleWritableColumn
        {
            get
            {
                return GetFirstColumn(true /*isVisible*/, null /*isFrozen*/, false /*isReadOnly*/);
            }
        }

        internal DataGridColumn FirstVisibleScrollingColumn
        {
            get
            {
                return GetFirstColumn(true /*isVisible*/, false /*isFrozen*/, null /*isReadOnly*/);
            }
        }

        internal List<DataGridColumn> ItemsInternal
        {
            get;
            private set;
        }

        internal DataGridColumn LastVisibleColumn
        {
            get
            {
                return GetLastColumn(true /*isVisible*/, null /*isFrozen*/, null /*isReadOnly*/);
            }
        }

        internal DataGridColumn LastVisibleScrollingColumn
        {
            get
            {
                return GetLastColumn(true /*isVisible*/, false /*isFrozen*/, null /*isReadOnly*/);
            }
        }

        internal DataGridColumn LastVisibleWritableColumn
        {
            get
            {
                return GetLastColumn(true /*isVisible*/, null /*isFrozen*/, false /*isReadOnly*/);
            }
        }

        internal DataGridFillerColumn RowGroupSpacerColumn
        {
            get;
            private set;
        }

        internal int VisibleColumnCount
        {
            get;
            private set;
        }

        internal double VisibleEdgedColumnsWidth
        {
            get;
            private set;
        }

        /// <summary>
        /// The number of star columns that are currently visible.
        /// NOTE: Requires that EnsureVisibleEdgedColumnsWidth has been called.
        /// </summary>
        internal int VisibleStarColumnCount
        {
            get;
            private set;
        }

        protected override void ClearItems()
        {
            try
            {
                _owningGrid.NoCurrentCellChangeCount++;
                if (ItemsInternal.Count > 0)
                {
                    if (_owningGrid.InDisplayIndexAdjustments)
                    {
                        // We are within columns display indexes adjustments. We do not allow changing the column collection while adjusting display indexes.
                        throw DataGridError.DataGrid.CannotChangeColumnCollectionWhileAdjustingDisplayIndexes();
                    }

                    _owningGrid.OnClearingColumns();
                    for (int columnIndex = 0; columnIndex < ItemsInternal.Count; columnIndex++)
                    {
                        // Detach the column...
                        ItemsInternal[columnIndex].OwningGrid = null;
                    }
                    ItemsInternal.Clear();
                    DisplayIndexMap.Clear();
                    AutogeneratedColumnCount = 0;
                    _owningGrid.OnColumnCollectionChanged_PreNotification(false /*columnsGrew*/);
                    base.ClearItems();
                    VisibleEdgedColumnsWidth = 0;
                    _owningGrid.OnColumnCollectionChanged_PostNotification(false /*columnsGrew*/);
                }
            }
            finally
            {
                _owningGrid.NoCurrentCellChangeCount--;
            }
        }

        protected override void InsertItem(int columnIndex, DataGridColumn dataGridColumn)
        {
            try
            {
                _owningGrid.NoCurrentCellChangeCount++;
                if (_owningGrid.InDisplayIndexAdjustments)
                {
                    // We are within columns display indexes adjustments. We do not allow changing the column collection while adjusting display indexes.
                    throw DataGridError.DataGrid.CannotChangeColumnCollectionWhileAdjustingDisplayIndexes();
                }
                if (dataGridColumn == null)
                {
                    throw new ArgumentNullException(nameof(dataGridColumn));
                }

                int columnIndexWithFiller = columnIndex;
                if (dataGridColumn != RowGroupSpacerColumn && RowGroupSpacerColumn.IsRepresented)
                {
                    columnIndexWithFiller++;
                }

                // get the new current cell coordinates
                DataGridCellCoordinates newCurrentCellCoordinates = _owningGrid.OnInsertingColumn(columnIndex, dataGridColumn);

                // insert the column into our internal list
                ItemsInternal.Insert(columnIndexWithFiller, dataGridColumn);
                dataGridColumn.Index = columnIndexWithFiller;
                dataGridColumn.OwningGrid = _owningGrid;
                dataGridColumn.RemoveEditingElement();
                if (dataGridColumn.IsVisible)
                {
                    VisibleEdgedColumnsWidth += dataGridColumn.ActualWidth;
                }

                // continue with the base insert
                _owningGrid.OnInsertedColumn_PreNotification(dataGridColumn);
                _owningGrid.OnColumnCollectionChanged_PreNotification(true /*columnsGrew*/);

                if (dataGridColumn != RowGroupSpacerColumn)
                {
                    base.InsertItem(columnIndex, dataGridColumn);
                }
                _owningGrid.OnInsertedColumn_PostNotification(newCurrentCellCoordinates, dataGridColumn.DisplayIndex);
                _owningGrid.OnColumnCollectionChanged_PostNotification(true /*columnsGrew*/);
            }
            finally
            {
                _owningGrid.NoCurrentCellChangeCount--;
            }
        }

        protected override void RemoveItem(int columnIndex)
        {
            RemoveItemPrivate(columnIndex, false /*isSpacer*/);
        }

        protected override void SetItem(int columnIndex, DataGridColumn dataGridColumn)
        {
            RemoveItem(columnIndex);
            InsertItem(columnIndex, dataGridColumn);
        }

        internal bool DisplayInOrder(int columnIndex1, int columnIndex2)
        {
            int displayIndex1 = ItemsInternal[columnIndex1].DisplayIndexWithFiller;
            int displayIndex2 = ItemsInternal[columnIndex2].DisplayIndexWithFiller;
            return displayIndex1 < displayIndex2;
        }

        internal bool EnsureRowGrouping(bool rowGrouping)
        {
            // The insert below could cause the first column to be added.  That causes a refresh 
            // which re-enters method so instead of checking RowGroupSpacerColumn.IsRepresented, 
            // we need to check to see if it's actually in our collection instead.
            bool spacerRepresented = (ItemsInternal.Count > 0) && (ItemsInternal[0] == RowGroupSpacerColumn);
            if (rowGrouping && !spacerRepresented)
            {
                Insert(0, RowGroupSpacerColumn);
                RowGroupSpacerColumn.IsRepresented = true;
                return true;
            }
            else if (!rowGrouping && spacerRepresented)
            {
                // We need to set IsRepresented to false before removing the RowGroupSpacerColumn
                // otherwise, we'll remove the column after it
                RowGroupSpacerColumn.IsRepresented = false;
                RemoveItemPrivate(0, true /*isSpacer*/);
                return true;
            }
            return false;
        }

        /// <summary>
        /// In addition to ensuring that column widths are valid, method updates the values of
        /// VisibleEdgedColumnsWidth and VisibleStarColumnCount.
        /// </summary>
        internal void EnsureVisibleEdgedColumnsWidth()
        {
            VisibleStarColumnCount = 0;
            VisibleEdgedColumnsWidth = 0;
            VisibleColumnCount = 0;

            for (int columnIndex = 0; columnIndex < ItemsInternal.Count; columnIndex++)
            {
                var item = ItemsInternal[columnIndex];
                if (item.IsVisible)
                {
                    VisibleColumnCount++;
                    item.EnsureWidth();
                    if (item.Width.IsStar)
                    {
                        VisibleStarColumnCount++;
                    }
                    VisibleEdgedColumnsWidth += item.ActualWidth;
                }
            }
        }

        internal int GetColumnDisplayIndex(int columnIndex)
        {
            if (columnIndex < 0 || columnIndex >= ItemsInternal.Count)
                return -1;
            var column = ItemsInternal[columnIndex];
            if (!column.IsVisible)
                return -1;
            return column.DisplayIndex;
        }

        internal DataGridColumn GetColumnAtDisplayIndex(int displayIndex)
        {
            if (displayIndex < 0 || displayIndex >= ItemsInternal.Count || displayIndex >= DisplayIndexMap.Count)
            {
                return null;
            }
            int columnIndex = DisplayIndexMap[displayIndex];
            return ItemsInternal[columnIndex];
        }

        internal int GetColumnCount(bool isVisible, bool isFrozen, int fromColumnIndex, int toColumnIndex)
        {
            int columnCount = 0;
            DataGridColumn dataGridColumn = ItemsInternal[fromColumnIndex];

            while (dataGridColumn != ItemsInternal[toColumnIndex])
            {
                dataGridColumn = GetNextColumn(dataGridColumn, isVisible, isFrozen, null /*isReadOnly*/);
                columnCount++;
            }
            return columnCount;
        }

        internal IEnumerable<DataGridColumn> GetDisplayedColumns()
        {
            foreach (int columnIndex in DisplayIndexMap)
            {
                yield return ItemsInternal[columnIndex];
            }
        }

        /// <summary>
        /// Returns an enumeration of all columns that meet the criteria of the filter predicate.
        /// </summary>
        /// <param name="filter">Criteria for inclusion.</param>
        /// <returns>Columns that meet the criteria, in ascending DisplayIndex order.</returns>
        internal IEnumerable<DataGridColumn> GetDisplayedColumns(Predicate<DataGridColumn> filter)
        {
            Debug.Assert(filter != null);
            Debug.Assert(ItemsInternal.Count == DisplayIndexMap.Count);
            foreach (int columnIndex in DisplayIndexMap)
            {
                DataGridColumn column = ItemsInternal[columnIndex];
                if (filter(column))
                {
                    yield return column;
                }
            }
        }

        /// <summary>
        /// Returns an enumeration of all columns that meet the criteria of the filter predicate.
        /// The columns are returned in the order specified by the reverse flag.
        /// </summary>
        /// <param name="reverse">Whether or not to return the columns in descending DisplayIndex order.</param>
        /// <param name="filter">Criteria for inclusion.</param>
        /// <returns>Columns that meet the criteria, in the order specified by the reverse flag.</returns>
        internal IEnumerable<DataGridColumn> GetDisplayedColumns(bool reverse, Predicate<DataGridColumn> filter)
        {
            return reverse ? GetDisplayedColumnsReverse(filter) : GetDisplayedColumns(filter);
        }

        /// <summary>
        /// Returns an enumeration of all columns that meet the criteria of the filter predicate.
        /// The columns are returned in descending DisplayIndex order.
        /// </summary>
        /// <param name="filter">Criteria for inclusion.</param>
        /// <returns>Columns that meet the criteria, in descending DisplayIndex order.</returns>
        internal IEnumerable<DataGridColumn> GetDisplayedColumnsReverse(Predicate<DataGridColumn> filter)
        {
            for (int displayIndex = DisplayIndexMap.Count - 1; displayIndex >= 0; displayIndex--)
            {
                DataGridColumn column = ItemsInternal[DisplayIndexMap[displayIndex]];
                if (filter(column))
                {
                    yield return column;
                }
            }
        }

        internal DataGridColumn GetFirstColumn(bool? isVisible, bool? isFrozen, bool? isReadOnly)
        {
            Debug.Assert(ItemsInternal.Count == DisplayIndexMap.Count);
            int index = 0;
            while (index < DisplayIndexMap.Count)
            {
                DataGridColumn dataGridColumn = GetColumnAtDisplayIndex(index);
                if ((isVisible == null || (dataGridColumn.IsVisible) == isVisible) &&
                    (isFrozen == null || dataGridColumn.IsFrozen == isFrozen) &&
                    (isReadOnly == null || dataGridColumn.IsReadOnly == isReadOnly))
                {
                    return dataGridColumn;
                }
                index++;
            }
            return null;
        }

        internal DataGridColumn GetLastColumn(bool? isVisible, bool? isFrozen, bool? isReadOnly)
        {
            Debug.Assert(ItemsInternal.Count == DisplayIndexMap.Count);
            int index = DisplayIndexMap.Count - 1;
            while (index >= 0)
            {
                DataGridColumn dataGridColumn = GetColumnAtDisplayIndex(index);
                if ((isVisible == null || (dataGridColumn.IsVisible) == isVisible) &&
                    (isFrozen == null || dataGridColumn.IsFrozen == isFrozen) &&
                    (isReadOnly == null || dataGridColumn.IsReadOnly == isReadOnly))
                {
                    return dataGridColumn;
                }
                index--;
            }
            return null;
        }

        internal DataGridColumn GetNextColumn(DataGridColumn dataGridColumnStart)
        {
            return GetNextColumn(dataGridColumnStart, null /*isVisible*/, null /*isFrozen*/, null /*isReadOnly*/);
        }

        internal DataGridColumn GetNextColumn(DataGridColumn dataGridColumnStart,
                                                  bool? isVisible, bool? isFrozen, bool? isReadOnly)
        {
            Debug.Assert(dataGridColumnStart != null);
            Debug.Assert(ItemsInternal.Contains(dataGridColumnStart));
            Debug.Assert(ItemsInternal.Count == DisplayIndexMap.Count);

            int index = dataGridColumnStart.DisplayIndexWithFiller + 1;
            while (index < DisplayIndexMap.Count)
            {
                DataGridColumn dataGridColumn = GetColumnAtDisplayIndex(index);

                if ((isVisible == null || (dataGridColumn.IsVisible) == isVisible) &&
                    (isFrozen == null || dataGridColumn.IsFrozen == isFrozen) &&
                    (isReadOnly == null || dataGridColumn.IsReadOnly == isReadOnly))
                {
                    return dataGridColumn;
                }
                index++;
            }
            return null;
        }

        internal DataGridColumn GetNextVisibleColumn(DataGridColumn dataGridColumnStart)
        {
            return GetNextColumn(dataGridColumnStart, true /*isVisible*/, null /*isFrozen*/, null /*isReadOnly*/);
        }

        internal DataGridColumn GetNextVisibleFrozenColumn(DataGridColumn dataGridColumnStart)
        {
            return GetNextColumn(dataGridColumnStart, true /*isVisible*/, true /*isFrozen*/, null /*isReadOnly*/);
        }

        internal DataGridColumn GetNextVisibleWritableColumn(DataGridColumn dataGridColumnStart)
        {
            return GetNextColumn(dataGridColumnStart, true /*isVisible*/, null /*isFrozen*/, false /*isReadOnly*/);
        }

        internal DataGridColumn GetPreviousColumn(DataGridColumn dataGridColumnStart,
                                                      bool? isVisible, bool? isFrozen, bool? isReadOnly)
        {
            int index = dataGridColumnStart.DisplayIndexWithFiller - 1;
            while (index >= 0)
            {
                DataGridColumn dataGridColumn = GetColumnAtDisplayIndex(index);
                if ((isVisible == null || (dataGridColumn.IsVisible) == isVisible) &&
                    (isFrozen == null || dataGridColumn.IsFrozen == isFrozen) &&
                    (isReadOnly == null || dataGridColumn.IsReadOnly == isReadOnly))
                {
                    return dataGridColumn;
                }
                index--;
            }
            return null;
        }

        internal DataGridColumn GetPreviousVisibleNonFillerColumn(DataGridColumn dataGridColumnStart)
        {
            DataGridColumn column = GetPreviousColumn(dataGridColumnStart, true /*isVisible*/, null /*isFrozen*/, null /*isReadOnly*/);
            return (column is DataGridFillerColumn) ? null : column;
        }

        internal DataGridColumn GetPreviousVisibleScrollingColumn(DataGridColumn dataGridColumnStart)
        {
            return GetPreviousColumn(dataGridColumnStart, true /*isVisible*/, false /*isFrozen*/, null /*isReadOnly*/);
        }

        internal DataGridColumn GetPreviousVisibleWritableColumn(DataGridColumn dataGridColumnStart)
        {
            return GetPreviousColumn(dataGridColumnStart, true /*isVisible*/, null /*isFrozen*/, false /*isReadOnly*/);
        }

        internal int GetVisibleColumnCount(int fromColumnIndex, int toColumnIndex)
        {
            int columnCount = 0;
            DataGridColumn dataGridColumn = ItemsInternal[fromColumnIndex];

            while (dataGridColumn != ItemsInternal[toColumnIndex])
            {
                dataGridColumn = GetNextVisibleColumn(dataGridColumn);
                columnCount++;
            }
            return columnCount;
        }

        internal IEnumerable<DataGridColumn> GetVisibleColumns()
        {
            Predicate<DataGridColumn> filter = column => column.IsVisible;
            return GetDisplayedColumns(filter);
        }

        internal IEnumerable<DataGridColumn> GetVisibleFrozenColumns()
        {
            Predicate<DataGridColumn> filter = column => column.IsVisible && column.IsFrozen;
            return GetDisplayedColumns(filter);
        }

        internal double GetVisibleFrozenEdgedColumnsWidth()
        {
            double visibleFrozenColumnsWidth = 0;
            for (int columnIndex = 0; columnIndex < ItemsInternal.Count; columnIndex++)
            {
                if (ItemsInternal[columnIndex].IsVisible && ItemsInternal[columnIndex].IsFrozen)
                {
                    visibleFrozenColumnsWidth += ItemsInternal[columnIndex].ActualWidth;
                }
            }
            return visibleFrozenColumnsWidth;
        }

        internal IEnumerable<DataGridColumn> GetVisibleScrollingColumns()
        {
            Predicate<DataGridColumn> filter = column => column.IsVisible && !column.IsFrozen;
            return GetDisplayedColumns(filter);
        }

        private void RemoveItemPrivate(int columnIndex, bool isSpacer)
        {
            try
            {
                _owningGrid.NoCurrentCellChangeCount++;

                if (_owningGrid.InDisplayIndexAdjustments)
                {
                    // We are within columns display indexes adjustments. We do not allow changing the column collection while adjusting display indexes.
                    throw DataGridError.DataGrid.CannotChangeColumnCollectionWhileAdjustingDisplayIndexes();
                }

                int columnIndexWithFiller = columnIndex;
                if (!isSpacer && RowGroupSpacerColumn.IsRepresented)
                {
                    columnIndexWithFiller++;
                }

                DataGridColumn dataGridColumn = ItemsInternal[columnIndexWithFiller];
                DataGridCellCoordinates newCurrentCellCoordinates = _owningGrid.OnRemovingColumn(dataGridColumn);
                ItemsInternal.RemoveAt(columnIndexWithFiller);
                if (dataGridColumn.IsVisible)
                {
                    VisibleEdgedColumnsWidth -= dataGridColumn.ActualWidth;
                }
                dataGridColumn.OwningGrid = null;
                dataGridColumn.RemoveEditingElement();

                // continue with the base remove
                _owningGrid.OnRemovedColumn_PreNotification(dataGridColumn);
                _owningGrid.OnColumnCollectionChanged_PreNotification(false /*columnsGrew*/);
                if (!isSpacer)
                {
                    base.RemoveItem(columnIndex);
                }
                _owningGrid.OnRemovedColumn_PostNotification(newCurrentCellCoordinates);
                _owningGrid.OnColumnCollectionChanged_PostNotification(false /*columnsGrew*/);
            }
            finally
            {
                _owningGrid.NoCurrentCellChangeCount--;
            }
        }

    }
}
