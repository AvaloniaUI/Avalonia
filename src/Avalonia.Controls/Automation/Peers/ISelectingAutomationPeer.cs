using System.Collections.Generic;

namespace Avalonia.Controls.Automation.Peers
{
    public interface ISelectingAutomationPeer
    {
        SelectionMode GetSelectionMode();
        IReadOnlyList<AutomationPeer> GetSelection();
    }
}
