using Avalonia.Automation.Provider;
using Avalonia.Controls;

namespace Avalonia.Automation.Peers;

public class TreeViewItemAutomationPeer : ItemsControlAutomationPeer,
    ISelectionItemProvider
{
    public TreeViewItemAutomationPeer(TreeViewItem owner)
        : base(owner)
    {
    }

    public bool IsSelected => Owner.GetValue(TreeViewItem.IsSelectedProperty);

    public ISelectionProvider? SelectionContainer
    {
        get
        {
            if (Owner is TreeViewItem { TreeViewOwner: { } treeView })
            {
                var parentPeer = GetOrCreate(treeView);
                return parentPeer.GetProvider<ISelectionProvider>();
            }

            return null;
        }
    }

    public void Select()
    {
        EnsureEnabled();

        if (Owner is TreeViewItem item)
        {
            item.TreeViewOwner?.SelectedItems.Clear();
            item.IsSelected = true;
        }
    }

    void ISelectionItemProvider.AddToSelection()
    {
        EnsureEnabled();

        if (Owner is TreeViewItem item)
            item.IsSelected = true;
    }

    void ISelectionItemProvider.RemoveFromSelection()
    {
        EnsureEnabled();

        if (Owner is TreeViewItem item)
            item.IsSelected = false;
    }

    protected override AutomationControlType GetAutomationControlTypeCore()
    {
        return AutomationControlType.TreeItem;
    }
}
