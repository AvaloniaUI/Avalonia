using System;
using System.Collections.Generic;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;

namespace Avalonia.Automation.Peers;

public class TreeViewAutomationPeer : ItemsControlAutomationPeer, ISelectionProvider
{
    private List<TreeViewItem>? _toggledRows;

    public TreeViewAutomationPeer(TreeView owner)
        : base(owner)
    {
        owner.AddHandler(SelectingItemsControl.SelectionChangedEvent, OwnerSelectionChanged, handledEventsToo: true);
        owner.AddHandler(TreeViewItem.ExpandedEvent, OwnerRowToggled, handledEventsToo: true);
        owner.AddHandler(TreeViewItem.CollapsedEvent, OwnerRowToggled, handledEventsToo: true);
    }

    private new TreeView Owner => (TreeView)base.Owner;

    public bool CanSelectMultiple => Owner.SelectionMode.HasAllFlags(SelectionMode.Multiple);

    public bool IsSelectionRequired => false;

    public IReadOnlyList<AutomationPeer> GetSelection()
    {
        var selectedItems = Owner.SelectedItems;
        List<AutomationPeer>? result = null;

        if (selectedItems.Count == 1)
        {
            if (selectedItems[0] is { } item &&
                Owner.TreeContainerFromItem(item) is { IsEffectivelyVisible: true } container)
            {
                result = new List<AutomationPeer> { GetOrCreate(container) };
            }
        }
        else if (selectedItems.Count > 1)
        {
            // Walk the tree once: a lookup per item would be quadratic.
            foreach (var container in Owner.GetRealizedTreeContainers())
            {
                if (SelectingItemsControl.GetIsSelected(container) && container.IsEffectivelyVisible)
                {
                    result ??= new List<AutomationPeer>();
                    result.Add(GetOrCreate(container));
                }
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
            RaiseSelectionChanged();
        }
    }

    private void OwnerRowToggled(object? sender, RoutedEventArgs e)
    {
        // Child containers of a first-time expanded row only exist after the next layout pass.
        if (e.Source is not TreeViewItem row || row.TreeViewOwner != Owner || Owner.SelectedItems.Count == 0)
            return;

        if (_toggledRows is null)
        {
            _toggledRows = new List<TreeViewItem>();
            Owner.LayoutUpdated += OwnerLayoutUpdated;
        }

        _toggledRows.Add(row);
    }

    private void OwnerLayoutUpdated(object? sender, EventArgs e)
    {
        Owner.LayoutUpdated -= OwnerLayoutUpdated;

        var rows = _toggledRows;
        _toggledRows = null;

        if (rows is null)
            return;

        foreach (var row in rows)
        {
            if (HasSelectedDescendant(row))
            {
                RaiseSelectionChanged();
                return;
            }
        }
    }

    private void RaiseSelectionChanged()
    {
        RaisePropertyChangedEvent(SelectionPatternIdentifiers.SelectionProperty, null, null);
    }

    private static bool HasSelectedDescendant(ItemsControl parent)
    {
        foreach (var child in parent.GetRealizedContainers())
        {
            if (SelectingItemsControl.GetIsSelected(child) ||
                child is ItemsControl childItems && HasSelectedDescendant(childItems))
            {
                return true;
            }
        }

        return false;
    }
}
