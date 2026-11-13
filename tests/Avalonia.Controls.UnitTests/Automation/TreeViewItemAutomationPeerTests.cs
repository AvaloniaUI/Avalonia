using System;
using System.Collections.Generic;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests.Automation;

public class TreeViewItemAutomationPeerTests : ScopedTestBase
{
    [Fact]
    public void ExpandCollapseState_Is_LeafNode_For_Item_Without_Children()
    {
        var provider = GetProvider(new TreeViewItem { IsExpanded = true });

        Assert.Equal(ExpandCollapseState.LeafNode, provider.ExpandCollapseState);
        Assert.False(provider.ShowsMenu);
    }

    [Fact]
    public void ExpandCollapseState_Reflects_IsExpanded()
    {
        var item = CreateParentItem();
        var provider = GetProvider(item);

        Assert.Equal(ExpandCollapseState.Collapsed, provider.ExpandCollapseState);

        item.IsExpanded = true;
        Assert.Equal(ExpandCollapseState.Expanded, provider.ExpandCollapseState);

        item.IsExpanded = false;
        Assert.Equal(ExpandCollapseState.Collapsed, provider.ExpandCollapseState);
    }

    [Fact]
    public void Expand_And_Collapse_Set_IsExpanded()
    {
        var item = CreateParentItem();
        var provider = GetProvider(item);

        provider.Expand();
        Assert.True(item.IsExpanded);

        provider.Collapse();
        Assert.False(item.IsExpanded);
    }

    [Fact]
    public void Expand_Throws_For_Leaf_Item()
    {
        var item = new TreeViewItem();
        var provider = GetProvider(item);

        Assert.Throws<InvalidOperationException>(() => provider.Expand());
        Assert.False(item.IsExpanded);
    }

    [Fact]
    public void Collapse_Throws_For_Leaf_Item()
    {
        var provider = GetProvider(new TreeViewItem());

        Assert.Throws<InvalidOperationException>(() => provider.Collapse());
    }

    [Fact]
    public void Expand_Throws_When_Disabled()
    {
        var item = CreateParentItem();
        item.IsEnabled = false;
        var provider = GetProvider(item);

        Assert.Throws<ElementNotEnabledException>(() => provider.Expand());
        Assert.False(item.IsExpanded);
    }

    [Fact]
    public void Collapse_Throws_When_Disabled()
    {
        var item = CreateParentItem();
        item.IsEnabled = false;
        item.IsExpanded = true;
        var provider = GetProvider(item);

        Assert.Throws<ElementNotEnabledException>(() => provider.Collapse());
        Assert.True(item.IsExpanded);
    }

    [Fact]
    public void ExpandCollapse_Raises_PropertyChanged()
    {
        var item = CreateParentItem();
        var provider = GetProvider(item);
        var raised = CollectStateChanges(item);

        provider.Expand();
        provider.Collapse();

        Assert.Equal(2, raised.Count);
        Assert.Equal(ExpandCollapseState.Collapsed, raised[0].OldValue);
        Assert.Equal(ExpandCollapseState.Expanded, raised[0].NewValue);
        Assert.Equal(ExpandCollapseState.Expanded, raised[1].OldValue);
        Assert.Equal(ExpandCollapseState.Collapsed, raised[1].NewValue);
    }

    [Fact]
    public void Adding_And_Removing_Children_Raises_PropertyChanged()
    {
        var item = new TreeViewItem { IsExpanded = true };
        var raised = CollectStateChanges(item);

        item.Items.Add(new TreeViewItem());
        item.Items.Add(new TreeViewItem());
        item.Items.Clear();

        Assert.Equal(2, raised.Count);
        Assert.Equal(ExpandCollapseState.LeafNode, raised[0].OldValue);
        Assert.Equal(ExpandCollapseState.Expanded, raised[0].NewValue);
        Assert.Equal(ExpandCollapseState.Expanded, raised[1].OldValue);
        Assert.Equal(ExpandCollapseState.LeafNode, raised[1].NewValue);
    }

    [Fact]
    public void Expanding_Leaf_Item_Does_Not_Raise_PropertyChanged()
    {
        var item = new TreeViewItem();
        var raised = CollectStateChanges(item);

        item.IsExpanded = true;

        Assert.Empty(raised);
    }

    [Fact]
    public void Populating_Children_In_Expanded_Handler_Raises_Single_PropertyChanged()
    {
        var item = new TreeViewItem();
        var raised = CollectStateChanges(item);

        item.AddHandler(TreeViewItem.ExpandedEvent, (_, _) => item.Items.Add(new TreeViewItem()));
        item.IsExpanded = true;

        var e = Assert.Single(raised);
        Assert.Equal(ExpandCollapseState.LeafNode, e.OldValue);
        Assert.Equal(ExpandCollapseState.Expanded, e.NewValue);
    }

    [Fact]
    public void Select_Preserves_Style_Binding()
    {
        var target = new TreeViewItem();
        var peer = (ISelectionItemProvider)ControlAutomationPeer.CreatePeerForElement(target);

        AutomationPeerAssert.PreservesStyleBinding(target, TreeViewItem.IsSelectedProperty, false, true, peer.Select);
    }

    [Fact]
    public void AddToSelection_Preserves_Style_Binding()
    {
        var target = new TreeViewItem();
        var peer = (ISelectionItemProvider)ControlAutomationPeer.CreatePeerForElement(target);

        AutomationPeerAssert.PreservesStyleBinding(target, TreeViewItem.IsSelectedProperty, false, true, peer.AddToSelection);
    }

    [Fact]
    public void RemoveFromSelection_Preserves_Style_Binding()
    {
        var target = new TreeViewItem();
        var peer = (ISelectionItemProvider)ControlAutomationPeer.CreatePeerForElement(target);

        AutomationPeerAssert.PreservesStyleBinding(target, TreeViewItem.IsSelectedProperty, true, false, peer.RemoveFromSelection);
    }

    private static TreeViewItem CreateParentItem() => new() { Items = { new TreeViewItem() } };

    private static List<AutomationPropertyChangedEventArgs> CollectStateChanges(TreeViewItem item)
    {
        var peer = ControlAutomationPeer.CreatePeerForElement(item);
        var result = new List<AutomationPropertyChangedEventArgs>();

        peer.PropertyChanged += (_, e) =>
        {
            if (e.Property == ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty)
                result.Add(e);
        };

        return result;
    }

    private static IExpandCollapseProvider GetProvider(TreeViewItem item)
    {
        var provider = ControlAutomationPeer.CreatePeerForElement(item).GetProvider<IExpandCollapseProvider>();
        Assert.NotNull(provider);
        return provider;
    }
}
