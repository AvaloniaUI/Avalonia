// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;

namespace Avalonia.Controls
{
    /// <summary>
    /// Represents an individual <see cref="T:System.Windows.Controls.DataGrid" /> column header.
    /// </summary>
    public class DataGridColumnHeader : ContentControl
    {
        private enum DragMode
        {
            None = 0,
            MouseDown = 1,
            Drag = 2,
            Resize = 3,
            Reorder = 4
        }

        private const int DATAGRIDCOLUMNHEADER_resizeRegionWidth = 5;
        private const double DATAGRIDCOLUMNHEADER_separatorThickness = 1;
        
        private bool _areHandlersSuspended;
        private static DragMode _dragMode;
        private static Point? _lastMousePositionHeaders;
        private static Cursor _originalCursor;
        private static double _originalHorizontalOffset;
        private static double _originalWidth;
        private bool _desiredSeparatorVisibility;
        private static Point? _dragStart;
        private static DataGridColumn _dragColumn;
        private static double _frozenColumnsWidth;

        public static readonly StyledProperty<IBrush> SeparatorBrushProperty =
            AvaloniaProperty.Register<DataGridColumnHeader, IBrush>(nameof(SeparatorBrush));

        public IBrush SeparatorBrush
        {
            get { return GetValue(SeparatorBrushProperty); }
            set { SetValue(SeparatorBrushProperty, value); }
        }

        public static readonly StyledProperty<bool> AreSeparatorsVisibleProperty =
            AvaloniaProperty.Register<DataGridColumnHeader, bool>(
                nameof(AreSeparatorsVisible),
                defaultValue: true);

        public bool AreSeparatorsVisible
        {
            get { return GetValue(AreSeparatorsVisibleProperty); }
            set { SetValue(AreSeparatorsVisibleProperty, value); }
        }

        static DataGridColumnHeader()
        {
            AreSeparatorsVisibleProperty.Changed.AddClassHandler<DataGridColumnHeader>(x => x.OnAreSeparatorsVisibleChanged);
            ContentProperty.Changed.AddClassHandler<DataGridColumnHeader>(x => x.OnContentChanged);
        }



        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Windows.Controls.Primitives.DataGridColumnHeader" /> class. 
        /// </summary>
        //TODO
        public DataGridColumnHeader()
        {
            Debug.WriteLine("DataGridColumnHeader Ctor");
        }
        /*public DataGridColumnHeader()
        {
            LostMouseCapture += new MouseEventHandler(DataGridColumnHeader_LostMouseCapture);
            MouseLeftButtonDown += new MouseButtonEventHandler(DataGridColumnHeader_MouseLeftButtonDown);
            MouseLeftButtonUp += new MouseButtonEventHandler(DataGridColumnHeader_MouseLeftButtonUp);
            MouseMove += new MouseEventHandler(DataGridColumnHeader_MouseMove);
            MouseEnter += new MouseEventHandler(DataGridColumnHeader_MouseEnter);
            MouseLeave += new MouseEventHandler(DataGridColumnHeader_MouseLeave);

            DefaultStyleKey = typeof(DataGridColumnHeader);
        }  */



        #region Protected Methods

        /// <summary>
        /// Builds the visual tree for the column header when a new template is applied. 
        /// </summary>
        /*public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            ApplyState(false);
        }  */
        /// <summary>
        /// Called when the value of the <see cref="P:System.Windows.Controls.ContentControl.Content" /> property changes.
        /// </summary>
        /// <param name="oldContent">The old value of the <see cref="P:System.Windows.Controls.ContentControl.Content" /> property.</param>
        /// <param name="newContent">The new value of the <see cref="P:System.Windows.Controls.ContentControl.Content" /> property.</param>
        /// <exception cref="T:System.NotSupportedException">
        /// <paramref name="newContent" /> is not a UIElement.
        /// </exception>
        /*protected override void OnContentChanged(object oldContent, object newContent)
        {
            if (newContent is UIElement)
            {
                throw DataGridError.DataGridColumnHeader.ContentDoesNotSupportUIElements();
            }
            base.OnContentChanged(oldContent, newContent);
        }  */


        #endregion Protected Methods
        private void OnAreSeparatorsVisibleChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (!_areHandlersSuspended)
            {
                _desiredSeparatorVisibility = (bool)e.NewValue;
                if (OwningGrid != null)
                {
                    UpdateSeparatorVisibility(OwningGrid.ColumnsInternal.LastVisibleColumn);
                }
                else
                {
                    UpdateSeparatorVisibility(null);
                }
            }
        }
        //TODO
        private void OnContentChanged(AvaloniaPropertyChangedEventArgs e)
        {
            Debug.WriteLine($"Changed: {e.NewValue.ToString()}");

        }

