// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;

namespace Avalonia.Controls
{
    /// <summary>
    /// Represents an individual <see cref="T:Avalonia.Controls.DataGrid" /> cell.
    /// </summary>
    public class DataGridCell : ContentControl
    {
        private const string DATAGRIDCELL_elementRightGridLine = "PART_RightGridLine";

        private Rectangle _rightGridLine;
        private DataGridColumn _owningColumn;

        bool _isValid;

        public static readonly DirectProperty<DataGridCell, bool> IsValidProperty =
            AvaloniaProperty.RegisterDirect<DataGridCell, bool>(
                nameof(IsValid),
                o => o.IsValid);

        static DataGridCell()
        {
            PointerPressedEvent.AddClassHandler<DataGridCell>(
                x => x.DataGridCell_PointerPressed, handledEventsToo: true);
        }
        public DataGridCell()
        { }

        public bool IsValid
        {
            get { return _isValid; }
            internal set { SetAndRaise(IsValidProperty, ref _isValid, value); }
        }

        internal DataGridColumn OwningColumn
        {
            get => _owningColumn;
            set
            {
                if (_owningColumn != value)
                {
                    _owningColumn = value;
                    OnOwningColumnSet(value);
                }
            }
        }
        internal DataGridRow OwningRow
        {
            get;
            set;
        }

        internal DataGrid OwningGrid
        {
            get { return OwningRow?.OwningGrid ?? OwningColumn?.OwningGrid; }
        }

        internal double ActualRightGridLineWidth
        {
            get { return _rightGridLine?.Bounds.Width ?? 0; }
        }

        internal int ColumnIndex
        {
            get { return OwningColumn?.Index ?? -1; }
        }

        internal int RowIndex
        {
            get { return OwningRow?.Index ?? -1; }
        }

        internal bool IsCurrent
        {
            get
            {
                return OwningGrid.CurrentColumnIndex == OwningColumn.Index &&
                       OwningGrid.CurrentSlot == OwningRow.Slot;
            }
        }

        private bool IsEdited
        {
            get
            {
                return OwningGrid.EditingRow == OwningRow &&
                       OwningGrid.EditingColumnIndex == ColumnIndex;
            }
        }

        private bool IsMouseOver
        {
            get
            {
                return OwningRow != null && OwningRow.MouseOverColumnIndex == ColumnIndex;
            }
            set
            {
                if (value != IsMouseOver)
                {
                    if (value)
                    {
                        OwningRow.MouseOverColumnIndex = ColumnIndex;
                    }
                    else
                    {
                        OwningRow.MouseOverColumnIndex = null;
                    }
                }
            }
        }

        /// <summary>
        /// Builds the visual tree for the cell control when a new template is applied.
        /// </summary>
        protected override void OnTemplateApplied(TemplateAppliedEventArgs e)
        {
            base.OnTemplateApplied(e);

            UpdatePseudoClasses();
            _rightGridLine = e.NameScope.Find<Rectangle>(DATAGRIDCELL_elementRightGridLine);
            if (_rightGridLine != null && OwningColumn == null)
            {
                // Turn off the right GridLine for filler cells
                _rightGridLine.IsVisible = false;
            }
            else
            {
                EnsureGridLine(null);
            }

        }
        protected override void OnPointerEnter(PointerEventArgs e)
        {
            base.OnPointerEnter(e);

            if (OwningRow != null)
            {
                IsMouseOver = true;
            }
        }
        protected override void OnPointerLeave(PointerEventArgs e)
        {
            base.OnPointerLeave(e);

            if (OwningRow != null)
            {
                IsMouseOver = false;
            }
        }

        //TODO TabStop
        private void DataGridCell_PointerPressed(PointerPressedEventArgs e)
        {
            // OwningGrid is null for TopLeftHeaderCell and TopRightHeaderCell because they have no OwningRow
            if (OwningGrid != null)
            {
                OwningGrid.OnCellPointerPressed(new DataGridCellPointerPressedEventArgs(this, OwningRow, OwningColumn, e));
                if (e.MouseButton == MouseButton.Left)
                {
                    if (!e.Handled)
                    //if (!e.Handled && OwningGrid.IsTabStop)
                    {
                        OwningGrid.Focus();
                    }
                    if (OwningRow != null)
                    {
                        e.Handled = OwningGrid.UpdateStateOnMouseLeftButtonDown(e, ColumnIndex, OwningRow.Slot, !e.Handled);
                        OwningGrid.UpdatedStateOnMouseLeftButtonDown = true;
                    }
                }
            }
        }

