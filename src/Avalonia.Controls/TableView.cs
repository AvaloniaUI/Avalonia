using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Collections;
using static Avalonia.Controls.Presenters.TableViewLayoutHelper;

namespace Avalonia.Controls;

/// <summary>
/// A read-only tabular control that presents items in configurable columns.
/// </summary>
public class TableView : ListBox
{
    /// <summary>
    /// Defines the <see cref="Columns"/> property.
    /// </summary>
    public static readonly DirectProperty<TableView, AvaloniaList<TableViewColumn>?> ColumnsProperty =
        AvaloniaProperty.RegisterDirect<TableView, AvaloniaList<TableViewColumn>?>(
            nameof(Columns),
            o => o.Columns,
            (o, v) => o.Columns = v);

    /// <summary>
    /// Defines the <see cref="CanResizeColumns"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> CanResizeColumnsProperty =
        AvaloniaProperty.Register<TableView, bool>(nameof(CanResizeColumns), true);

    private IDisposable? _columnsSubscription;
    private AvaloniaList<TableViewColumn>? _columns;

    /// <summary>
    /// Gets or sets the collection of columns displayed by this <see cref="TableView"/>.
    /// </summary>
    [NotNull]
    public AvaloniaList<TableViewColumn>? Columns
    {
        get
        {
            if (_columns is null)
            {
                _columns = new AvaloniaList<TableViewColumn> { ResetBehavior = ResetBehavior.Remove };
                if (IsInitialized)
                    SubscribeToColumns(_columns);
            }

            return _columns;
        }
        set
        {
            var oldValue = _columns;
            if (oldValue == value)
                return;

            UnsubscribeFromColumns();
            value?.ResetBehavior = ResetBehavior.Remove;
            SetAndRaise(ColumnsProperty, ref _columns, value);

            if (IsInitialized)
            {
                if (value is not null)
                    SubscribeToColumns(value);

                ColumnsChanged?.Invoke(this, new ColumnsChangedEventArgs(false));

                RebuildCells();
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the user can resize columns by dragging the
    /// separator between column headers.
    /// </summary>
    public bool CanResizeColumns
    {
        get => GetValue(CanResizeColumnsProperty);
        set => SetValue(CanResizeColumnsProperty, value);
    }

    internal event EventHandler<ColumnsChangedEventArgs>? ColumnsChanged;

    private void SubscribeToColumns(AvaloniaList<TableViewColumn> columns)
    {
        Debug.Assert(_columnsSubscription is null);

        _columnsSubscription = columns.ForEachItem(
            column =>
            {
                if (column.TableView is not null && column.TableView != this)
                {
                    throw new InvalidOperationException(
                        $"The column {column.DebugDisplay} is already attached to a {nameof(TableView)}.");
                }

                column.TableView = this;

                OnColumnsChanged();
            },
            column =>
            {
                Debug.Assert(column.TableView == this);

                column.TableView = null;
                column.ActualWidth = double.NaN;

                OnColumnsChanged();
            },
            () => { });
    }

    private void UnsubscribeFromColumns()
    {
        _columnsSubscription?.Dispose();
        _columnsSubscription = null;
    }

    /// <inheritdoc/>
    protected internal override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey)
        => new TableViewRow();

    /// <inheritdoc/>
    protected internal override void PrepareContainerForItemOverride(Control container, object? item, int index)
    {
        base.PrepareContainerForItemOverride(container, item, index);

        if (container is TableViewRow row)
        {
            row.Columns = Columns;
            row.RebuildCells();
        }
    }

    /// <inheritdoc/>
    protected internal override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
        => NeedsContainer<TableViewRow>(item, out recycleKey);

    /// <inheritdoc/>
    protected internal override void ClearContainerForItemOverride(Control element)
    {
        base.ClearContainerForItemOverride(element);

        if (element is TableViewRow row)
        {
            row.Columns = null;
            row.ClearCells();
        }
    }

    private void OnColumnsChanged()
    {
        ResetActualWidths(Columns);
        ColumnsChanged?.Invoke(this, new ColumnsChangedEventArgs(false));
        RebuildCells();
    }

    internal void OnColumnsSizeChanged()
    {
        ResetActualWidths(Columns);
        ColumnsChanged?.Invoke(this, new ColumnsChangedEventArgs(true));
        InvalidateCellsMeasure();
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();

        if (_columns is not null)
            SubscribeToColumns(_columns);
    }

    private void RebuildCells()
    {
        var columns = Columns;

        foreach (var row in GetRealizedContainers())
        {
            if (row is TableViewRow tableViewRow)
            {
                tableViewRow.Columns = columns;
                tableViewRow.RebuildCells();
            }
            else
                row.InvalidateMeasure();
        }
    }

    internal void InvalidateCellsMeasure()
    {
        foreach (var row in GetRealizedContainers())
        {
            if (row is TableViewRow tableViewRow)
                tableViewRow.InvalidateCellsMeasure();
            else
                row.InvalidateMeasure();
        }
    }

    internal void RefreshColumnCells(TableViewColumn column)
    {
        var columnIndex = Columns.IndexOf(column);
        if (columnIndex < 0)
            return;

        foreach (var row in GetRealizedContainers())
        {
            if (row is TableViewRow tableViewRow)
                tableViewRow.RefreshCell(columnIndex);
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == CanResizeColumnsProperty && _columns is not null)
        {
            foreach (var column in _columns)
                column.UpdateCanEffectivelyResize();
        }
    }

    internal readonly struct ColumnsChangedEventArgs(bool isSizeChange)
    {
        public bool IsSizeChange { get; } = isSizeChange;
    }
}
