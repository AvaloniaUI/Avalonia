// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using System;
using System.Collections.Specialized;

namespace Avalonia.Controls
{
    /// <summary>
    /// Represents a <see cref="T:System.Windows.Controls.DataGrid" /> column that hosts 
    /// <see cref="T:System.Windows.Controls.CheckBox" /> controls in its cells.
    /// </summary>
    public class DataGridCheckBoxColumn : DataGridBoundColumn
    {
        private CheckBox _currentCheckBox;
        private DataGrid _owningGrid;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Windows.Controls.DataGridCheckBoxColumn" /> class. 
        /// </summary>
        public DataGridCheckBoxColumn()
        {
            BindingTarget = CheckBox.IsCheckedProperty;
        }

        /// <summary>
        /// Defines the <see cref="IsThreeState"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsThreeStateProperty =
            CheckBox.IsThreeStateProperty.AddOwner<DataGridCheckBoxColumn>();

        /// <summary>
        /// Gets or sets a value that indicates whether the hosted <see cref="T:System.Windows.Controls.CheckBox" /> controls allow three states or two. 
        /// </summary>
        /// <returns>
        /// true if the hosted controls support three states; false if they support two states. The default is false. 
        /// </returns>
        public bool IsThreeState
        {
            get => GetValue(IsThreeStateProperty);
            set => SetValue(IsThreeStateProperty, value);
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == IsThreeStateProperty)
            {
                NotifyPropertyChanged(change.Property.Name);
            }
        }

        /// <summary>
        /// Causes the column cell being edited to revert to the specified value.
        /// </summary>
        /// <param name="editingElement">
        /// The element that the column displays for a cell in editing mode.
        /// </param>
        /// <param name="uneditedValue">
        /// The previous, unedited value in the cell being edited.
        /// </param>
        protected override void CancelCellEdit(Control editingElement, object uneditedValue)
        {
            if (editingElement is CheckBox editingCheckBox)
            {
                editingCheckBox.IsChecked = (bool?)uneditedValue;
            }
        }

        ///  <summary>
        ///  Gets a <see cref="T:System.Windows.Controls.CheckBox" /> control that is bound to the column's <see cref="P:System.Windows.Controls.DataGridBoundColumn.Binding" /> property value.
        ///  </summary>
        ///  <param name="cell">
        ///  The cell that will contain the generated element.
        ///  </param>
        ///  <param name="dataItem">
        ///  The data item represented by the row that contains the intended cell.
        /// </param>
        ///  <returns>
        ///  A new <see cref="T:System.Windows.Controls.CheckBox" /> control that is bound to the column's <see cref="P:System.Windows.Controls.DataGridBoundColumn.Binding" /> property value.
        ///  </returns>
        protected override Control GenerateEditingElementDirect(DataGridCell cell, object dataItem)
        {
            var checkBox = new CheckBox
            {
                Margin = new Thickness(0)
            };
            ConfigureCheckBox(checkBox);
            return checkBox;
        }

        /// <summary>                
        /// Gets a read-only <see cref="T:System.Windows.Controls.CheckBox" /> control that is bound to the column's <see cref="P:System.Windows.Controls.DataGridBoundColumn.Binding" /> property value.
        /// </summary>
        /// <param name="cell">
        /// The cell that will contain the generated element.
        /// </param>
        /// <param name="dataItem">
        /// The data item represented by the row that contains the intended cell.
        /// </param>
        /// <returns>
        /// A new, read-only <see cref="T:System.Windows.Controls.CheckBox" /> control that is bound to the column's <see cref="P:System.Windows.Controls.DataGridBoundColumn.Binding" /> property value.
        /// </returns>
        protected override Control GenerateElement(DataGridCell cell, object dataItem)
        {
            bool isEnabled = false;
            CheckBox checkBoxElement = new CheckBox();
            if (EnsureOwningGrid())
            {
                if (cell.RowIndex != -1 && cell.ColumnIndex != -1 &&
                    cell.OwningRow != null &&
                    cell.OwningRow.Slot == this.OwningGrid.CurrentSlot &&
                    cell.ColumnIndex == this.OwningGrid.CurrentColumnIndex)
                {
                    isEnabled = true;
                    if (_currentCheckBox != null)
                    {
                        _currentCheckBox.IsEnabled = false;
                    }
                    _currentCheckBox = checkBoxElement;
                }
            }
            checkBoxElement.IsEnabled = isEnabled;
            checkBoxElement.IsHitTestVisible = false;
            ConfigureCheckBox(checkBoxElement);
            if (Binding != null)
            {
                checkBoxElement.Bind(BindingTarget, Binding);
            }
            return checkBoxElement;
        }

