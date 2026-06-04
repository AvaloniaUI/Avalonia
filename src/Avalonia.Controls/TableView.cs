using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Collections;

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
                SubscribeToColumns(field);
            }

            return field;
        }
        set
        {
            var oldColumns = field;
            if (oldColumns is not null)
                UnsubscribeFromColumns(oldColumns);

            SetAndRaise(ColumnsProperty, ref field, value);

            if (value is not null)
                SubscribeToColumns(value);
        }
    }

    private void SubscribeToColumns(AvaloniaList<TableViewColumn> columns)
        => columns.CollectionChanged += OnColumnsChanged;

    private void UnsubscribeFromColumns(AvaloniaList<TableViewColumn> columns)
        => columns.CollectionChanged -= OnColumnsChanged;

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
        if (!IsInitialized)
            return;

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
}
