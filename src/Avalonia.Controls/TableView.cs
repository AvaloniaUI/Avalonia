using System;
using System.Collections.Specialized;
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

    private IDisposable? _columnsSizeTracker;

    /// <summary>
    /// Gets or sets the collection of columns displayed by this <see cref="TableView"/>.
    /// </summary>
    [NotNull]
    public AvaloniaList<TableViewColumn>? Columns
    {
        get
        {
            if (field is null)
            {
                field = [];
                if (IsInitialized)
                    SubscribeToColumns(field);
            }

            return field;
        }
        set
        {
            var oldValue = field;
            if (oldValue == value)
                return;

            if (oldValue is not null)
                UnsubscribeFromColumns(oldValue);

            SetAndRaise(ColumnsProperty, ref field, value);

            if (IsInitialized)
            {
                if (value is not null)
                    SubscribeToColumns(value);

                RebuildCells();
            }
        }
    }

    internal event EventHandler<ColumnsChangedEventArgs>? ColumnsChanged;

    private void SubscribeToColumns(AvaloniaList<TableViewColumn> columns)
    {
        columns.CollectionChanged += OnColumnsChanged;

        _columnsSizeTracker = columns.TrackItemPropertyChanged(tuple =>
        {
            if (tuple.Item2.PropertyName is nameof(TableViewColumn.Width) or null)
                OnColumnsSizeChanged();
        });
    }

    private void UnsubscribeFromColumns(AvaloniaList<TableViewColumn> columns)
    {
        columns.CollectionChanged -= OnColumnsChanged;

        _columnsSizeTracker?.Dispose();
        _columnsSizeTracker = null;
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

    private void OnColumnsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        ResetActualWidths(Columns);
        ColumnsChanged?.Invoke(this, new ColumnsChangedEventArgs(false));
        RebuildCells();
    }

    private void OnColumnsSizeChanged()
    {
        ResetActualWidths(Columns);
        ColumnsChanged?.Invoke(this, new ColumnsChangedEventArgs(true));
        InvalidateCellsMeasure();
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();

        SubscribeToColumns(Columns);
    }

    private void RebuildCells()
    {
        foreach (var row in GetRealizedContainers())
        {
            if (row is TableViewRow tableViewRow)
                tableViewRow.RebuildCells();
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

    internal readonly struct ColumnsChangedEventArgs(bool isSizeChange)
    {
        public bool IsSizeChange { get; } = isSizeChange;
    }
}
