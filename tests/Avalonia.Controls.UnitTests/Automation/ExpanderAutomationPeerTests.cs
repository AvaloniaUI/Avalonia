using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests.Automation;

public class ExpanderAutomationPeerTests : ScopedTestBase
{
    [Fact]
    public void Expand_Preserves_Style_Binding()
    {
        var target = new Expander();
        var peer = (IExpandCollapseProvider)ControlAutomationPeer.CreatePeerForElement(target);

        AutomationPeerAssert.PreservesStyleBinding(target, Expander.IsExpandedProperty, false, true, peer.Expand);
    }

    [Fact]
    public void Collapse_Preserves_Style_Binding()
    {
        var target = new Expander();
        var peer = (IExpandCollapseProvider)ControlAutomationPeer.CreatePeerForElement(target);

        AutomationPeerAssert.PreservesStyleBinding(target, Expander.IsExpandedProperty, true, false, peer.Collapse);
    }
}
