using System;
using Avalonia.Automation.Provider;
using Avalonia.Controls;

namespace Avalonia.Automation.Peers;

public class TreeViewItemAutomationPeer : ItemsControlAutomationPeer,
    IExpandCollapseProvider,
    ISelectionItemProvider
{
    public TreeViewItemAutomationPeer(TreeViewItem owner)
        : base(owner)
    {
        owner.PropertyChanged += OwnerPropertyChanged;
    }

    public new TreeViewItem Owner => (TreeViewItem)base.Owner;

    public ExpandCollapseState ExpandCollapseState => ToState(Owner.IsExpanded, Owner.ItemCount);

    public bool ShowsMenu => false;

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

    public void Expand()
    {
        EnsureEnabled();
        if (Owner.ItemCount == 0)
            throw new InvalidOperationException();
        Owner.SetCurrentValue(TreeViewItem.IsExpandedProperty, true);
    }

    public void Collapse()
    {
        EnsureEnabled();
        if (Owner.ItemCount == 0)
            throw new InvalidOperationException();
        Owner.SetCurrentValue(TreeViewItem.IsExpandedProperty, false);
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

    private void OwnerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        ExpandCollapseState oldState, newState;

        if (e.Property == TreeViewItem.IsExpandedProperty)
        {
            oldState = ToState(e.GetOldValue<bool>(), Owner.ItemCount);
            newState = ToState(e.GetNewValue<bool>(), Owner.ItemCount);
        }
        else if (e.Property == ItemsControl.ItemCountProperty)
        {
            oldState = ToState(Owner.IsExpanded, e.GetOldValue<int>());
            newState = ToState(Owner.IsExpanded, e.GetNewValue<int>());
        }
        else
        {
            return;
        }

        if (oldState != newState)
        {
            RaisePropertyChangedEvent(
                ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty,
                oldState,
                newState);
        }
    }

    private static ExpandCollapseState ToState(bool isExpanded, int itemCount)
    {
        if (itemCount == 0)
            return ExpandCollapseState.LeafNode;
        return isExpanded ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;
    }
}