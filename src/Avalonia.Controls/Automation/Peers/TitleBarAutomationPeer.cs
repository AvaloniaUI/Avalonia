using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls.Chrome;

namespace Avalonia.Controls.Automation.Peers;

internal class TitleBarAutomationPeer : ControlAutomationPeer
{
    public TitleBarAutomationPeer(TitleBar owner) : base(owner)
    {
    }

    protected override bool IsContentElementCore() => true;

    protected override string GetClassNameCore()
    {
        return "TitleBar";
    }

    protected override string? GetAutomationIdCore() => base.GetAutomationIdCore() ?? "AvaloniaTitleBar";

    protected override AutomationControlType GetAutomationControlTypeCore()
    {
        return AutomationControlType.TitleBar;
    }
}
