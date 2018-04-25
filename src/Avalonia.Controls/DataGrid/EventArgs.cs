﻿// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using Avalonia.Interactivity;
using System;
using System.ComponentModel;
using System.Diagnostics;

namespace Avalonia.Controls
{
    /// <summary>
    /// Provides data for the <see cref="E:System.Windows.Controls.DataGrid.AutoGeneratingColumn" /> event. 
    /// </summary>
    public class DataGridAutoGeneratingColumnEventArgs : CancelEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Windows.Controls.DataGridAutoGeneratingColumnEventArgs" /> class.
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
    /// Provides data for the <see cref="E:System.Windows.Controls.DataGrid.BeginningEdit" /> event.
    /// </summary>
    public class DataGridBeginningEditEventArgs : CancelEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the 
        /// <see cref="T:System.Windows.Controls.DataGridBeginningEditEventArgs" /> class.
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

        #region Properties

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

        #endregion Properties
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
            this.Column = column;
            this.Row = row;
            this.EditAction = editAction;
        }

        #region Properties

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

        #endregion Properties
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

        #region Properties

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

        #endregion Properties
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
    /// Provides data for <see cref="T:System.Windows.Controls.DataGrid" /> column-related events.
    /// </summary>
    public class DataGridColumnEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Windows.Controls.DataGridColumnEventArgs" /> class.
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
    /// Provides data for the <see cref="E:System.Windows.Controls.DataGrid.ColumnReordering" /> event.
    /// </summary>
    public class DataGridColumnReorderingEventArgs : CancelEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Windows.Controls.DataGridColumnReorderingEventArgs" /> class.
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
        public Control DropLocationIndicator
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Provides data for <see cref="T:System.Windows.Controls.DataGrid" /> row-related events.
    /// </summary>
    public class DataGridRowEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Windows.Controls.DataGridRowEventArgs" /> class.
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
    /// <QualityBand>Preview</QualityBand>
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
    /// <QualityBand>Preview</QualityBand>
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
    /// Provides data for the <see cref="E:System.Windows.Controls.DataGrid.PreparingCellForEdit" /> event.
    /// </summary>
    public class DataGridPreparingCellForEditEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Windows.Controls.DataGridPreparingCellForEditEventArgs" /> class.
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

        #region Public Properties

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

        #endregion
    }
}
