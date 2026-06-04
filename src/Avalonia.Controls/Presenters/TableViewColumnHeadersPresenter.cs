using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;
using static Avalonia.Controls.Presenters.TableViewLayoutHelper;

namespace Avalonia.Controls.Presenters;

/// <summary>
/// Displays the column headers for a <see cref="TableView"/>.
/// Computes column widths (pixel and star) and exposes them via
/// <see cref="TableViewColumn.ActualWidth"/> so that <see cref="TableViewRowPresenter"/>
/// can align cells with the headers.
/// </summary>
public class TableViewColumnHeadersPresenter : Panel
{
    internal TableView? TableView { get; set; }

    private void RebuildHeaders()
    {
        Children.Clear();

        if (TableView is null)
            return;

        foreach (var column in TableView.Columns)
        {
            var header = new TableViewColumnHeader
            {
                Column = column,
                [!ContentControl.ContentProperty] = column[!TableViewColumn.HeaderProperty],
                [!ContentControl.ContentTemplateProperty] = column[!TableViewColumn.HeaderTemplateProperty],
            };

            Children.Add(header);
        }

        InvalidateMeasure();
    }

    /// <inheritdoc />
    protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnAttachedToLogicalTree(e);

        TableView = this.FindLogicalAncestorOfType<TableView>();
        RebuildHeaders();

        if (TableView is not null)
        {
            TableView.Columns.CollectionChanged += OnColumnsChanged;

            if (TableView.Scroll is INotifyPropertyChanged notifyPropertyChanged)
                notifyPropertyChanged.PropertyChanged += OnScrollPropertyChanged;
        }
    }

    protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        if (TableView is not null)
        {
            TableView.Columns.CollectionChanged -= OnColumnsChanged;

            if (TableView.Scroll is INotifyPropertyChanged notifyPropertyChanged)
                notifyPropertyChanged.PropertyChanged -= OnScrollPropertyChanged;

            TableView = null;
            RebuildHeaders();
        }

        base.OnDetachedFromLogicalTree(e);
    }

    private void OnColumnsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => RebuildHeaders();

    private void OnScrollPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IScrollable.Offset))
            InvalidateArrange();
    }

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        if (TableView is null)
            return default;

        if (UpdateActualWidths(TableView.Columns, availableSize.Width))
            TableView.InvalidateCellsMeasure();

        return MeasureRow(TableView.Columns, Children, availableSize);
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (TableView is null)
            return finalSize;

        var columns = TableView.Columns;

        if (UpdateActualWidths(columns, finalSize.Width))
            TableView.InvalidateCellsMeasure();

        var offset = TableView.Scroll?.Offset.X ?? 0;
        return ArrangeRow(columns, Children, finalSize, -offset);
    }
}
