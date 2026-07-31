using Avalonia.Controls;

namespace Avalonia.Automation.Peers;

public class NavigationPageAutomationPeer : ControlAutomationPeer
{
    public NavigationPageAutomationPeer(NavigationPage owner)
        : base(owner)
    {
    }

    public new NavigationPage Owner => (NavigationPage)base.Owner;

    protected override AutomationControlType GetAutomationControlTypeCore()
        => AutomationControlType.Pane;

    protected override string? GetNameCore()
    {
        var result = base.GetNameCore();

        if (string.IsNullOrEmpty(result))
            result = Owner.Header?.ToString();

        if (string.IsNullOrEmpty(result))
            result = Owner.CurrentPage?.Header?.ToString();

        return result;
    }
}
