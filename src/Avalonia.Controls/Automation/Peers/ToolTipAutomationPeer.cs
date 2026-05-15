using Avalonia.Automation.Provider;
using Avalonia.Controls;

namespace Avalonia.Automation.Peers
{
    public class ToolTipAutomationPeer(ToolTip owner) : ControlAutomationPeer(owner)
    {
        public new ToolTip Owner => (ToolTip)base.Owner;

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ToolTip;

        protected override string GetClassNameCore() => "ToolTip";
    }
}
