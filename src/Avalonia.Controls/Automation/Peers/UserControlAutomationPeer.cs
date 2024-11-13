using Avalonia.Automation;
using Avalonia.Automation.Peers;

namespace Avalonia.Controls.Automation.Peers;

public class UserControlAutomationPeer : ControlAutomationPeer
{
    public UserControlAutomationPeer(UserControl owner)
        : base(owner)
    {
    }
    
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Custom;
}
