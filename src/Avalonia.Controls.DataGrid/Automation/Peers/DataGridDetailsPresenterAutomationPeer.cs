using Avalonia.Automation.Peers;
using Avalonia.Controls.Primitives;

namespace Avalonia.Controls.Automation.Peers;

public class DataGridDetailsPresenterAutomationPeer : ControlAutomationPeer
{
    public DataGridDetailsPresenterAutomationPeer(DataGridDetailsPresenter owner)
        : base(owner)
    {
    }

    public new DataGridDetailsPresenter Owner => (DataGridDetailsPresenter)base.Owner;
}
