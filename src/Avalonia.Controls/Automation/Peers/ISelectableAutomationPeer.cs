namespace Avalonia.Controls.Automation.Peers
{
    public interface ISelectableAutomationPeer
    {
        bool GetIsSelected();
        ISelectingAutomationPeer GetSelectionContainer();
    }
}
