// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Controls.Templates;
using Avalonia.Controls.Utils;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace Avalonia.Controls
{
    public abstract class DataGridColumn : AvaloniaObject
    {
        internal const int DATAGRIDCOLUMN_maximumWidth = 65536;
        private const bool DATAGRIDCOLUMN_defaultIsReadOnly = false;
        private bool? _isReadOnly;
        private double? _maxWidth;
        private double? _minWidth;
        private bool _settingWidthInternally;
        private int _displayIndexWithFiller;
        private object _header;
        private IDataTemplate _headerTemplate;
        private DataGridColumnHeader _headerCell;
        private Control _editingElement;
        private ICellEditBinding _editBinding;
        private IBinding _clipboardContentBinding;
        private ControlTheme _cellTheme;
        private Classes _cellStyleClasses;
        private bool _setWidthInternalNoCallback;

        /// <summary>
        /// Occurs when the pointer is pressed over the column's header
        /// </summary>
        public event EventHandler<PointerPressedEventArgs> HeaderPointerPressed;
        /// <summary>
        /// Occurs when the pointer is released over the column's header
        /// </summary>
        public event EventHandler<PointerReleasedEventArgs> HeaderPointerReleased;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Avalonia.Controls.DataGridColumn" /> class.
        /// </summary>
        protected internal DataGridColumn()
        {
            _displayIndexWithFiller = -1;
            IsInitialDesiredWidthDetermined = false;
            InheritsWidth = true;
        }

        /// <summary>
        /// Gets the <see cref="T:Avalonia.Controls.DataGrid"/> control that contains this column.
        /// </summary>
        protected internal DataGrid OwningGrid
        {
            get;
            internal set;
        }

        internal int Index
        {
            get;
            set;
        }

        internal bool? CanUserReorderInternal
        {
            get;
            set;
        }

        internal bool? CanUserResizeInternal
        {
            get;
            set;
        }

        internal bool? CanUserSortInternal
        {
            get;
            set;
        }

        internal bool ActualCanUserResize
        {
            get
            {
                if (OwningGrid == null || OwningGrid.CanUserResizeColumns == false || this is DataGridFillerColumn)
                {
                    return false;
                }
                return CanUserResizeInternal ?? true;
            }
        }

        // MaxWidth from local setting or DataGrid setting
        internal double ActualMaxWidth
        {
            get
            {
                return _maxWidth ?? OwningGrid?.MaxColumnWidth ?? double.PositiveInfinity;
            }
        }

        // MinWidth from local setting or DataGrid setting
        internal double ActualMinWidth
        {
            get
            {
                double minWidth = _minWidth ?? OwningGrid?.MinColumnWidth ?? 0;
                if (Width.IsStar)
                {
                    return Math.Max(DataGrid.DATAGRID_minimumStarColumnWidth, minWidth);
                }
                return minWidth;
            }
        }

        internal bool DisplayIndexHasChanged
        {
            get;
            set;
        }

        internal int DisplayIndexWithFiller
        {
            get { return _displayIndexWithFiller; }
            set { _displayIndexWithFiller = value; }
        }

        internal bool HasHeaderCell
        {
            get
            {
                return _headerCell != null;
            }
        }

        internal DataGridColumnHeader HeaderCell
        {
            get
            {
                if (_headerCell == null)
                {
                    _headerCell = CreateHeader();
                }
                return _headerCell;
            }
        }

        /// <summary>
        /// Tracks whether or not this column inherits its Width value from the DataGrid.
        /// </summary>
        internal bool InheritsWidth
        {
            get;
            private set;
        }

        /// <summary>
        /// When a column is initially added, we won't know its initial desired value
        /// until all rows have been measured.  We use this variable to track whether or
        /// not the column has been fully measured.
        /// </summary>
        internal bool IsInitialDesiredWidthDetermined
        {
            get;
            set;
        }

        internal double LayoutRoundedWidth
        {
            get;
            private set;
        }

        internal ICellEditBinding CellEditBinding
        {
            get => _editBinding;
        }


        /// <summary>
        /// Defines the <see cref="IsVisible"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsVisibleProperty =
             Control.IsVisibleProperty.AddOwner<DataGridColumn>();

        /// <summary>
        /// Determines whether or not this column is visible.
        /// </summary>
        public bool IsVisible
        {
            get => GetValue(IsVisibleProperty);
            set => SetValue(IsVisibleProperty, value);
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == IsVisibleProperty)
            {
                OwningGrid?.OnColumnVisibleStateChanging(this);
                var isVisible = change.GetNewValue<bool>();

                if (_headerCell != null)
                {
                    _headerCell.IsVisible = isVisible;
                }

                OwningGrid?.OnColumnVisibleStateChanged(this);
                NotifyPropertyChanged(change.Property.Name);
            }
            else if (change.Property == WidthProperty)
            {
                if (!_settingWidthInternally)
                {
                    InheritsWidth = false;
                }
                if (_setWidthInternalNoCallback == false)
                {
                    var grid = OwningGrid;
                    var width = (change as AvaloniaPropertyChangedEventArgs<DataGridLength>).NewValue.Value;
                    if (grid != null)
                    {
                        var oldWidth = (change as AvaloniaPropertyChangedEventArgs<DataGridLength>).OldValue.Value;
                        if (width.IsStar != oldWidth.IsStar)
                        {
                            SetWidthInternalNoCallback(width);
                            IsInitialDesiredWidthDetermined = false;
                            grid.OnColumnWidthChanged(this);
                        }
                        else
                        {
                            Resize(oldWidth, width, false);
                        }
                    }
                    else
                    {
                        SetWidthInternalNoCallback(width);
                    }
                }
            }
        }


        /// <summary>
        /// Actual visible width after Width, MinWidth, and MaxWidth setting at the Column level and DataGrid level
        /// have been taken into account
        /// </summary>
        public double ActualWidth
        {
            get
            {
                if (OwningGrid == null || double.IsNaN(Width.DisplayValue))
                {
                    return ActualMinWidth;
                }
                return Width.DisplayValue;
            }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether the user can change the column display position by
        /// dragging the column header.
        /// </summary>
        /// <returns>
        /// true if the user can drag the column header to a new position; otherwise, false. The default is the current <see cref="P:Avalonia.Controls.DataGrid.CanUserReorderColumns" /> property value.
        /// </returns>
        public bool CanUserReorder
        {
            get
            {
                return
                    CanUserReorderInternal ??
                        OwningGrid?.CanUserReorderColumns ??
                        DataGrid.DATAGRID_defaultCanUserResizeColumns;
            }
            set
            {
                CanUserReorderInternal = value;
            }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether the user can adjust the column width using the mouse.
        /// </summary>
        /// <returns>
        /// true if the user can resize the column; false if the user cannot resize the column. The default is the current <see cref="P:Avalonia.Controls.DataGrid.CanUserResizeColumns" /> property value.
        /// </returns>
        public bool CanUserResize
        {
            get
            {
                return
                    CanUserResizeInternal ??
                    OwningGrid?.CanUserResizeColumns ??
                    DataGrid.DATAGRID_defaultCanUserResizeColumns;
            }
            set
            {
                CanUserResizeInternal = value;
                OwningGrid?.OnColumnCanUserResizeChanged(this);
            }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether the user can sort the column by clicking the column header.
        /// </summary>
        /// <returns>
        /// true if the user can sort the column; false if the user cannot sort the column. The default is the current <see cref="P:Avalonia.Controls.DataGrid.CanUserSortColumns" /> property value.
        /// </returns>
        public bool CanUserSort
        {
            get
            {
                if (CanUserSortInternal.HasValue)
                {
                    return CanUserSortInternal.Value;
                }
                else if (OwningGrid != null)
                {
                    string propertyPath = GetSortPropertyName();
                    Type propertyType = OwningGrid.DataConnection.DataType.GetNestedPropertyType(propertyPath);

                    // if the type is nullable, then we will compare the non-nullable type
                    if (TypeHelper.IsNullableType(propertyType))
                    {
                        propertyType = TypeHelper.GetNonNullableType(propertyType);
                    }

                    // return whether or not the property type can be compared
                    return typeof(IComparable).IsAssignableFrom(propertyType) ? true : false;
                }
                else
                {
                    return DataGrid.DATAGRID_defaultCanUserSortColumns;
                }
            }
            set
            {
                CanUserSortInternal = value;
            }
        }

        /// <summary>
        /// Gets or sets the display position of the column relative to the other columns in the <see cref="T:Avalonia.Controls.DataGrid" />.
        /// </summary>
        /// <returns>
        /// The zero-based position of the column as it is displayed in the associated <see cref="T:Avalonia.Controls.DataGrid" />. The default is the index of the corresponding <see cref="P:System.Collections.ObjectModel.Collection`1.Item(System.Int32)" /> in the <see cref="P:Avalonia.Controls.DataGrid.Columns" /> collection.
        /// </returns>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// When setting this property, the specified value is less than -1 or equal to <see cref="F:System.Int32.MaxValue" />.
        ///
        /// -or-
        ///
        /// When setting this property on a column in a <see cref="T:Avalonia.Controls.DataGrid" />, the specified value is less than zero or greater than or equal to the number of columns in the <see cref="T:Avalonia.Controls.DataGrid" />.
        /// </exception>
        /// <exception cref="T:System.InvalidOperationException">
        /// When setting this property, the <see cref="T:Avalonia.Controls.DataGrid" /> is already making <see cref="P:Avalonia.Controls.DataGridColumn.DisplayIndex" /> adjustments. For example, this exception is thrown when you attempt to set <see cref="P:Avalonia.Controls.DataGridColumn.DisplayIndex" /> in a <see cref="E:Avalonia.Controls.DataGrid.ColumnDisplayIndexChanged" /> event handler.
        ///
        /// -or-
        ///
        /// When setting this property, the specified value would result in a frozen column being displayed in the range of unfrozen columns, or an unfrozen column being displayed in the range of frozen columns.
        /// </exception>
        public int DisplayIndex
        {
            get
            {
                if (OwningGrid != null && OwningGrid.ColumnsInternal.RowGroupSpacerColumn.IsRepresented)
                {
                    return _displayIndexWithFiller - 1;
                }
                else
                {
                    return _displayIndexWithFiller;
                }
            }
            set
            {
                if (value == Int32.MaxValue)
                {
                    throw DataGridError.DataGrid.ValueMustBeLessThan(nameof(value), nameof(DisplayIndex), Int32.MaxValue);
                }
                if (OwningGrid != null)
                {
                    if (OwningGrid.ColumnsInternal.RowGroupSpacerColumn.IsRepresented)
                    {
                        value++;
                    }
                    if (_displayIndexWithFiller != value)
                    {
                        if (value < 0 || value >= OwningGrid.ColumnsItemsInternal.Count)
                        {
                            throw DataGridError.DataGrid.ValueMustBeBetween(nameof(value), nameof(DisplayIndex), 0, true, OwningGrid.Columns.Count, false);
                        }
                        // Will throw an error if a visible frozen column is placed inside a non-frozen area or vice-versa.
                        OwningGrid.OnColumnDisplayIndexChanging(this, value);
                        _displayIndexWithFiller = value;
                        try
                        {
                            OwningGrid.InDisplayIndexAdjustments = true;
                            OwningGrid.OnColumnDisplayIndexChanged(this);
                            OwningGrid.OnColumnDisplayIndexChanged_PostNotification();
                        }
                        finally
                        {
                            OwningGrid.InDisplayIndexAdjustments = false;
                        }
                    }
                }
                else
                {
                    if (value < -1)
                    {
                        throw DataGridError.DataGrid.ValueMustBeGreaterThanOrEqualTo(nameof(value), nameof(DisplayIndex), -1);
                    }
                    _displayIndexWithFiller = value;
                }
            }
        }

        public Classes CellStyleClasses => _cellStyleClasses ??= new();

        /// <summary>
        ///    Backing field for CellTheme property.
        /// </summary>
        public static readonly DirectProperty<DataGridColumn, ControlTheme> CellThemeProperty =
            AvaloniaProperty.RegisterDirect<DataGridColumn, ControlTheme>(
                nameof(CellTheme),
                o => o.CellTheme,
                (o, v) => o.CellTheme = v);

        /// <summary>
        ///    Gets or sets the <see cref="DataGridColumnHeader"/> cell theme.
        /// </summary>
        public ControlTheme CellTheme
        {
            get { return _cellTheme; }
            set { SetAndRaise(CellThemeProperty, ref _cellTheme, value); }
        }

        /// <summary>
        ///    Backing field for Header property
        /// </summary>
        public static readonly DirectProperty<DataGridColumn, object> HeaderProperty =
            AvaloniaProperty.RegisterDirect<DataGridColumn, object>(
                nameof(Header),
                o => o.Header,
                (o, v) => o.Header = v);

        /// <summary>
        ///    Gets or sets the <see cref="DataGridColumnHeader"/> content
        /// </summary>
        public object Header
        {
            get { return _header; }
            set { SetAndRaise(HeaderProperty, ref _header, value); }
        }

        /// <summary>
        ///    Backing field for Header property
        /// </summary>
        public static readonly DirectProperty<DataGridColumn, IDataTemplate> HeaderTemplateProperty =
            AvaloniaProperty.RegisterDirect<DataGridColumn, IDataTemplate>(
                nameof(HeaderTemplate),
                o => o.HeaderTemplate,
                (o, v) => o.HeaderTemplate = v);

        /// <summary>
        ///  Gets or sets an <see cref="IDataTemplate"/> for the <see cref="Header"/>
        /// </summary>
        public IDataTemplate HeaderTemplate
        {
            get { return _headerTemplate; }
            set { SetAndRaise(HeaderTemplateProperty, ref _headerTemplate, value); }
        }

        public bool IsAutoGenerated
        {
            get;
            internal set;
        }

        public bool IsFrozen
        {
            get;
            internal set;
        }

        public virtual bool IsReadOnly
        {
            get
            {
                if (OwningGrid == null)
                {
                    return _isReadOnly ?? DATAGRIDCOLUMN_defaultIsReadOnly;
                }
                if (_isReadOnly != null)
                {
                    return _isReadOnly.Value || OwningGrid.IsReadOnly;
                }
                return OwningGrid.GetColumnReadOnlyState(this, DATAGRIDCOLUMN_defaultIsReadOnly);
            }
            set
            {
                if (value != _isReadOnly)
                {
                    OwningGrid?.OnColumnReadOnlyStateChanging(this, value);
                    _isReadOnly = value;
                }
            }
        }

        public double MaxWidth
        {
            get
            {
                return _maxWidth ?? double.PositiveInfinity;
            }
            set
            {
                if (value < 0)
                {
                    throw DataGridError.DataGrid.ValueMustBeGreaterThanOrEqualTo("value", "MaxWidth", 0);
                }
                if (value < ActualMinWidth)
                {
                    throw DataGridError.DataGrid.ValueMustBeGreaterThanOrEqualTo("value", "MaxWidth", "MinWidth");
                }
                if (!_maxWidth.HasValue || _maxWidth.Value != value)
                {
                    double oldValue = ActualMaxWidth;
                    _maxWidth = value;
                    if (OwningGrid != null && OwningGrid.ColumnsInternal != null)
                    {
                        OwningGrid.OnColumnMaxWidthChanged(this, oldValue);
                    }
                }
            }
        }

        public double MinWidth
        {
            get
            {
                return _minWidth ?? 0;
            }
            set
            {
                if (double.IsNaN(value))
                {
                    throw DataGridError.DataGrid.ValueCannotBeSetToNAN("MinWidth");
                }
                if (value < 0)
                {
                    throw DataGridError.DataGrid.ValueMustBeGreaterThanOrEqualTo("value", "MinWidth", 0);
                }
                if (double.IsPositiveInfinity(value))
                {
                    throw DataGridError.DataGrid.ValueCannotBeSetToInfinity("MinWidth");
                }
                if (value > ActualMaxWidth)
                {
                    throw DataGridError.DataGrid.ValueMustBeLessThanOrEqualTo("value", "MinWidth", "MaxWidth");
                }
                if (!_minWidth.HasValue || _minWidth.Value != value)
                {
                    double oldValue = ActualMinWidth;
                    _minWidth = value;
                    if (OwningGrid != null && OwningGrid.ColumnsInternal != null)
                    {
                        OwningGrid.OnColumnMinWidthChanged(this, oldValue);
                    }
                }
            }
        }

        public static readonly StyledProperty<DataGridLength> WidthProperty = AvaloniaProperty
            .Register<DataGridColumn, DataGridLength>(nameof(Width)
            , coerce: CoerceWidth
            );

        public DataGridLength Width
        {
            get => this.GetValue(WidthProperty);
            set => SetValue(WidthProperty, value);
        }

        /// <summary>
        /// The binding that will be used to get or set cell content for the clipboard.
        /// </summary>
        public virtual IBinding ClipboardContentBinding
        {
            get
            {
                return _clipboardContentBinding;
            }
            set
            {
                _clipboardContentBinding = value;
            }
        }

        /// <summary>
        /// Gets the value of a cell according to the the specified binding.
        /// </summary>
        /// <param name="item">The item associated with a cell.</param>
        /// <param name="binding">The binding to get the value of.</param>
        /// <returns>The resultant cell value.</returns>
        internal object GetCellValue(object item, IBinding binding)
        {
            Debug.Assert(OwningGrid != null);

            object content = null;
            if (binding != null)
            {
                OwningGrid.ClipboardContentControl.DataContext = item;
                var sub = OwningGrid.ClipboardContentControl.Bind(ContentControl.ContentProperty, binding);
                content = OwningGrid.ClipboardContentControl.GetValue(ContentControl.ContentProperty);
                sub.Dispose();
            }
            return content;
        }

        public Control GetCellContent(DataGridRow dataGridRow)
        {
            dataGridRow = dataGridRow ?? throw new ArgumentNullException(nameof(dataGridRow));
            if (OwningGrid == null)
            {
                throw DataGridError.DataGrid.NoOwningGrid(GetType());
            }
            if (dataGridRow.OwningGrid == OwningGrid)
            {
                DataGridCell dataGridCell = dataGridRow.Cells[Index];
                if (dataGridCell != null)
                {
                    return dataGridCell.Content as Control;
                }
            }
            return null;
        }

        public Control GetCellContent(object dataItem)
        {
            dataItem = dataItem ?? throw new ArgumentNullException(nameof(dataItem));
            if (OwningGrid == null)
            {
                throw DataGridError.DataGrid.NoOwningGrid(GetType());
            }
            DataGridRow dataGridRow = OwningGrid.GetRowFromItem(dataItem);
            if (dataGridRow == null)
            {
                return null;
            }
            return GetCellContent(dataGridRow);
        }

        /// <summary>
        /// Returns the column which contains the given element
        /// </summary>
        /// <param name="element">element contained in a column</param>
        /// <returns>Column that contains the element, or null if not found
        /// </returns>
        public static DataGridColumn GetColumnContainingElement(Control element)
        {
            // Walk up the tree to find the DataGridCell or DataGridColumnHeader that contains the element
            Visual parent = element;
            while (parent != null)
            {
                if (parent is DataGridCell cell)
                {
                    return cell.OwningColumn;
                }
                if (parent is DataGridColumnHeader columnHeader)
                {
                    return columnHeader.OwningColumn;
                }
                parent = parent.GetVisualParent();
            }
            return null;
        }

        /// <summary>
        /// Clears the current sort direction
        /// </summary>
        public void ClearSort()
        {
            //InvokeProcessSort is already validating if sorting is possible
            _headerCell?.InvokeProcessSort(KeyboardHelper.GetPlatformCtrlOrCmdKeyModifier(OwningGrid));
        }

        /// <summary>
        /// Switches the current state of sort direction
        /// </summary>
        public void Sort()
        {
            //InvokeProcessSort is already validating if sorting is possible
            _headerCell?.InvokeProcessSort(Input.KeyModifiers.None);
        }

        /// <summary>
        /// Changes the sort direction of this column
        /// </summary>
        /// <param name="direction">New sort direction</param>
        public void Sort(ListSortDirection direction)
        {
            //InvokeProcessSort is already validating if sorting is possible
            _headerCell?.InvokeProcessSort(Input.KeyModifiers.None, direction);
        }

        /// <summary>
        /// When overridden in a derived class, causes the column cell being edited to revert to the unedited value.
        /// </summary>
        /// <param name="editingElement">
        /// The element that the column displays for a cell in editing mode.
        /// </param>
        /// <param name="uneditedValue">
        /// The previous, unedited value in the cell being edited.
        /// </param>
        protected virtual void CancelCellEdit(Control editingElement, object uneditedValue)
        { }

        /// <summary>
        /// When overridden in a derived class, gets an editing element that is bound to the column's <see cref="P:Avalonia.Controls.DataGridBoundColumn.Binding" /> property value.
        /// </summary>
        /// <param name="cell">
        /// The cell that will contain the generated element.
        /// </param>
        /// <param name="dataItem">
        /// The data item represented by the row that contains the intended cell.
        /// </param>
        /// <param name="binding">When the method returns, contains the applied binding.</param>
        /// <returns>
        /// A new editing element that is bound to the column's <see cref="P:Avalonia.Controls.DataGridBoundColumn.Binding" /> property value.
        /// </returns>
        protected abstract Control GenerateEditingElement(DataGridCell cell, object dataItem, out ICellEditBinding binding);

        /// <summary>
        /// When overridden in a derived class, gets a read-only element that is bound to the column's
        /// <see cref="P:Avalonia.Controls.DataGridBoundColumn.Binding" /> property value.
        /// </summary>
        /// <param name="cell">
        /// The cell that will contain the generated element.
        /// </param>
        /// <param name="dataItem">
        /// The data item represented by the row that contains the intended cell.
        /// </param>
        /// <returns>
        /// A new, read-only element that is bound to the column's <see cref="P:Avalonia.Controls.DataGridBoundColumn.Binding" /> property value.
        /// </returns>
        protected abstract Control GenerateElement(DataGridCell cell, object dataItem);

        /// <summary>
        /// Called by a specific column type when one of its properties changed,
        /// and its current cells need to be updated.
        /// </summary>
        /// <param name="propertyName">Indicates which property changed and caused this call</param>
        protected void NotifyPropertyChanged(string propertyName)
        {
            OwningGrid?.RefreshColumnElements(this, propertyName);
        }

        /// <summary>
        /// When overridden in a derived class, called when a cell in the column enters editing mode.
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
        protected abstract object PrepareCellForEdit(Control editingElement, RoutedEventArgs editingEventArgs);

        /// <summary>
        /// Called by the DataGrid control when a column asked for its
        /// elements to be refreshed, typically because one of its properties changed.
        /// </summary>
        /// <param name="element">Indicates the element that needs to be refreshed</param>
        /// <param name="propertyName">Indicates which property changed and caused this call</param>
        protected internal virtual void RefreshCellContent(Control element, string propertyName)
        { }

        /// <summary>
        /// When overridden in a derived class, called when a cell in the column exits editing mode.
        /// </summary>
        protected virtual void EndCellEdit()
        { }

        internal void CancelCellEditInternal(Control editingElement, object uneditedValue)
        {
            CancelCellEdit(editingElement, uneditedValue);
        }

        internal void EndCellEditInternal()
        {
            EndCellEdit();
        }

        /// <summary>
        /// Coerces a DataGridLength to a valid value.  If any value components are double.NaN, this method
        /// coerces them to a proper initial value.  For star columns, the desired width is calculated based
        /// on the rest of the star columns.  For pixel widths, the desired value is based on the pixel value.
        /// For auto widths, the desired value is initialized as the column's minimum width.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="width">The DataGridLength to coerce.</param>
        /// <returns>The resultant (coerced) DataGridLength.</returns>
        private static DataGridLength CoerceWidth(AvaloniaObject source, DataGridLength width)
        {
            var target = (DataGridColumn)source;

            if (target._setWidthInternalNoCallback)
            {
                return width;
            }

            if (!target.IsSet(WidthProperty))
            {

                return target.OwningGrid?.ColumnWidth ??
                        DataGridLength.Auto;
            }

            double desiredValue = width.DesiredValue;
            if (double.IsNaN(desiredValue))
            {
                if (width.IsStar && target.OwningGrid != null && target.OwningGrid.ColumnsInternal != null)
                {
                    double totalStarValues = 0;
                    double totalStarDesiredValues = 0;
                    double totalNonStarDisplayWidths = 0;
                    foreach (DataGridColumn column in target.OwningGrid.ColumnsInternal.GetDisplayedColumns(c => c.IsVisible && c != target && !double.IsNaN(c.Width.DesiredValue)))
                    {
                        if (column.Width.IsStar)
                        {
                            totalStarValues += column.Width.Value;
                            totalStarDesiredValues += column.Width.DesiredValue;
                        }
                        else
                        {
                            totalNonStarDisplayWidths += column.ActualWidth;
                        }
                    }
                    if (totalStarValues == 0)
                    {
                        // Compute the new star column's desired value based on the available space if there are no other visible star columns
                        desiredValue = Math.Max(target.ActualMinWidth, target.OwningGrid.CellsWidth - totalNonStarDisplayWidths);
                    }
                    else
                    {
                        // Otherwise, compute its desired value based on those of other visible star columns
                        desiredValue = totalStarDesiredValues * width.Value / totalStarValues;
                    }
                }
                else if (width.IsAbsolute)
                {
                    desiredValue = width.Value;
                }
                else
                {
                    desiredValue = target.ActualMinWidth;
                }
            }

            double displayValue = width.DisplayValue;
            if (double.IsNaN(displayValue))
            {
                displayValue = desiredValue;
            }
            displayValue = Math.Max(target.ActualMinWidth, Math.Min(target.ActualMaxWidth, displayValue));

            return new DataGridLength(width.Value, width.UnitType, desiredValue, displayValue);
        }

        /// <summary>
        /// If the DataGrid is using layout rounding, the pixel snapping will force all widths to
        /// whole numbers. Since the column widths aren't visual elements, they don't go through the normal
        /// rounding process, so we need to do it ourselves.  If we don't, then we'll end up with some
        /// pixel gaps and/or overlaps between columns.
        /// </summary>
        /// <param name="leftEdge"></param>
        internal void ComputeLayoutRoundedWidth(double leftEdge)
        {
            if (OwningGrid != null && OwningGrid.UseLayoutRounding)
            {
                var scale = LayoutHelper.GetLayoutScale(HeaderCell);
                var roundSize = LayoutHelper.RoundLayoutSizeUp(new Size(leftEdge + ActualWidth, 1), scale, scale);
                LayoutRoundedWidth = roundSize.Width - leftEdge;
            }
            else
            {
                LayoutRoundedWidth = ActualWidth;
            }
        }

        internal virtual DataGridColumnHeader CreateHeader()
        {
            var result = new DataGridColumnHeader
            {
                OwningColumn = this
            };
            result[!ContentControl.ContentProperty] = this[!HeaderProperty];
            result[!ContentControl.ContentTemplateProperty] = this[!HeaderTemplateProperty];
            if (OwningGrid.ColumnHeaderTheme is { } columnTheme)
            {
                result.SetValue(StyledElement.ThemeProperty, columnTheme, BindingPriority.Template);
            }

            result.PointerPressed += (s, e) => { HeaderPointerPressed?.Invoke(this, e); };
            result.PointerReleased += (s, e) => { HeaderPointerReleased?.Invoke(this, e); };
            return result;
        }

        /// <summary>
        /// Ensures that this column's width has been coerced to a valid value.
        /// </summary>
        internal void EnsureWidth()
        {
            SetWidthInternalNoCallback(CoerceWidth(this, Width));
        }

        internal Control GenerateElementInternal(DataGridCell cell, object dataItem)
        {
            return GenerateElement(cell, dataItem);
        }

        internal object PrepareCellForEditInternal(Control editingElement, RoutedEventArgs editingEventArgs)
        {
            var result = PrepareCellForEdit(editingElement, editingEventArgs);
            editingElement.Focus();

            return result;
        }

        /// <summary>
        /// Attempts to resize the column's width to the desired DisplayValue, but limits the final size
        /// to the column's minimum and maximum values.  If star sizing is being used, then the column
        /// can only decrease in size by the amount that the columns after it can increase in size.
        /// Likewise, the column can only increase in size if other columns can spare the width.
        /// </summary>
        /// <param name="oldWidth">with before resize.</param>
        /// <param name="newWidth">with after resize.</param>
        /// <param name="userInitiated">Whether or not this resize was initiated by a user action.</param>

        //  double value, DataGridLengthUnitType unitType, double desiredValue, double displayValue
        internal void Resize(DataGridLength oldWidth, DataGridLength newWidth, bool userInitiated)
        {
            double newValue = newWidth.Value;
            double newDesiredValue = newWidth.DesiredValue;
            double newDisplayValue = Math.Max(ActualMinWidth, Math.Min(ActualMaxWidth, newWidth.DisplayValue));
            DataGridLengthUnitType newUnitType = newWidth.UnitType;

            int starColumnsCount = 0;
            double totalDisplayWidth = 0;
            foreach (DataGridColumn column in OwningGrid.ColumnsInternal.GetVisibleColumns())
            {
                column.EnsureWidth();
                totalDisplayWidth += column.ActualWidth;
                starColumnsCount += (column != this && column.Width.IsStar) ? 1 : 0;
            }
            bool hasInfiniteAvailableWidth = !OwningGrid.RowsPresenterAvailableSize.HasValue || double.IsPositiveInfinity(OwningGrid.RowsPresenterAvailableSize.Value.Width);

            // If we're using star sizing, we can only resize the column as much as the columns to the
            // right will allow (i.e. until they hit their max or min widths).
            if (!hasInfiniteAvailableWidth && (starColumnsCount > 0 || (newUnitType == DataGridLengthUnitType.Star && newWidth.IsStar && userInitiated)))
            {
                double limitedDisplayValue = oldWidth.DisplayValue;
                double availableIncrease = Math.Max(0, OwningGrid.CellsWidth - totalDisplayWidth);
                double desiredChange = newDisplayValue - oldWidth.DisplayValue;
                if (desiredChange > availableIncrease)
                {
                    // The desired change is greater than the amount of available space,
                    // so we need to decrease the widths of columns to the right to make room.
                    desiredChange -= availableIncrease;
                    double actualChange = desiredChange + OwningGrid.DecreaseColumnWidths(DisplayIndex + 1, -desiredChange, userInitiated);
                    limitedDisplayValue += availableIncrease + actualChange;
                }
                else if (desiredChange > 0)
                {
                    // The desired change is positive but less than the amount of available space,
                    // so there's no need to decrease the widths of columns to the right.
                    limitedDisplayValue += desiredChange;
                }
                else
                {
                    // The desired change is negative, so we need to increase the widths of columns to the right.
                    limitedDisplayValue += desiredChange + OwningGrid.IncreaseColumnWidths(DisplayIndex + 1, -desiredChange, userInitiated);
                }
                if (ActualCanUserResize || (oldWidth.IsStar && !userInitiated))
                {
                    newDisplayValue = limitedDisplayValue;
                }
            }

            if (userInitiated)
            {
                newDesiredValue = newDisplayValue;
                if (!Width.IsStar)
                {
                    InheritsWidth = false;
                    newValue = newDisplayValue;
                    newUnitType = DataGridLengthUnitType.Pixel;
                }
                else if (starColumnsCount > 0 && !hasInfiniteAvailableWidth)
                {
                    // Recalculate star weight of this column based on the new desired value
                    InheritsWidth = false;
                    newValue = Width.Value * newDisplayValue / ActualWidth;
                }
            }

            newDisplayValue = Math.Min(double.MaxValue, newValue);
            newWidth = new DataGridLength(newDisplayValue, newUnitType, newDesiredValue, newDisplayValue);
            SetWidthInternalNoCallback(newWidth);
            if (newWidth != oldWidth)
            {
                OwningGrid.OnColumnWidthChanged(this);
            }
        }

        /// <summary>
        /// Sets the column's Width to a new DataGridLength with a different DesiredValue.
        /// </summary>
        /// <param name="desiredValue">The new DesiredValue.</param>
        internal void SetWidthDesiredValue(double desiredValue)
        {
            SetWidthInternalNoCallback(new DataGridLength(Width.Value, Width.UnitType, desiredValue, Width.DisplayValue));
        }

        /// <summary>
        /// Sets the column's Width to a new DataGridLength with a different DisplayValue.
        /// </summary>
        /// <param name="displayValue">The new DisplayValue.</param>
        internal void SetWidthDisplayValue(double displayValue)
        {
            SetWidthInternalNoCallback(new DataGridLength(Width.Value, Width.UnitType, Width.DesiredValue, displayValue));
        }

        /// <summary>
        /// Set the column's Width without breaking inheritance.
        /// </summary>
        /// <param name="width">The new Width.</param>
        internal void SetWidthInternal(DataGridLength width)
        {
            bool originalValue = _settingWidthInternally;
            _settingWidthInternally = true;
            try
            {
                Width = width;
            }
            finally
            {
                _settingWidthInternally = originalValue;
            }
        }

        /// <summary>
        /// Sets the column's Width directly, without any callback effects.
        /// </summary>
        /// <param name="width">The new Width.</param>
        internal void SetWidthInternalNoCallback(DataGridLength width)
        {
            var originalValue = _setWidthInternalNoCallback;
            _setWidthInternalNoCallback = true;
            try
            {
                Width = width;
            }
            finally
            {
                _setWidthInternalNoCallback = originalValue;
            }

        }

        /// <summary>
        /// Set the column's star value.  Whenever the star value changes, width inheritance is broken.
        /// </summary>
        /// <param name="value">The new star value.</param>
        internal void SetWidthStarValue(double value)
        {
            InheritsWidth = false;
            SetWidthInternalNoCallback(new DataGridLength(value, Width.UnitType, Width.DesiredValue, Width.DisplayValue));
        }

        //TODO Binding
        internal Control GenerateEditingElementInternal(DataGridCell cell, object dataItem)
        {
            if (_editingElement == null)
            {
                _editingElement = GenerateEditingElement(cell, dataItem, out _editBinding);
            }

            return _editingElement;
        }

        /// <summary>
        /// Clears the cached editing element.
        /// </summary>
        //TODO Binding
        internal void RemoveEditingElement()
        {
            _editingElement = null;
        }

        /// <summary>
        /// Holds the name of the member to use for sorting, if not using the default.
        /// </summary>
        public string SortMemberPath
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets an object associated with this column.
        /// </summary>
        public object Tag
        {
            get;
            set;
        }

        /// <summary>
        /// Holds a Comparer to use for sorting, if not using the default.
        /// </summary>
        public System.Collections.IComparer CustomSortComparer
        {
            get;
            set;
        }

        /// <summary>
        /// We get the sort description from the data source.  We don't worry whether we can modify sort -- perhaps the sort description
        /// describes an unchangeable sort that exists on the data.
        /// </summary>
        internal DataGridSortDescription GetSortDescription()
        {
            if (OwningGrid != null
                && OwningGrid.DataConnection != null
                && OwningGrid.DataConnection.SortDescriptions != null)
            {
                if (CustomSortComparer != null)
                {
                    return
                        OwningGrid.DataConnection.SortDescriptions
                                  .OfType<DataGridComparerSortDescription>()
                                  .FirstOrDefault(s => s.SourceComparer == CustomSortComparer);
                }

                string propertyName = GetSortPropertyName();

                return OwningGrid.DataConnection.SortDescriptions.FirstOrDefault(s => s.HasPropertyPath && s.PropertyPath == propertyName);
            }

            return null;
        }

        internal string GetSortPropertyName()
        {
            string result = SortMemberPath;

            if (String.IsNullOrEmpty(result))
            {
                if (this is DataGridBoundColumn boundColumn)
                {
                    if (boundColumn.Binding is Binding binding)
                    {
                        result = binding.Path;
                    }
                    else if (boundColumn.Binding is CompiledBindingExtension compiledBinding)
                    {
                        result = compiledBinding.Path.ToString();
                    }
                }
            }

            return result;
        }

    }

}
