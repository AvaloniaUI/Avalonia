using System;
using System.Collections.Generic;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace Avalonia.Automation.Peers;

public class TreeViewAutomationPeer : ItemsControlAutomationPeer, ISelectionProvider
{
    public TreeViewAutomationPeer(TreeView owner)
        : base(owner)
    {
        owner.AddHandler(SelectingItemsControl.SelectionChangedEvent, OwnerSelectionChanged, handledEventsToo: true);
    }

    private TreeView Tree => (TreeView)Owner;

    public bool CanSelectMultiple => Tree.SelectionMode.HasAllFlags(SelectionMode.Multiple);

    public bool IsSelectionRequired => false;

    public IReadOnlyList<AutomationPeer> GetSelection()
    {
        List<AutomationPeer>? result = null;

        foreach (var item in Tree.SelectedItems)
        {
            if (item is not null && Tree.TreeContainerFromItem(item) is { } container)
            {
                result ??= new List<AutomationPeer>();
                result.Add(GetOrCreate(container));
            }
        }

        return result is not null ? result : Array.Empty<AutomationPeer>();
    }

    protected override AutomationControlType GetAutomationControlTypeCore()
    {
        return AutomationControlType.Tree;
    }

    private void OwnerSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.Source == Owner)
        {
            RaisePropertyChangedEvent(SelectionPatternIdentifiers.SelectionProperty, null, null);
        }
    }
}
