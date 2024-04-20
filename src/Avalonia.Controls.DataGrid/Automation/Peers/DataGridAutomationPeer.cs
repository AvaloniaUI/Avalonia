using Avalonia.Automation.Peers;

namespace Avalonia.Controls.Automation.Peers;

public class DataGridAutomationPeer : ControlAutomationPeer
{
    public DataGridAutomationPeer(DataGrid owner)
        : base(owner)
    {
    }

    public new DataGrid Owner => (DataGrid)base.Owner;

    protected override AutomationControlType GetAutomationControlTypeCore()
    {
        return AutomationControlType.DataGrid;
    }
}