        /// <summary>
        /// Called when a cell in the column enters editing mode.
        /// </summary>
        /// <param name="editingElement">
        /// The element that the column displays for a cell in editing mode.
        /// </param>
        /// <param name="editingEventArgs">
        /// Information about the user gesture that is causing a cell to enter editing mode.
        /// </param>
        /// <returns>
        /// The unedited value. 
        /// </returns>
        protected override object PrepareCellForEdit(Control editingElement, RoutedEventArgs editingEventArgs)
        {
            if (editingElement is CheckBox editingCheckBox)
            {
                void EditValue()
                {
                    // User clicked the checkbox itself or pressed space, let's toggle the IsChecked value
                    if (editingCheckBox.IsThreeState)
                    {
                        switch (editingCheckBox.IsChecked)
                        {
                            case false:
                                editingCheckBox.IsChecked = true;
                                break;
                            case true:
                                editingCheckBox.IsChecked = null;
                                break;
                            case null:
                                editingCheckBox.IsChecked = false;
                                break;
                        }
                    }
                    else
                    {
                        editingCheckBox.IsChecked = !editingCheckBox.IsChecked;
                    }
                }

                bool? uneditedValue = editingCheckBox.IsChecked;
                if (editingEventArgs is PointerPressedEventArgs args)
                {
                    void ProcessPointerArgs()
                    {
                        // Editing was triggered by a mouse click
                        Point position = args.GetPosition(editingCheckBox);
                        Rect rect = new Rect(0, 0, editingCheckBox.Bounds.Width, editingCheckBox.Bounds.Height);
                        if (rect.Contains(position))
                        {
                            EditValue();
                        }
                    }
                    
                    void OnLayoutUpdated(object sender, EventArgs e)
                    {
                        if (editingCheckBox.Bounds.Width != 0 || editingCheckBox.Bounds.Height != 0)
                        {
                            editingCheckBox.LayoutUpdated -= OnLayoutUpdated;
                            ProcessPointerArgs();
                        }
                    }

                    if (editingCheckBox.Bounds.Width == 0 && editingCheckBox.Bounds.Height == 0)
                    {
                        editingCheckBox.LayoutUpdated += OnLayoutUpdated;
                    }
                    else
                    {
                        ProcessPointerArgs();
                    }
                }

                return uneditedValue;
            }
            return false;
        }

        /// <summary>
        /// Called by the DataGrid control when this column asks for its elements to be
        /// updated, because its CheckBoxContent or IsThreeState property changed.
        /// </summary>
        protected internal override void RefreshCellContent(Control element, string propertyName)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }
            if (element is CheckBox checkBox)
            {
                DataGridHelper.SyncColumnProperty(this, checkBox, IsThreeStateProperty);
            }
            else
            {
                throw DataGridError.DataGrid.ValueIsNotAnInstanceOf("element", typeof(CheckBox));
            }
        }

        private void Columns_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems.Contains(this) && _owningGrid != null)
            {
                _owningGrid.Columns.CollectionChanged -= Columns_CollectionChanged;
                _owningGrid.CurrentCellChanged -= OwningGrid_CurrentCellChanged;
                _owningGrid.KeyDown -= OwningGrid_KeyDown;
                _owningGrid.LoadingRow -= OwningGrid_LoadingRow;
                _owningGrid = null;
            }
        }

        private void ConfigureCheckBox(CheckBox checkBox)
        {
            checkBox.HorizontalAlignment = HorizontalAlignment.Center;
            checkBox.VerticalAlignment = VerticalAlignment.Center;
            DataGridHelper.SyncColumnProperty(this, checkBox, IsThreeStateProperty);
        }

        private bool EnsureOwningGrid()
        {
            if (OwningGrid != null)
            {
                if (OwningGrid != _owningGrid)
                {
                    _owningGrid = OwningGrid;
                    _owningGrid.Columns.CollectionChanged += Columns_CollectionChanged;
                    _owningGrid.CurrentCellChanged += OwningGrid_CurrentCellChanged;
                    _owningGrid.KeyDown += OwningGrid_KeyDown;
                    _owningGrid.LoadingRow += OwningGrid_LoadingRow;
                }
                return true;
            }
            return false;
        }

        private void OwningGrid_CurrentCellChanged(object sender, EventArgs e)
        {
            if (_currentCheckBox != null)
            {
                _currentCheckBox.IsEnabled = false;
            }
            if (OwningGrid != null && OwningGrid.CurrentColumn == this
                && OwningGrid.IsSlotVisible(OwningGrid.CurrentSlot))
            {
                if (OwningGrid.DisplayData.GetDisplayedElement(OwningGrid.CurrentSlot) is DataGridRow row)
                {
                    CheckBox checkBox = GetCellContent(row) as CheckBox;
                    if (checkBox != null)
                    {
                        checkBox.IsEnabled = true;
                    }
                    _currentCheckBox = checkBox;
                }
            }
        }

        private void OwningGrid_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space && OwningGrid != null &&
                OwningGrid.CurrentColumn == this)
            {
                if (OwningGrid.DisplayData.GetDisplayedElement(OwningGrid.CurrentSlot) is DataGridRow row)
                {
                    CheckBox checkBox = GetCellContent(row) as CheckBox;
                    if (checkBox == _currentCheckBox)
                    {
                        OwningGrid.BeginEdit();
                    }
                }
            }
        }

        private void OwningGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            if (OwningGrid != null)
            {
                if (GetCellContent(e.Row) is CheckBox checkBox)
                {
                    if (OwningGrid.CurrentColumnIndex == Index && OwningGrid.CurrentSlot == e.Row.Slot)
                    {
                        if (_currentCheckBox != null)
                        {
                            _currentCheckBox.IsEnabled = false;
                        }
                        checkBox.IsEnabled = true;
                        _currentCheckBox = checkBox;
                    }
                    else
                    {
                        checkBox.IsEnabled = false;
                    }
                }
            }
        }

    }
}
