using System;
using System.Text;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;

namespace Avalonia.Controls;

/// <summary>
/// Represents the header of a <see cref="TableViewColumn"/>.
/// </summary>
[TemplatePart(PartResizer, typeof(Thumb))]
public class TableViewColumnHeader : ContentControl
{
    private const string PartResizer = "PART_Resizer";

    /// <summary>
    /// Identifies the <see cref="Column"/> property.
    /// </summary>
    public static readonly DirectProperty<TableViewColumnHeader, TableViewColumn?> ColumnProperty =
        AvaloniaProperty.RegisterDirect<TableViewColumnHeader, TableViewColumn?>(nameof(Column), o => o.Column);

    private Thumb? _resizer;

    /// <summary>
    /// Gets the column associated with this header.
    /// </summary>
    public TableViewColumn? Column
    {
        get;
        internal set => SetAndRaise(ColumnProperty, ref field, value);
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _resizer?.DragDelta -= OnResizerDragDelta;
        _resizer = e.NameScope.Find<Thumb>(PartResizer);
        _resizer?.DragDelta += OnResizerDragDelta;
    }

    private void OnResizerDragDelta(object? sender, VectorEventArgs e)
    {
        if (Column is not { } column)
            return;

        var actualWidth = column.ActualWidth;
        if (double.IsNaN(actualWidth))
            return;

        var minWidth = _resizer?.Bounds.Width ?? 0;
        var newWidth = Math.Max(minWidth, actualWidth + e.Vector.X);
        column.Width = new GridLength(newWidth, GridUnitType.Pixel);
    }

    internal override void BuildDebugDisplay(StringBuilder builder, bool includeContent)
    {
        base.BuildDebugDisplay(builder, includeContent);

        if (includeContent)
        {
            DebugDisplayHelper.AppendOptionalValue(builder, nameof(Column), Column?.Header, includeContent);
        }
    }
}