        #region Internal Properties

        internal DataGridColumn OwningColumn
        {
            get;
            set;
        } 
        internal DataGrid OwningGrid => OwningColumn?.OwningGrid;
        
        internal int ColumnIndex
        {
            get
            {
                if (OwningColumn == null)
                {
                    return -1;
                }
                return OwningColumn.Index;
            }
        } 

        internal ListSortDirection? CurrentSortingState
        {
            get;
            private set;
        } 

        #endregion Internal Properties

        #region Private Properties

        private bool IsMouseOver
        {
            get;
            set;
        } 

        private bool IsPressed
        {
            get;
            set;
        }

        #endregion Private Properties

        private void SetValueNoCallback<T>(AvaloniaProperty<T> property, T value, BindingPriority priority = BindingPriority.LocalValue)
        {
            _areHandlersSuspended = true;
            try
            {
                SetValue(property, value, priority);
            }
            finally
            {
                _areHandlersSuspended = false;
            }
        }

        #region Internal Methods

        //TODO
        internal void ApplyState()
        {
            /*
            // Common States
            if (IsPressed && DataGridColumnHeader._dragMode != DragMode.Resize)
            {
                VisualStates.GoToState(this, useTransitions, VisualStates.StatePressed, VisualStates.StateMouseOver, VisualStates.StateNormal);
            }
            else if (IsMouseOver && DataGridColumnHeader._dragMode != DragMode.Resize)
            {
                VisualStates.GoToState(this, useTransitions, VisualStates.StateMouseOver, VisualStates.StateNormal);
            }
            else
            {
                VisualStates.GoToState(this, useTransitions, VisualStates.StateNormal);
            }

            // Sort States
            CurrentSortingState = null;
            if (OwningGrid != null
                && OwningGrid.DataConnection != null
                && OwningGrid.DataConnection.AllowSort)
            {
                SortDescription? sort = OwningColumn.GetSortDescription();

                if (sort.HasValue)
                {
                    CurrentSortingState = sort.Value.Direction;
                    if (CurrentSortingState == ListSortDirection.Ascending)
                    {
                        VisualStates.GoToState(this, useTransitions, VisualStates.StateSortAscending, VisualStates.StateUnsorted);
                    }
                    if (CurrentSortingState == ListSortDirection.Descending)
                    {
                        VisualStates.GoToState(this, useTransitions, VisualStates.StateSortDescending, VisualStates.StateUnsorted);
                    }
                }
                else
                {
                    VisualStates.GoToState(this, useTransitions, VisualStates.StateUnsorted);
                }
            }
            */
        } 


        internal void UpdateSeparatorVisibility(DataGridColumn lastVisibleColumn)
        {
            bool newVisibility = _desiredSeparatorVisibility;

            // Collapse separator for the last column if there is no filler column
            if (OwningColumn != null &&
                OwningGrid != null &&
                _desiredSeparatorVisibility &&
                OwningColumn == lastVisibleColumn &&
                !OwningGrid.ColumnsInternal.FillerColumn.IsActive)
            {
                newVisibility = false;
            }

            // Update the public property if it has changed
            if (AreSeparatorsVisible != newVisibility)
            {
                SetValueNoCallback(AreSeparatorsVisibleProperty, newVisibility);
            }
        } 

        #endregion Internal Methods


        #region Private Methods

        private bool CanReorderColumn(DataGridColumn column)
        {
            return OwningGrid.CanUserReorderColumns 
                && !(column is DataGridFillerColumn)
                && (column.CanUserReorderInternal.HasValue && column.CanUserReorderInternal.Value || !column.CanUserReorderInternal.HasValue);
        } 

        /// <summary>
        /// Determines whether a column can be resized by dragging the border of its header.  If star sizing
        /// is being used, there are special conditions that can prevent a column from being resized:
        /// 1. The column is the last visible column.
        /// 2. All columns are constrained by either their maximum or minimum values.
        /// </summary>
        /// <param name="column">Column to check.</param>
        /// <returns>Whether or not the column can be resized by dragging its header.</returns>
        private static bool CanResizeColumn(DataGridColumn column)
        {
            if (column.OwningGrid != null && column.OwningGrid.ColumnsInternal != null && column.OwningGrid.UsesStarSizing &&
                (column.OwningGrid.ColumnsInternal.LastVisibleColumn == column || !DoubleUtil.AreClose(column.OwningGrid.ColumnsInternal.VisibleEdgedColumnsWidth, column.OwningGrid.CellsWidth)))
            {
                return false;
            }
            return column.ActualCanUserResize;
        }  



