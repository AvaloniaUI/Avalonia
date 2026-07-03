using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Collections;
using Avalonia.Controls.Presenters;
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

                RebuildHeaders();
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

    internal TableViewColumnHeadersPresenter? HeadersPresenter { get; set; }

    private void SubscribeToColumns(AvaloniaList<TableViewColumn> columns)
    {
        Debug.Assert(_columnsSubscription is null);

        var isInitialIteration = true;

        _columnsSubscription = columns.ForEachItem(
            [SuppressMessage("ReSharper", "AccessToModifiedClosure")]
            (column) =>
            {
                AttachColumn(column);
                if (!isInitialIteration)
                    OnColumnsChanged();
            },
            column =>
            {
                DetachColumn(column);
                OnColumnsChanged();
            },
            () => { });

        isInitialIteration = false;
    }

    private void AttachColumn(TableViewColumn column)
    {
        if (column.TableView is not null && column.TableView != this)
        {
            throw new InvalidOperationException(
                $"The column {column.DebugDisplay} is already attached to a {nameof(TableView)}.");
        }

        column.TableView = this;
    }

    private void DetachColumn(TableViewColumn column)
    {
        Debug.Assert(column.TableView == this || column.TableView is null);

        column.TableView = null;
        column.ActualWidth = double.NaN;
    }

    private void UnsubscribeFromColumns()
    {
        if (_columnsSubscription is null)
            return;

        _columnsSubscription.Dispose();
        _columnsSubscription = null;

        foreach (var column in Columns)
            DetachColumn(column);
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
        RebuildHeaders();
        RebuildCells();
    }

    internal void OnColumnsSizeChanged()
    {
        ResetActualWidths(Columns);
        InvalidateHeadersMeasure();
        InvalidateCellsMeasure();
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();

        if (_columns is not null)
            SubscribeToColumns(_columns);
    }

    private void RebuildHeaders()
        => HeadersPresenter?.RebuildHeaders();

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

    internal void InvalidateHeadersMeasure()
        => HeadersPresenter?.InvalidateMeasure();

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

    internal void RefreshColumnHeaders(TableViewColumn column)
    {
        var columnIndex = Columns.IndexOf(column);
        if (columnIndex < 0)
            return;

        HeadersPresenter?.RefreshHeader(columnIndex);
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
}
