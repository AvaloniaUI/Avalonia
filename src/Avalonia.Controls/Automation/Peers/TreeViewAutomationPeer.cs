using Avalonia.Controls;

namespace Avalonia.Automation.Peers;

public class TreeViewAutomationPeer : ItemsControlAutomationPeer
{
    public TreeViewAutomationPeer(TreeView owner)
        : base(owner)
    {
    }

    protected override AutomationControlType GetAutomationControlTypeCore()
    {
        return AutomationControlType.Tree;
    }
}
