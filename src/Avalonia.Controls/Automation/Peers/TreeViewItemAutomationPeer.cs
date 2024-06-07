using Avalonia.Controls;

namespace Avalonia.Automation.Peers;

public class TreeViewItemAutomationPeer : ItemsControlAutomationPeer
{
    public TreeViewItemAutomationPeer(TreeViewItem owner)
        : base(owner)
    {
    }
   
    protected override AutomationControlType GetAutomationControlTypeCore()
    {
        return AutomationControlType.TreeItem;
    }
}