        private static bool TrySetResizeColumn(DataGridColumn column)
        {
            // If datagrid.CanUserResizeColumns == false, then the column can still override it
            if (CanResizeColumn(column))
            {
                _dragColumn = column;

                _dragMode = DragMode.Resize;

                return true;
            }
            return false;
        } 

        #endregion Private Methods


    }

    #region Style

    /// <summary>
    /// Ensures that the correct Style is applied to this object.
    /// </summary>
    /// <param name="previousStyle">Caller's previous associated Style</param>
    /*internal void EnsureStyle(Style previousStyle)
    {
        if (Style != null
            && (OwningColumn == null || Style != OwningColumn.HeaderStyle)
            && (OwningGrid == null || Style != OwningGrid.ColumnHeaderStyle)
            && (Style != previousStyle))
        {
            return;
        }

        Style style = null;
        if (OwningColumn != null)
        {
            style = OwningColumn.HeaderStyle;
        }
        if (style == null && OwningGrid != null)
        {
            style = OwningGrid.ColumnHeaderStyle;
        }
        SetStyleWithType(style);
    }  */

    #endregion

    #region Sort

    /*internal void InvokeProcessSort()
    {
        Debug.Assert(OwningGrid != null);
        if (OwningGrid.WaitForLostFocus(delegate { InvokeProcessSort(); }))
        {
            return;
        }
        if (OwningGrid.CommitEdit(DataGridEditingUnit.Row, true /*exitEditingMode))
        {
            Dispatcher.BeginInvoke(new Action(ProcessSort));
        }
    }  */

    /*internal void ProcessSort()
    {
        // if we can sort:
        //  - DataConnection.AllowSort is true, and
        //  - AllowUserToSortColumns and CanSort are true, and
        //  - OwningColumn is bound, and
        //  - SortDescriptionsCollection exists, and
        //  - the column's data type is comparable
        // then try to sort
        if (OwningColumn != null
            && OwningGrid != null
            && OwningGrid.EditingRow == null
            && OwningColumn != OwningGrid.ColumnsInternal.FillerColumn
            && OwningGrid.DataConnection.AllowSort
            && OwningGrid.CanUserSortColumns
            && OwningColumn.CanUserSort
            && OwningGrid.DataConnection.SortDescriptions != null)
        {
            DataGrid owningGrid = OwningGrid;
            ListSortDirection newSortDirection;
            SortDescription newSort;

            bool ctrl;
            bool shift;

            KeyboardHelper.GetMetaKeyState(out ctrl, out shift);

            SortDescription? sort = OwningColumn.GetSortDescription();
            ICollectionView collectionView = owningGrid.DataConnection.CollectionView;
            Debug.Assert(collectionView != null);
            using (collectionView.DeferRefresh())
            {

                // if shift is held down, we multi-sort, therefore if it isn't, we'll clear the sorts beforehand
                if (!shift || owningGrid.DataConnection.SortDescriptions.Count == 0)
                {
                    if (collectionView.CanGroup && collectionView.GroupDescriptions != null)
                    {
                        // Make sure we sort by the GroupDescriptions first
                        for (int i = 0; i < collectionView.GroupDescriptions.Count; i++)
                        {
                            PropertyGroupDescription groupDescription = collectionView.GroupDescriptions[i] as PropertyGroupDescription;
                            if (groupDescription != null && collectionView.SortDescriptions.Count <= i || collectionView.SortDescriptions[i].PropertyName != groupDescription.PropertyName)
                            {
                                collectionView.SortDescriptions.Insert(Math.Min(i, collectionView.SortDescriptions.Count), new SortDescription(groupDescription.PropertyName, ListSortDirection.Ascending));
                            }
                        }
                        while (collectionView.SortDescriptions.Count > collectionView.GroupDescriptions.Count)
                        {
                            collectionView.SortDescriptions.RemoveAt(collectionView.GroupDescriptions.Count);
                        }
                    }
                    else if (!shift)
                    {
                        owningGrid.DataConnection.SortDescriptions.Clear();
                    }
                }

                if (sort.HasValue)
                {
                    // swap direction
                    switch (sort.Value.Direction)
                    {
                        case ListSortDirection.Ascending:
                            newSortDirection = ListSortDirection.Descending;
                            break;
                        default:
                            newSortDirection = ListSortDirection.Ascending;
                            break;
                    }

                    newSort = new SortDescription(sort.Value.PropertyName, newSortDirection);

                    // changing direction should not affect sort order, so we replace this column's
                    // sort description instead of just adding it to the end of the collection
                    int oldIndex = owningGrid.DataConnection.SortDescriptions.IndexOf(sort.Value);
                    if (oldIndex >= 0)
                    {
                        owningGrid.DataConnection.SortDescriptions.Remove(sort.Value);
                        owningGrid.DataConnection.SortDescriptions.Insert(oldIndex, newSort);
                    }
                    else
                    {
                        owningGrid.DataConnection.SortDescriptions.Add(newSort);
                    }
                }
                else
                {
                    // start new sort
                    newSortDirection = ListSortDirection.Ascending;

                    string propertyName = OwningColumn.GetSortPropertyName();
                    // no-opt if we couldn't find a property to sort on
                    if (string.IsNullOrEmpty(propertyName))
                    {
                        return;
                    }

                    newSort = new SortDescription(propertyName, newSortDirection);

                    owningGrid.DataConnection.SortDescriptions.Add(newSort);
                }
            }

            // We've completed the sort, so send the Invoked event for the column header's automation peer
            if (AutomationPeer.ListenerExists(AutomationEvents.InvokePatternOnInvoked))
            {
                AutomationPeer peer = FrameworkElementAutomationPeer.FromElement(this);
                if (peer != null)
                {
                    peer.RaiseAutomationEvent(AutomationEvents.InvokePatternOnInvoked);
                }
            }

        }
    }  */

    #endregion

    #region DragDrop

    /*internal void OnMouseLeftButtonDown(ref bool handled, Point mousePosition)
    {
        IsPressed = true;

        if (OwningGrid != null && OwningGrid.ColumnHeaders != null)
        {
            CaptureMouse();

            _dragMode = DragMode.MouseDown;
            _frozenColumnsWidth = OwningGrid.ColumnsInternal.GetVisibleFrozenEdgedColumnsWidth();
            _lastMousePositionHeaders = Translate(OwningGrid.ColumnHeaders, mousePosition);

            double distanceFromLeft = mousePosition.X;
            double distanceFromRight = ActualWidth - distanceFromLeft;
            DataGridColumn currentColumn = OwningColumn;
            DataGridColumn previousColumn = null;
            if (!(OwningColumn is DataGridFillerColumn))
            {
                previousColumn = OwningGrid.ColumnsInternal.GetPreviousVisibleNonFillerColumn(currentColumn);
            }

            if (_dragMode == DragMode.MouseDown && _dragColumn == null && (distanceFromRight <= DATAGRIDCOLUMNHEADER_resizeRegionWidth))
            {
                handled = TrySetResizeColumn(currentColumn);
            }
            else if (_dragMode == DragMode.MouseDown && _dragColumn == null && distanceFromLeft <= DATAGRIDCOLUMNHEADER_resizeRegionWidth && previousColumn != null)
            {
                handled = TrySetResizeColumn(previousColumn);
            }

            if (_dragMode == DragMode.Resize && _dragColumn != null)
            {
                _dragStart = _lastMousePositionHeaders;
                _originalWidth = _dragColumn.ActualWidth;
                _originalHorizontalOffset = OwningGrid.HorizontalOffset;

                handled = true;
            }
        }
    }  */

    /*internal void OnMouseLeftButtonUp(ref bool handled, Point mousePosition, Point mousePositionHeaders)
    {
        IsPressed = false;

        if (OwningGrid != null && OwningGrid.ColumnHeaders != null)
        {
            if (_dragMode == DragMode.MouseDown)
            {
               OnMouseLeftButtonUp_Click(ref handled);
            }
            else if (_dragMode == DragMode.Reorder)
            {
                // Find header we're hovering over
                int targetIndex = GetReorderingTargetDisplayIndex(mousePositionHeaders);

                if (((!OwningColumn.IsFrozen && targetIndex >= OwningGrid.FrozenColumnCount)
                      || (OwningColumn.IsFrozen && targetIndex < OwningGrid.FrozenColumnCount)))
                {
                    OwningColumn.DisplayIndex = targetIndex;

                    DataGridColumnEventArgs ea = new DataGridColumnEventArgs(OwningColumn);
                    OwningGrid.OnColumnReordered(ea);
                }

                DragCompletedEventArgs dragCompletedEventArgs = new DragCompletedEventArgs(mousePosition.X - _dragStart.Value.X, mousePosition.Y - _dragStart.Value.Y, false);
                OwningGrid.OnColumnHeaderDragCompleted(dragCompletedEventArgs);
            }
            else if (_dragMode == DragMode.Drag)
            {
                DragCompletedEventArgs dragCompletedEventArgs = new DragCompletedEventArgs(0, 0, false);
                OwningGrid.OnColumnHeaderDragCompleted(dragCompletedEventArgs);
            }

            SetDragCursor(mousePosition);

            // Variables that track drag mode states get reset in DataGridColumnHeader_LostMouseCapture
            ReleaseMouseCapture();
            DataGridColumnHeader._dragMode = DragMode.None;
            handled = true;
        }
    }  */

    /*internal void OnMouseLeftButtonUp_Click(ref bool handled)
    {
        // completed a click without dragging, so we're sorting
        InvokeProcessSort();
        handled = true;
    }  */

    /*internal void OnMouseMove(ref bool handled, Point mousePosition, Point mousePositionHeaders)
    {
        if (handled || OwningGrid == null || OwningGrid.ColumnHeaders == null)
        {
            return;
        }

        Debug.Assert(OwningGrid.Parent is UIElement);

        double distanceFromLeft = mousePosition.X;
        double distanceFromRight = ActualWidth - distanceFromLeft;

        OnMouseMove_Resize(ref handled, mousePositionHeaders);

        OnMouseMove_Reorder(ref handled, mousePosition, mousePositionHeaders, distanceFromLeft, distanceFromRight);

        // if we still haven't done anything about moving the mouse while 
        // the button is down, we remember that we're dragging, but we don't 
        // claim to have actually handled the event
        if (_dragMode == DragMode.MouseDown)
        {
            _dragMode = DragMode.Drag;
        }

        if (_dragMode == DragMode.Drag)
        {
            DragDeltaEventArgs dragDeltaEventArgs = new DragDeltaEventArgs(mousePositionHeaders.X - _lastMousePositionHeaders.Value.X, mousePositionHeaders.Y - _lastMousePositionHeaders.Value.Y);
            OwningGrid.OnColumnHeaderDragDelta(dragDeltaEventArgs);
        }

        _lastMousePositionHeaders = mousePositionHeaders;

        SetDragCursor(mousePosition);
    }  */

    /*private void DataGridColumnHeader_LostMouseCapture(object sender, MouseEventArgs e)
    {
        OnLostMouseCapture();
    }  */

    /*private void DataGridColumnHeader_MouseEnter(object sender, MouseEventArgs e)
    {
        if (!IsEnabled)
        {
            return;
        }

        Point mousePosition = e.GetPosition(this);
        OnMouseEnter(mousePosition);
        ApplyState(true);
    }  */

    /*private void DataGridColumnHeader_MouseLeave(object sender, MouseEventArgs e)
    {
        if (!IsEnabled)
        {
            return;
        }

        OnMouseLeave();
        ApplyState(true);
    }  */

    /*private void DataGridColumnHeader_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (OwningColumn == null || e.Handled || !IsEnabled)
        {
            return;
        }

        Point mousePosition = e.GetPosition(this);
        bool handled = e.Handled;
        OnMouseLeftButtonDown(ref handled, mousePosition);
        e.Handled = handled;

        ApplyState(true);
    }  */

    /*private void DataGridColumnHeader_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (OwningColumn == null || e.Handled || !IsEnabled)
        {
            return;
        }

        Point mousePosition = e.GetPosition(this);
        Point mousePositionHeaders = e.GetPosition(OwningGrid.ColumnHeaders);
        bool handled = e.Handled;
        OnMouseLeftButtonUp(ref handled, mousePosition, mousePositionHeaders);
        e.Handled = handled;

        ApplyState(true);
    }  */

    /*private void DataGridColumnHeader_MouseMove(object sender, MouseEventArgs e)
    {
        if (OwningGrid == null || !IsEnabled)
        {
            return;
        }

        Point mousePosition = e.GetPosition(this);
        Point mousePositionHeaders = e.GetPosition(OwningGrid.ColumnHeaders);

        bool handled = false;
        OnMouseMove(ref handled, mousePosition, mousePositionHeaders);
    }  */

    /// <summary>
    /// Returns the column against whose top-left the reordering caret should be positioned
    /// </summary>
    /// <param name="mousePositionHeaders">Mouse position within the ColumnHeadersPresenter</param>
    /// <param name="scroll">Whether or not to scroll horizontally when a column is dragged out of bounds</param>
    /// <param name="scrollAmount">If scroll is true, returns the horizontal amount that was scrolled</param>
    /// <returns></returns>
    /*private DataGridColumn GetReorderingTargetColumn(Point mousePositionHeaders, bool scroll, out double scrollAmount)
    {
        scrollAmount = 0;
        double leftEdge = OwningGrid.ColumnsInternal.RowGroupSpacerColumn.IsRepresented ? OwningGrid.ColumnsInternal.RowGroupSpacerColumn.ActualWidth : 0;
        double rightEdge = OwningGrid.CellsWidth;
        if (OwningColumn.IsFrozen)
        {
            rightEdge = Math.Min(rightEdge, _frozenColumnsWidth);
        }
        else if (OwningGrid.FrozenColumnCount > 0)
        {
            leftEdge = _frozenColumnsWidth;
        }

        if (mousePositionHeaders.X < leftEdge)
        {
            if (scroll &&
                OwningGrid.HorizontalScrollBar != null &&
                OwningGrid.HorizontalScrollBar.Visibility == Visibility.Visible &&
                OwningGrid.HorizontalScrollBar.Value > 0)
            {
                double newVal = mousePositionHeaders.X - leftEdge;
                scrollAmount = Math.Min(newVal, OwningGrid.HorizontalScrollBar.Value);
                OwningGrid.UpdateHorizontalOffset(scrollAmount + OwningGrid.HorizontalScrollBar.Value);
            }
            mousePositionHeaders.X = leftEdge;
        }
        else if (mousePositionHeaders.X >= rightEdge)
        {
            if (scroll &&
                OwningGrid.HorizontalScrollBar != null &&
                OwningGrid.HorizontalScrollBar.Visibility == Visibility.Visible &&
                OwningGrid.HorizontalScrollBar.Value < OwningGrid.HorizontalScrollBar.Maximum)
            {
                double newVal = mousePositionHeaders.X - rightEdge;
                scrollAmount = Math.Min(newVal, OwningGrid.HorizontalScrollBar.Maximum - OwningGrid.HorizontalScrollBar.Value);
                OwningGrid.UpdateHorizontalOffset(scrollAmount + OwningGrid.HorizontalScrollBar.Value);
            }
            mousePositionHeaders.X = rightEdge - 1;
        }

        foreach (DataGridColumn column in OwningGrid.ColumnsInternal.GetDisplayedColumns())
        {
            Point mousePosition = OwningGrid.ColumnHeaders.Translate(column.HeaderCell, mousePositionHeaders);
            double columnMiddle = column.HeaderCell.ActualWidth / 2;
            if (mousePosition.X >= 0 && mousePosition.X <= columnMiddle)
            {
                return column;
            }
            else if (mousePosition.X > columnMiddle && mousePosition.X < column.HeaderCell.ActualWidth)
            {
                return OwningGrid.ColumnsInternal.GetNextVisibleColumn(column);
            }
        }

        return null;
    }  */

    /// <summary>
    /// Returns the display index to set the column to
    /// </summary>
    /// <param name="mousePositionHeaders">Mouse position relative to the column headers presenter</param>
    /// <returns></returns>
    /*private int GetReorderingTargetDisplayIndex(Point mousePositionHeaders)
    {
        double scrollAmount = 0;
        DataGridColumn targetColumn = GetReorderingTargetColumn(mousePositionHeaders, false /*scroll, out scrollAmount);
        if (targetColumn != null)
        {
            return targetColumn.DisplayIndex > OwningColumn.DisplayIndex ? targetColumn.DisplayIndex - 1 : targetColumn.DisplayIndex;
        }
        else
        {
            return OwningGrid.Columns.Count - 1;
        }
    }  */

    /// <summary>
    /// Returns true if the mouse is 
    /// - to the left of the element, or within the left half of the element
    /// and
    /// - within the vertical range of the element, or ignoreVertical == true
    /// </summary>
    /// <param name="mousePosition"></param>
    /// <param name="element"></param>
    /// <param name="ignoreVertical"></param>
    /// <returns></returns>
    /*private bool IsReorderTargeted(Point mousePosition, FrameworkElement element, bool ignoreVertical)
    {
        Point position = Translate(element, mousePosition);

        return (position.X < 0 || (position.X >= 0 && position.X <= element.ActualWidth / 2))
            && (ignoreVertical || (position.Y >= 0 && position.Y <= element.ActualHeight))
            ;
    }  */

    /// <summary>
    /// Resets the static DataGridColumnHeader properties when a header loses mouse capture
    /// </summary>
    /*private void OnLostMouseCapture()
    {
        // When we stop interacting with the column headers, we need to reset the drag mode
        // and close any popups if they are open.

        if (DataGridColumnHeader._dragColumn != null && DataGridColumnHeader._dragColumn.HeaderCell != null)
        {
            DataGridColumnHeader._dragColumn.HeaderCell.Cursor = DataGridColumnHeader._originalCursor;
        }
        _dragMode = DragMode.None;
        _dragColumn = null;
        _dragStart = null;
        _lastMousePositionHeaders = null;

        if (OwningGrid != null && OwningGrid.ColumnHeaders != null)
        {
            OwningGrid.ColumnHeaders.DragColumn = null;
            OwningGrid.ColumnHeaders.DragIndicator = null;
            OwningGrid.ColumnHeaders.DropLocationIndicator = null;
        }
    }  */

    /// <summary>
    /// Sets up the DataGridColumnHeader for the MouseEnter event
    /// </summary>
    /// <param name="mousePosition">mouse position relative to the DataGridColumnHeader</param>
    /*private void OnMouseEnter(Point mousePosition)
    {
        IsMouseOver = true;
        SetDragCursor(mousePosition);
    }  */

    /// <summary>
    /// Sets up the DataGridColumnHeader for the MouseLeave event
    /// </summary>
    /*private void OnMouseLeave()
    {
        IsMouseOver = false;
    }  */

    /*private void OnMouseMove_BeginReorder(Point mousePosition)
    {
        DataGridColumnHeader dragIndicator = new DataGridColumnHeader();
        dragIndicator.OwningColumn = OwningColumn;
        dragIndicator.IsEnabled = false;
        dragIndicator.Content = Content;
        dragIndicator.ContentTemplate = ContentTemplate;

        Control dropLocationIndicator = new ContentControl();
        dropLocationIndicator.SetStyleWithType(OwningGrid.DropLocationIndicatorStyle);

        if (OwningColumn.DragIndicatorStyle != null)
        {
            dragIndicator.SetStyleWithType(OwningColumn.DragIndicatorStyle);
        }
        else if (OwningGrid.DragIndicatorStyle != null)
        {
            dragIndicator.SetStyleWithType(OwningGrid.DragIndicatorStyle);
        }

        // If the user didn't style the dragIndicator's Width, default it to the column header's width
        if (double.IsNaN(dragIndicator.Width))
        {
            dragIndicator.Width = ActualWidth;
        }

        // If the user didn't style the dropLocationIndicator's Height, default to the column header's height
        if (double.IsNaN(dropLocationIndicator.Height))
        {
            dropLocationIndicator.Height = ActualHeight;
        }

        // pass the caret's data template to the user for modification
        DataGridColumnReorderingEventArgs columnReorderingEventArgs = new DataGridColumnReorderingEventArgs(OwningColumn)
        {
            DropLocationIndicator = dropLocationIndicator,
            DragIndicator = dragIndicator
        };
        OwningGrid.OnColumnReordering(columnReorderingEventArgs);
        if (columnReorderingEventArgs.Cancel)
        {
            return;
        }

        // The user didn't cancel, so prepare for the reorder
        _dragColumn = OwningColumn;
        _dragMode = DragMode.Reorder;
        _dragStart = mousePosition;

        // Display the reordering thumb
        OwningGrid.ColumnHeaders.DragColumn = OwningColumn;
        OwningGrid.ColumnHeaders.DragIndicator = columnReorderingEventArgs.DragIndicator;
        OwningGrid.ColumnHeaders.DropLocationIndicator = columnReorderingEventArgs.DropLocationIndicator;
    }  */

    /*private void OnMouseMove_Reorder(ref bool handled, Point mousePosition, Point mousePositionHeaders, double distanceFromLeft, double distanceFromRight)
    {
        if (handled)
        {
            return;
        }

        #region handle entry into reorder mode
        if (_dragMode == DragMode.MouseDown && _dragColumn == null && (distanceFromRight > DATAGRIDCOLUMNHEADER_resizeRegionWidth && distanceFromLeft > DATAGRIDCOLUMNHEADER_resizeRegionWidth))
        {
            DragStartedEventArgs dragStartedEventArgs = new DragStartedEventArgs(mousePositionHeaders.X - _lastMousePositionHeaders.Value.X, mousePositionHeaders.Y - _lastMousePositionHeaders.Value.Y);
            OwningGrid.OnColumnHeaderDragStarted(dragStartedEventArgs);

            handled = CanReorderColumn(OwningColumn);

            if (handled)
            {
                OnMouseMove_BeginReorder(mousePosition);
            }
        }
        #endregion

        #region handle reorder mode (eg, positioning of the popup)
        if (_dragMode == DragMode.Reorder && OwningGrid.ColumnHeaders.DragIndicator != null)
        {
            DragDeltaEventArgs dragDeltaEventArgs = new DragDeltaEventArgs(mousePositionHeaders.X - _lastMousePositionHeaders.Value.X, mousePositionHeaders.Y - _lastMousePositionHeaders.Value.Y);
            OwningGrid.OnColumnHeaderDragDelta(dragDeltaEventArgs);

            // Find header we're hovering over
            double scrollAmount = 0;
            DataGridColumn targetColumn = GetReorderingTargetColumn(mousePositionHeaders, !OwningColumn.IsFrozen /*scroll, out scrollAmount);

            OwningGrid.ColumnHeaders.DragIndicatorOffset = mousePosition.X - _dragStart.Value.X + scrollAmount;
            OwningGrid.ColumnHeaders.InvalidateArrange();

            if (OwningGrid.ColumnHeaders.DropLocationIndicator != null)
            {
                Point targetPosition = new Point(0, 0);
                if (targetColumn == null || targetColumn == OwningGrid.ColumnsInternal.FillerColumn || targetColumn.IsFrozen != OwningColumn.IsFrozen)
                {
                    targetColumn = OwningGrid.ColumnsInternal.GetLastColumn(true /*isVisible, OwningColumn.IsFrozen /*isFrozen, null /*isReadOnly);
                    targetPosition = targetColumn.HeaderCell.Translate(OwningGrid.ColumnHeaders, targetPosition);
                    targetPosition.X += targetColumn.ActualWidth;
                }
                else
                {
                    targetPosition = targetColumn.HeaderCell.Translate(OwningGrid.ColumnHeaders, targetPosition);
                }
                OwningGrid.ColumnHeaders.DropLocationIndicatorOffset = targetPosition.X - scrollAmount;
            }

            handled = true;
        }
        #endregion
    }  */

    /*private void OnMouseMove_Resize(ref bool handled, Point mousePositionHeaders)
    {
        if (handled)
        {
            return;
        }

        if (_dragMode == DragMode.Resize && _dragColumn != null && _dragStart.HasValue)
        {
            // resize column

            double mouseDelta = mousePositionHeaders.X - _dragStart.Value.X;
            double desiredWidth = _originalWidth + mouseDelta;

            desiredWidth = Math.Max(_dragColumn.ActualMinWidth, Math.Min(_dragColumn.ActualMaxWidth, desiredWidth));
            _dragColumn.Resize(_dragColumn.Width.Value, _dragColumn.Width.UnitType, _dragColumn.Width.DesiredValue, desiredWidth, true);

            OwningGrid.UpdateHorizontalOffset(_originalHorizontalOffset);

            handled = true;
        }
    }  */

    /*private void SetDragCursor(Point mousePosition)
        {
            if (_dragMode != DragMode.None || OwningGrid == null || OwningColumn == null)
            {
                return;
            }

            // set mouse if we can resize column

            double distanceFromLeft = mousePosition.X;
            double distanceFromRight = ActualWidth - distanceFromLeft;
            DataGridColumn currentColumn = OwningColumn;
            DataGridColumn previousColumn = null;

            if (!(OwningColumn is DataGridFillerColumn))
            {
                previousColumn = OwningGrid.ColumnsInternal.GetPreviousVisibleNonFillerColumn(currentColumn);
            }

            if ((distanceFromRight <= DATAGRIDCOLUMNHEADER_resizeRegionWidth && currentColumn != null && CanResizeColumn(currentColumn)) ||
                (distanceFromLeft <= DATAGRIDCOLUMNHEADER_resizeRegionWidth && previousColumn != null && CanResizeColumn(previousColumn)))
            {
                if (Cursor != Cursors.SizeWE)
                {
                    DataGridColumnHeader._originalCursor = Cursor;
                    Cursor = Cursors.SizeWE;
                }
            }
            else
            {
                Cursor = DataGridColumnHeader._originalCursor;
            }
        } */



    #endregion

    /*
    
    /// <QualityBand>Mature</QualityBand>
    [TemplateVisualState(Name = VisualStates.StateNormal, GroupName = VisualStates.GroupCommon)]
    [TemplateVisualState(Name = VisualStates.StateMouseOver, GroupName = VisualStates.GroupCommon)]
    [TemplateVisualState(Name = VisualStates.StatePressed, GroupName = VisualStates.GroupCommon)]
    [TemplateVisualState(Name = VisualStates.StateUnsorted, GroupName = VisualStates.GroupSort)]
    [TemplateVisualState(Name = VisualStates.StateSortAscending, GroupName = VisualStates.GroupSort)]
    [TemplateVisualState(Name = VisualStates.StateSortDescending, GroupName = VisualStates.GroupSort)]
    public partial class DataGridColumnHeader : ContentControl
    */

    /// <summary>
    /// Creates AutomationPeer (<see cref="UIElement.OnCreateAutomationPeer"/>)
    /// </summary>
    /*protected override AutomationPeer OnCreateAutomationPeer()
    {
        if (OwningGrid != null && OwningColumn != OwningGrid.ColumnsInternal.FillerColumn)
        {
            return new DataGridColumnHeaderAutomationPeer(this);
        }
        return base.OnCreateAutomationPeer();
    }  */

}
