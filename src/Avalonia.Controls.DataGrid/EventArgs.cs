// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using Avalonia.Input;
using Avalonia.Interactivity;
using System;
using System.ComponentModel;
using System.Diagnostics;

namespace Avalonia.Controls
{
    /// <summary>
    /// Provides data for the <see cref="E:Avalonia.Controls.DataGrid.AutoGeneratingColumn" /> event. 
    /// </summary>
    public class DataGridAutoGeneratingColumnEventArgs : CancelEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:Avalonia.Controls.DataGridAutoGeneratingColumnEventArgs" /> class.
        /// </summary>
        /// <param name="propertyName">
        /// The name of the property bound to the generated column.
        /// </param>
        /// <param name="propertyType">
        /// The <see cref="T:System.Type" /> of the property bound to the generated column.
        /// </param>
        /// <param name="column">
        /// The generated column.
        /// </param>
        public DataGridAutoGeneratingColumnEventArgs(string propertyName, Type propertyType, DataGridColumn column)
        {
            Column = column;
            PropertyName = propertyName;
            PropertyType = propertyType;
        }

        /// <summary>
        /// Gets the generated column.
        /// </summary>
        public DataGridColumn Column
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the name of the property bound to the generated column.
        /// </summary>
        public string PropertyName
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the <see cref="T:System.Type" /> of the property bound to the generated column.
        /// </summary>
        public Type PropertyType
        {
            get;
            private set;
        }
    }

    /// <summary>
    /// Provides data for the <see cref="E:Avalonia.Controls.DataGrid.BeginningEdit" /> event.
    /// </summary>
    public class DataGridBeginningEditEventArgs : CancelEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the 
        /// <see cref="T:Avalonia.Controls.DataGridBeginningEditEventArgs" /> class.
        /// </summary>
        /// <param name="column">
        /// The column that contains the cell to be edited.
        /// </param>
        /// <param name="row">
        /// The row that contains the cell to be edited.
        /// </param>
        /// <param name="editingEventArgs">
        /// Information about the user gesture that caused the cell to enter edit mode.
        /// </param>
        public DataGridBeginningEditEventArgs(DataGridColumn column,
                                              DataGridRow row,
                                              RoutedEventArgs editingEventArgs)
        {
            this.Column = column;
            this.Row = row;
            this.EditingEventArgs = editingEventArgs;
        }

        /// <summary>
        /// Gets the column that contains the cell to be edited.
        /// </summary>
        public DataGridColumn Column
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets information about the user gesture that caused the cell to enter edit mode.
        /// </summary>
        public RoutedEventArgs EditingEventArgs
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the row that contains the cell to be edited.
        /// </summary>
        public DataGridRow Row
        {
            get;
            private set;
        }

    }

    /// <summary>
    /// Provides information just after a cell has exited editing mode.
    /// </summary>
    public class DataGridCellEditEndedEventArgs : EventArgs
    {
        /// <summary>
        /// Instantiates a new instance of this class.
        /// </summary>
        /// <param name="column">The column of the cell that has just exited edit mode.</param>
        /// <param name="row">The row container of the cell container that has just exited edit mode.</param>
        /// <param name="editAction">The editing action that has been taken.</param>
        public DataGridCellEditEndedEventArgs(DataGridColumn column, DataGridRow row, DataGridEditAction editAction)
        {
            Column = column;
            Row = row;
            EditAction = editAction;
        }

        /// <summary>
        /// The column of the cell that has just exited edit mode.
        /// </summary>
        public DataGridColumn Column
        {
            get;
            private set;
        }

        /// <summary>
        /// The edit action taken when leaving edit mode.
        /// </summary>
        public DataGridEditAction EditAction
        {
            get;
            private set;
        }

        /// <summary>
        /// The row container of the cell container that has just exited edit mode.
        /// </summary>
        public DataGridRow Row
        {
            get;
            private set;
        }

    }

    /// <summary>
    /// Provides information after the cell has been pressed.
    /// </summary>
    public class DataGridCellPointerPressedEventArgs : EventArgs
    {
        /// <summary>
        /// Instantiates a new instance of this class.
        /// </summary>
        /// <param name="cell">The cell that has been pressed.</param>
        /// <param name="row">The row container of the cell that has been pressed.</param>
        /// <param name="column">The column of the cell that has been pressed.</param>
        /// <param name="e">The pointer action that has been taken.</param>
        public DataGridCellPointerPressedEventArgs(DataGridCell cell, 
                                                   DataGridRow row,
                                                   DataGridColumn column,
                                                   PointerPressedEventArgs e)
        {
            Cell = cell;
            Row = row;
            Column = column;
            PointerPressedEventArgs = e;
        }

        /// <summary>
        /// The cell that has been pressed.
        /// </summary> 
        public DataGridCell Cell { get; }

        /// <summary>
        /// The row container of the cell that has been pressed.
        /// </summary> 
        public DataGridRow Row { get; }

        /// <summary>
        /// The column of the cell that has been pressed.
        /// </summary> 
        public DataGridColumn Column { get; }

        /// <summary>
        /// The pointer action that has been taken.
        /// </summary> 
        public PointerPressedEventArgs PointerPressedEventArgs { get; }
    }

    /// <summary>
    /// Provides information just before a cell exits editing mode.
    /// </summary>
    public class DataGridCellEditEndingEventArgs : CancelEventArgs
    {
        /// <summary>
        /// Instantiates a new instance of this class.
        /// </summary>
        /// <param name="column">The column of the cell that is about to exit edit mode.</param>
        /// <param name="row">The row container of the cell container that is about to exit edit mode.</param>
        /// <param name="editingElement">The editing element within the cell.</param>
        /// <param name="editAction">The editing action that will be taken.</param>
        public DataGridCellEditEndingEventArgs(DataGridColumn column,
                                               DataGridRow row,
                                               Control editingElement,
                                               DataGridEditAction editAction)
        {
            Column = column;
            Row = row;
            EditingElement = editingElement;
            EditAction = editAction;
        }

        /// <summary>
        /// The column of the cell that is about to exit edit mode.
        /// </summary>
        public DataGridColumn Column
        {
            get;
            private set;
        }

        /// <summary>
        /// The edit action to take when leaving edit mode.
        /// </summary>
        public DataGridEditAction EditAction
        {
            get;
            private set;
        }

        /// <summary>
        /// The editing element within the cell. 
        /// </summary>
        public Control EditingElement
        {
            get;
            private set;
        }

        /// <summary>
        /// The row container of the cell container that is about to exit edit mode.
        /// </summary>
        public DataGridRow Row
        {
            get;
            private set;
        }

    }

    internal class DataGridCellEventArgs : EventArgs
    {
        internal DataGridCellEventArgs(DataGridCell dataGridCell)
        {
            Debug.Assert(dataGridCell != null);
            this.Cell = dataGridCell;
        }

        internal DataGridCell Cell
        {
            get;
            private set;
        }
    }

    /// <summary>
    /// Provides data for <see cref="T:Avalonia.Controls.DataGrid" /> column-related events.
    /// </summary>
    public class DataGridColumnEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:Avalonia.Controls.DataGridColumnEventArgs" /> class.
        /// </summary>
        /// <param name="column">The column that the event occurs for.</param>
        public DataGridColumnEventArgs(DataGridColumn column)
        {
            Column = column ?? throw new ArgumentNullException(nameof(column));
        }

        /// <summary>
        /// Gets the column that the event occurs for.
        /// </summary>
        public DataGridColumn Column
        {
            get;
            private set;
        }
    }

    /// <summary>
    /// Provides data for the <see cref="E:Avalonia.Controls.DataGrid.ColumnReordering" /> event.
    /// </summary>
    public class DataGridColumnReorderingEventArgs : CancelEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:Avalonia.Controls.DataGridColumnReorderingEventArgs" /> class.
        /// </summary>
        /// <param name="dataGridColumn"></param>
        public DataGridColumnReorderingEventArgs(DataGridColumn dataGridColumn)
        {
            this.Column = dataGridColumn;
        }

        /// <summary>
        /// The column being moved.
        /// </summary>
        public DataGridColumn Column
        {
            get;
            private set;
        }

        /// <summary>
        /// The popup indicator displayed while dragging.  If null and Handled = true, then do not display a tooltip.
        /// </summary>
        public Control DragIndicator
        {
            get;
            set;
        }

        /// <summary>
        /// UIElement to display at the insertion position.  If null and Handled = true, then do not display an insertion indicator.
        /// </summary>
        public IControl DropLocationIndicator
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Provides data for <see cref="T:Avalonia.Controls.DataGrid" /> row-related events.
    /// </summary>
    public class DataGridRowEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:Avalonia.Controls.DataGridRowEventArgs" /> class.
        /// </summary>
        /// <param name="dataGridRow">The row that the event occurs for.</param>
        public DataGridRowEventArgs(DataGridRow dataGridRow)
        {
            this.Row = dataGridRow;
        }

        /// <summary>
        /// Gets the row that the event occurs for.
        /// </summary>
        public DataGridRow Row
        {
            get;
            private set;
        }
    }

    /// <summary>
    /// Provides information just before a row exits editing mode.
    /// </summary>
    public class DataGridRowEditEndingEventArgs : CancelEventArgs
    {
        /// <summary>
        /// Instantiates a new instance of this class.
        /// </summary>
        /// <param name="row">The row container of the cell container that is about to exit edit mode.</param>
        /// <param name="editAction">The editing action that will be taken.</param>
        public DataGridRowEditEndingEventArgs(DataGridRow row, DataGridEditAction editAction)
        {
            this.Row = row;
            this.EditAction = editAction;
        }

        /// <summary>
        /// The editing action that will be taken.
        /// </summary>
        public DataGridEditAction EditAction
        {
            get;
            private set;
        }

        /// <summary>
        /// The row container of the cell container that is about to exit edit mode.
        /// </summary>
        public DataGridRow Row
        {
            get;
            private set;
        }
    }

    /// <summary>
    /// Provides information just after a row has exited edit mode.
    /// </summary>
    public class DataGridRowEditEndedEventArgs : EventArgs
    {
        /// <summary>
        /// Instantiates a new instance of this class.
        /// </summary>
        /// <param name="row">The row container of the cell container that has just exited edit mode.</param>
        /// <param name="editAction">The editing action that has been taken.</param>
        public DataGridRowEditEndedEventArgs(DataGridRow row, DataGridEditAction editAction)
        {
            this.Row = row;
            this.EditAction = editAction;
        }

        /// <summary>
        /// The editing action that has been taken.
        /// </summary>
        public DataGridEditAction EditAction
        {
            get;
            private set;
        }

        /// <summary>
        /// The row container of the cell container that has just exited edit mode.
        /// </summary>
        public DataGridRow Row
        {
            get;
            private set;
        }
    }

    /// <summary>
    /// Provides data for the <see cref="E:Avalonia.Controls.DataGrid.PreparingCellForEdit" /> event.
    /// </summary>
    public class DataGridPreparingCellForEditEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:Avalonia.Controls.DataGridPreparingCellForEditEventArgs" /> class.
        /// </summary>
        /// <param name="column">The column that contains the cell to be edited.</param>
        /// <param name="row">The row that contains the cell to be edited.</param>
        /// <param name="editingEventArgs">Information about the user gesture that caused the cell to enter edit mode.</param>
        /// <param name="editingElement">The element that the column displays for a cell in editing mode.</param>
        public DataGridPreparingCellForEditEventArgs(DataGridColumn column,
                                                     DataGridRow row,
                                                     RoutedEventArgs editingEventArgs,
                                                     Control editingElement)
        {
            Column = column;
            Row = row;
            EditingEventArgs = editingEventArgs;
            EditingElement = editingElement;
        }

        /// <summary>
        /// Gets the column that contains the cell to be edited.
        /// </summary>
        public DataGridColumn Column
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the element that the column displays for a cell in editing mode.
        /// </summary>
        public Control EditingElement
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets information about the user gesture that caused the cell to enter edit mode.
        /// </summary>
        public RoutedEventArgs EditingEventArgs
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the row that contains the cell to be edited.
        /// </summary>
        public DataGridRow Row
        {
            get;
            private set;
        }

    }

    /// <summary>
    /// EventArgs used for the DataGrid's LoadingRowGroup and UnloadingRowGroup events
    /// </summary>
    public class DataGridRowGroupHeaderEventArgs : EventArgs
    {
        /// <summary>
        /// Constructs a DataGridRowGroupHeaderEventArgs instance
        /// </summary>
        /// <param name="rowGroupHeader"></param>
        public DataGridRowGroupHeaderEventArgs(DataGridRowGroupHeader rowGroupHeader)
        {
            RowGroupHeader = rowGroupHeader;
        }

        /// <summary>
        /// DataGridRowGroupHeader associated with this instance
        /// </summary>
        public DataGridRowGroupHeader RowGroupHeader
        {
            get;
            private set;
        }
    }

    /// <summary>
    /// Provides data for the <see cref="E:Avalonia.Controls.DataGrid.LoadingRowDetails" />, <see cref="E:Avalonia.Controls.DataGrid.UnloadingRowDetails" />, 
    /// and <see cref="E:Avalonia.Controls.DataGrid.RowDetailsVisibilityChanged" /> events.
    /// </summary>
    public class DataGridRowDetailsEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:Avalonia.Controls.DataGridRowDetailsEventArgs" /> class. 
        /// </summary>
        /// <param name="row">The row that the event occurs for.</param>
        /// <param name="detailsElement">The row details section as a framework element.</param>
        public DataGridRowDetailsEventArgs(DataGridRow row, IControl detailsElement)
        {
            Row = row;
            DetailsElement = detailsElement;
        }

        /// <summary>
        /// Gets the row details section as a framework element.
        /// </summary>
        public IControl DetailsElement
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the row that the event occurs for.
        /// </summary>
        public DataGridRow Row
        {
            get;
            private set;
        }
    }
}