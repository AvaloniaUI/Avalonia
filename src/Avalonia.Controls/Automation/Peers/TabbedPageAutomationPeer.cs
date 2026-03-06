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

        return result;
    }
}
