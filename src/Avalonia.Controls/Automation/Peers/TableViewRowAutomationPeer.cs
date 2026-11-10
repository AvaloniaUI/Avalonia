using System;
using System.Globalization;
using System.Text;
using Avalonia.Automation;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
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
        var hasVisibleCell = false;
        foreach (var child in presenter.Children)
        {
            if (child is not TableViewCell cell)
                continue;

            if (!cell.IsVisible || AutomationProperties.GetAccessibilityView(cell) == AccessibilityView.Raw)
                continue;

            hasVisibleCell = true;
            AppendCellName(cell, builder);
        }

        if (builder.Length > 0)
            return builder.ToString();

        return hasVisibleCell || presenter.Children.Count == 0 ? base.GetNameCore() : null;
    }

    private void AppendCellName(TableViewCell cell, StringBuilder builder)
    {
        var name = AutomationProperties.GetName(cell);
        if (!string.IsNullOrWhiteSpace(name))
        {
            AppendName(name, builder);
            return;
        }

        if (AutomationProperties.GetLabeledBy(cell) is { } labeledBy)
        {
            AppendVisualName(labeledBy, builder, allowLabeledBy: false);
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

    private void AppendVisualName(Visual visual, StringBuilder builder, bool allowLabeledBy = true)
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

            if (allowLabeledBy && AutomationProperties.GetLabeledBy(control) is { } labeledBy)
            {
                var previousLength = builder.Length;
                AppendVisualName(labeledBy, builder, allowLabeledBy: false);
                if (builder.Length > previousLength)
                    return;
            }

            if (control is AccessText accessText)
            {
                AppendName(AccessText.RemoveAccessKeyMarker(accessText.Inlines?.Text ?? accessText.Text), builder);
                return;
            }

            if (control is TextBlock textBlock)
            {
                AppendName(textBlock.Inlines?.Text ?? textBlock.Text, builder);
                return;
            }

            var peer = CreatePeerForElement(control);
            if (peer.GetProvider<IValueProvider>() is { } valueProvider)
            {
                AppendName(valueProvider.Value, builder);
                return;
            }

            if (peer.GetProvider<IRangeValueProvider>() is { } rangeValueProvider)
            {
                AppendName(rangeValueProvider.Value.ToString(CultureInfo.CurrentCulture), builder);
                return;
            }

            if (control.VisualChildren.Count > 0)
            {
                foreach (var child in control.VisualChildren)
                    AppendVisualName(child, builder);

                return;
            }

            if (control is not ContentControl contentControl ||
                !ReferenceEquals(contentControl.Content, Owner.Content) ||
                contentControl.Content is string ||
                contentControl.Content?.GetType().IsValueType == true)
            {
                AppendName(peer.GetName(), builder);
            }

            return;
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
