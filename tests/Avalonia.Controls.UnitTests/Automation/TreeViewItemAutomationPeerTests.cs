using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests.Automation;

public class TreeViewItemAutomationPeerTests : ScopedTestBase
{
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
}
