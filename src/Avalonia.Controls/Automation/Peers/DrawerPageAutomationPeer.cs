using Avalonia.Controls;

namespace Avalonia.Automation.Peers;

public class DrawerPageAutomationPeer : ControlAutomationPeer
{
    public DrawerPageAutomationPeer(DrawerPage owner)
        : base(owner)
    {
    }

    public new DrawerPage Owner => (DrawerPage)base.Owner;

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
