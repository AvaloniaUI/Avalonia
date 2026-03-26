using Avalonia.Controls;

namespace Avalonia.Automation.Peers;

public class TabbedPageAutomationPeer : ControlAutomationPeer
{
    public TabbedPageAutomationPeer(TabbedPage owner)
        : base(owner)
    {
    }

    public new TabbedPage Owner => (TabbedPage)base.Owner;

    protected override AutomationControlType GetAutomationControlTypeCore()
        => AutomationControlType.Pane;

    protected override string? GetNameCore()
    {
        var result = base.GetNameCore();

        if (string.IsNullOrEmpty(result))
            result = Owner.Header?.ToString();

        var index = Owner.SelectedIndex;
        var tabCount = GetTabCount();

        if (index >= 0 && tabCount > 0)
        {
            var header = Owner.SelectedPage?.Header?.ToString();
            var position = $"Tab {index + 1} of {tabCount}";
            var tabName = string.IsNullOrEmpty(header) ? position : $"{position}: {header}";
            return string.IsNullOrEmpty(result) ? tabName : $"{result} {tabName}";
        }

        return result;
    }

    private int GetTabCount()
    {
        return Owner.GetTabCount();
    }
}
