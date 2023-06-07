// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using Avalonia.Controls.Utils;
using Avalonia.Interactivity;
using Avalonia.Utilities;

namespace Avalonia.Controls
{
    internal class DataGridFillerColumn : DataGridColumn
    {
        public DataGridFillerColumn(DataGrid owningGrid)
        {
            IsReadOnly = true;
            OwningGrid = owningGrid;
            MinWidth = 0;
            MaxWidth = int.MaxValue;
        }

        internal double FillerWidth
        {
            get;
            set;
        }

        // True if there is room for the filler column; otherwise, false
        internal bool IsActive
        {
            get
            {
                return FillerWidth > 0;
            }
        }

        // True if the FillerColumn's header cell is contained in the visual tree
        internal bool IsRepresented
        {
            get;
            set;
        } 

        internal override DataGridColumnHeader CreateHeader()
        {
            DataGridColumnHeader headerCell = base.CreateHeader();
            if (headerCell != null)
            {
                headerCell.IsEnabled = false;
            }
            return headerCell;
        }

        protected override Control GenerateElement(DataGridCell cell, object dataItem)
        {
            return null;
        }

        protected override Control GenerateEditingElement(DataGridCell cell, object dataItem, out ICellEditBinding editBinding)
        {
            editBinding = null;
            return null;
        } 

        protected override object PrepareCellForEdit(Control editingElement, RoutedEventArgs editingEventArgs)
        {
            return null;
        }
    }
}
