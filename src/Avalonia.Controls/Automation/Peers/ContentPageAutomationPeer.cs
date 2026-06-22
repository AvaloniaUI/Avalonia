using Avalonia.Controls;

namespace Avalonia.Automation.Peers;

public class ContentPageAutomationPeer : ControlAutomationPeer
{
    public ContentPageAutomationPeer(ContentPage owner)
        : base(owner)
    {
    }

    public new ContentPage Owner => (ContentPage)base.Owner;

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
