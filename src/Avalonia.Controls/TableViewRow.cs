using Avalonia.Collections;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;

namespace Avalonia.Controls;

/// <summary>
/// A row container in a <see cref="TableView"/>.
/// </summary>
[TemplatePart(PartRowPresenter, typeof(TableViewRowPresenter))]
public class TableViewRow : ListBoxItem
{
    private const string PartRowPresenter = "PART_RowPresenter";

    private TableViewRowPresenter? _rowPresenter;

    internal AvaloniaList<TableViewColumn>? Columns
    {
        get;
        set
        {
            field = value;
            _rowPresenter?.Columns = value;
        }
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        if (_rowPresenter is not null)
        {
            _rowPresenter.Columns = null;
            _rowPresenter.RemoveCells();
        }

        _rowPresenter = e.NameScope.Find<TableViewRowPresenter>(PartRowPresenter);

        if (_rowPresenter is not null)
        {
            _rowPresenter.Columns = Columns;
            _rowPresenter.RebuildCells();
        }
    }

    internal void ClearCells()
        => _rowPresenter?.ClearCells();

    internal void InvalidateCellsMeasure()
        => _rowPresenter?.InvalidateMeasure();

    internal void RebuildCells()
        => _rowPresenter?.RebuildCells();
}