        internal void UpdatePseudoClasses()
        {
            /*
            if (OwningGrid == null || OwningColumn == null || OwningRow == null || OwningRow.Visibility == Visibility.Collapsed || OwningRow.Slot == -1)
            {
                return;
            }

            // CommonStates
            if (IsMouseOver)
            {
                VisualStates.GoToState(this, animate, VisualStates.StateMouseOver, VisualStates.StateNormal);
            }
            else
            {
                VisualStates.GoToState(this, animate, VisualStates.StateNormal);
            }

            // SelectionStates
            if (OwningRow.IsSelected)
            {
                VisualStates.GoToState(this, animate, VisualStates.StateSelected, VisualStates.StateUnselected);
            }
            else
            {
                VisualStates.GoToState(this, animate, VisualStates.StateUnselected);
            }

            // FocusStates
            if (OwningGrid.ContainsFocus)
            {
                VisualStates.GoToState(this, animate, VisualStates.StateFocused, VisualStates.StateUnfocused);
            }
            else
            {
                VisualStates.GoToState(this, animate, VisualStates.StateUnfocused);
            }

            // CurrentStates
            if (IsCurrent)
            {
                VisualStates.GoToState(this, animate, VisualStates.StateCurrent, VisualStates.StateRegular);
            }
            else
            {
                VisualStates.GoToState(this, animate, VisualStates.StateRegular);
            }

            // Interaction states
            if (IsEdited)
            {
                VisualStates.GoToState(this, animate, VisualStates.StateEditing, VisualStates.StateDisplay);
            }
            else
            {
                VisualStates.GoToState(this, animate, VisualStates.StateDisplay);
            }

            // Validation states
            if (IsValid)
            {
                VisualStates.GoToState(this, animate, VisualStates.StateValid);
            }
            else
            {
                VisualStates.GoToState(this, animate, VisualStates.StateInvalid, VisualStates.StateValid);
            }
             */
        }

        // Makes sure the right gridline has the proper stroke and visibility. If lastVisibleColumn is specified, the 
        // right gridline will be collapsed if this cell belongs to the lastVisibileColumn and there is no filler column
        internal void EnsureGridLine(DataGridColumn lastVisibleColumn)
        {
            if (OwningGrid != null && _rightGridLine != null)
            {
                if (OwningGrid.VerticalGridLinesBrush != null && OwningGrid.VerticalGridLinesBrush != _rightGridLine.Fill)
                {
                    _rightGridLine.Fill = OwningGrid.VerticalGridLinesBrush;
                }

                bool newVisibility =
                    (OwningGrid.GridLinesVisibility == DataGridGridLinesVisibility.Vertical || OwningGrid.GridLinesVisibility == DataGridGridLinesVisibility.All)
                        && (OwningGrid.ColumnsInternal.FillerColumn.IsActive || OwningColumn != lastVisibleColumn);

                if (newVisibility != _rightGridLine.IsVisible)
                {
                    _rightGridLine.IsVisible = newVisibility;
                }
            }
        }

        private void OnOwningColumnSet(DataGridColumn column)
        {
            if (column == null)
            {
                Classes.Clear();
            }
            else
            {
                Classes.Replace(column.CellStyleClasses);
            }
        }

    }

    /*
    [TemplatePart(Name = DATAGRIDCELL_elementRightGridLine, Type = typeof(Rectangle))]
    [TemplateVisualState(Name = VisualStates.StateNormal, GroupName = VisualStates.GroupCommon)]
    [TemplateVisualState(Name = VisualStates.StateMouseOver, GroupName = VisualStates.GroupCommon)]
    [TemplateVisualState(Name = VisualStates.StateUnselected, GroupName = VisualStates.GroupSelection)]
    [TemplateVisualState(Name = VisualStates.StateSelected, GroupName = VisualStates.GroupSelection)]
    [TemplateVisualState(Name = VisualStates.StateUnfocused, GroupName = VisualStates.GroupFocus)]
    [TemplateVisualState(Name = VisualStates.StateFocused, GroupName = VisualStates.GroupFocus)]
    [TemplateVisualState(Name = VisualStates.StateRegular, GroupName = VisualStates.GroupCurrent)]
    [TemplateVisualState(Name = VisualStates.StateCurrent, GroupName = VisualStates.GroupCurrent)]
    [TemplateVisualState(Name = VisualStates.StateDisplay, GroupName = VisualStates.GroupInteraction)]
    [TemplateVisualState(Name = VisualStates.StateEditing, GroupName = VisualStates.GroupInteraction)]
    [TemplateVisualState(Name = VisualStates.StateInvalid, GroupName = VisualStates.GroupValidation)]
    [TemplateVisualState(Name = VisualStates.StateValid, GroupName = VisualStates.GroupValidation)]
    public sealed partial class DataGridCell : ContentControl
    */


    //TODO Styles
    /// <summary>
    /// Ensures that the correct Style is applied to this object.
    /// </summary>
    /// <param name="previousStyle">Caller's previous associated Style</param>
    /*internal void EnsureStyle(Style previousStyle)
    {
        if (Style != null
            && (OwningColumn == null || Style != OwningColumn.CellStyle)
            && (OwningGrid == null || Style != OwningGrid.CellStyle)
            && (Style != previousStyle))
        {
            return;
        }

        Style style = null;
        if (OwningColumn != null)
        {
            style = OwningColumn.CellStyle;
        }
        if (style == null && OwningGrid != null)
        {
            style = OwningGrid.CellStyle;
        }
        SetStyleWithType(style);
    } */


}
