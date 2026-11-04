using System;
using System.Text;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.VisualTree;

namespace Avalonia.Automation.Peers;

/// <summary>
/// Automation peer for a <see cref="TableViewRow"/>.
/// </summary>
/// <remarks>
/// A row's data item is not necessarily a useful accessible name. When cells are realized,
/// the row name is composed from their visible content instead.
/// </remarks>
public class TableViewRowAutomationPeer : ListItemAutomationPeer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TableViewRowAutomationPeer"/> class.
    /// </summary>
    /// <param name="owner">The row represented by the peer.</param>
    public TableViewRowAutomationPeer(TableViewRow owner)
        : base(owner)
    {
    }

    /// <summary>
    /// Gets the row represented by the peer.
    /// </summary>
    public new TableViewRow Owner => (TableViewRow)base.Owner;

    /// <inheritdoc />
    protected override AutomationControlType GetAutomationControlTypeCore()
        => AutomationControlType.DataItem;

    /// <inheritdoc />
    protected override string? GetNameCore()
    {
        var name = AutomationProperties.GetName(Owner);
        if (!string.IsNullOrWhiteSpace(name))
            return name;

        name = GetLabeledBy()?.GetName();
        if (!string.IsNullOrWhiteSpace(name))
            return name;

        if (Owner.CellsPresenter is not { } presenter)
            return base.GetNameCore();

        var builder = new StringBuilder();
        foreach (var child in presenter.Children)
        {
            if (child is TableViewCell cell && cell.IsVisible)
                AppendCellName(cell, builder);
        }

        return builder.Length > 0 ? builder.ToString() : base.GetNameCore();
    }

    private void AppendCellName(TableViewCell cell, StringBuilder builder)
    {
        var name = AutomationProperties.GetName(cell);
        if (!string.IsNullOrWhiteSpace(name))
        {
            AppendName(name, builder);
            return;
        }

        if (cell.Presenter?.Child is { } child)
        {
            AppendVisualName(child, builder);
            return;
        }

        var content = cell.Content;
        if (content is not null &&
            (content is string || !ReferenceEquals(content, Owner.Content)))
        {
            AppendName(content.ToString(), builder);
        }
    }

    private void AppendVisualName(Visual visual, StringBuilder builder)
    {
        if (visual is Control control)
        {
            if (!control.IsVisible ||
                AutomationProperties.GetAccessibilityView(control) == AccessibilityView.Raw)
            {
                return;
            }

            var name = AutomationProperties.GetName(control);
            if (!string.IsNullOrWhiteSpace(name))
            {
                AppendName(name, builder);
                return;
            }

            if (control is TextBlock)
            {
                AppendName(CreatePeerForElement(control).GetName(), builder);
                return;
            }

            name = CreatePeerForElement(control).GetName();
            if (!string.IsNullOrWhiteSpace(name) &&
                !string.Equals(name, Owner.Content?.ToString(), StringComparison.Ordinal))
            {
                AppendName(name, builder);
                return;
            }
        }

        foreach (var child in visual.VisualChildren)
            AppendVisualName(child, builder);
    }

    private static void AppendName(string? name, StringBuilder builder)
    {
        if (string.IsNullOrWhiteSpace(name))
            return;

        if (builder.Length > 0)
            builder.Append(' ');

        builder.Append(name.Trim());
    }
}
