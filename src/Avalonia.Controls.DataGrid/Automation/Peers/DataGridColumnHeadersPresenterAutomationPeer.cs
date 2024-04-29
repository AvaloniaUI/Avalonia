using Avalonia.Automation.Peers;
using Avalonia.Controls.Primitives;

namespace Avalonia.Controls.Automation.Peers;

public class DataGridColumnHeadersPresenterAutomationPeer : ControlAutomationPeer
{
    public DataGridColumnHeadersPresenterAutomationPeer(DataGridColumnHeadersPresenter owner)
        : base(owner)
    {
    }

    public new DataGridColumnHeadersPresenter Owner => (DataGridColumnHeadersPresenter)base.Owner;

    protected override AutomationControlType GetAutomationControlTypeCore()
    {
        return AutomationControlType.Header;
    }

    protected override bool IsContentElementCore() => false;

    protected override bool IsControlElementCore() => true;
}
