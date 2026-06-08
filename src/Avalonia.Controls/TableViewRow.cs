using Avalonia.Collections;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;

namespace Avalonia.Controls;

/// <summary>
/// A row container in a <see cref="TableView"/>.
/// </summary>
[TemplatePart(PartCellsPresenter, typeof(TableViewCellsPresenter))]
public class TableViewRow : ListBoxItem
{
    private const string PartCellsPresenter = "PART_CellsPresenter";

    private TableViewCellsPresenter? _cellsPresenter;

    internal AvaloniaList<TableViewColumn>? Columns
    {
        get;
        set
        {
            field = value;
            _cellsPresenter?.Columns = value;
        }
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        if (_cellsPresenter is not null)
        {
            _cellsPresenter.Columns = null;
            _cellsPresenter.RemoveCells();
        }

        _cellsPresenter = e.NameScope.Find<TableViewCellsPresenter>(PartCellsPresenter);

        if (_cellsPresenter is not null)
        {
            _cellsPresenter.Columns = Columns;
            _cellsPresenter.RebuildCells();
        }
    }

    internal void ClearCells()
        => _cellsPresenter?.ClearCells();

    internal void InvalidateCellsMeasure()
        => _cellsPresenter?.InvalidateMeasure();

    internal void RebuildCells()
        => _cellsPresenter?.RebuildCells();

    internal void RefreshCell(int columnIndex)
        => _cellsPresenter?.RefreshCell(columnIndex);
}
