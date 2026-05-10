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

    internal AvaloniaList<TableViewColumn>? Columns { get; set; }

    /// <inheritdoc />
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        SetPresenterColumns(null);
        _rowPresenter = e.NameScope.Find<TableViewRowPresenter>(PartRowPresenter);
        SetPresenterColumns(Columns);
    }

    private void SetPresenterColumns(AvaloniaList<TableViewColumn>? columns)
    {
        if (_rowPresenter is null)
            return;

        _rowPresenter.Columns = columns;
        _rowPresenter.RebuildCells();
    }

    internal void InvalidateCells(bool rebuild)
    {
        if (_rowPresenter is null)
            return;

        if (rebuild)
            _rowPresenter.RebuildCells();
        else
            _rowPresenter.InvalidateMeasure();
    }
}
