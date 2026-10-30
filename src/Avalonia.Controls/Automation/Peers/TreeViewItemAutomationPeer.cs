using System;
using Avalonia.Automation.Provider;
using Avalonia.Controls;

namespace Avalonia.Automation.Peers;

public class TreeViewItemAutomationPeer : ItemsControlAutomationPeer,
    IExpandCollapseProvider,
    ISelectionItemProvider
{
    private ExpandCollapseState _lastState;

    public TreeViewItemAutomationPeer(TreeViewItem owner)
        : base(owner)
    {
        _lastState = ToState(owner.IsExpanded, owner.ItemCount);
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
            item.SetCurrentValue(TreeViewItem.IsSelectedProperty, true);
        }
    }

    void ISelectionItemProvider.AddToSelection()
    {
        EnsureEnabled();

        if (Owner is TreeViewItem item)
            item.SetCurrentValue(TreeViewItem.IsSelectedProperty, true);
    }

    void ISelectionItemProvider.RemoveFromSelection()
    {
        EnsureEnabled();

        if (Owner is TreeViewItem item)
            item.SetCurrentValue(TreeViewItem.IsSelectedProperty, false);
    }

    protected override AutomationControlType GetAutomationControlTypeCore()
    {
        return AutomationControlType.TreeItem;
    }

    private void OwnerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != TreeViewItem.IsExpandedProperty && e.Property != ItemsControl.ItemCountProperty)
            return;

        // Compare against the last reported state rather than the event's old/new values: the
        // Expanded/Collapsed routed events are raised before this handler runs, and a handler
        // there may change ItemCount, causing reentrant notifications.
        var oldState = _lastState;
        var newState = ExpandCollapseState;

        if (oldState != newState)
        {
            _lastState = newState;
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