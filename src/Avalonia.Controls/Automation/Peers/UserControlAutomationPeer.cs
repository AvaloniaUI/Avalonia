using Avalonia.Automation;
using Avalonia.Automation.Peers;

namespace Avalonia.Controls.Automation.Peers;

public class UserControlAutomationPeer : ContentControlAutomationPeer
{
    public UserControlAutomationPeer(UserControl owner)
        : base(owner)
    {
    }
    
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Custom;

    protected override string? GetNameCore()
    {
        var result = AutomationProperties.GetName(Owner);

        if (string.IsNullOrWhiteSpace(result) && GetLabeledBy() is AutomationPeer labeledBy)
        {
            result = labeledBy.GetName();
        }

        return result;
    }
}
