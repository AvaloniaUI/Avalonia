using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests.Automation;

public class TabControlAutomationPeerTests : ScopedTestBase
{
    [Fact]
    public void TabControl_Peer_Is_A_Tab_With_Selection_Pattern()
    {
        var tabControl = new TabControl();
        var peer = ControlAutomationPeer.CreatePeerForElement(tabControl);

        Assert.Equal(AutomationControlType.Tab, peer.GetAutomationControlType());
        var selection = Assert.IsAssignableFrom<ISelectionProvider>(peer);
        Assert.False(selection.CanSelectMultiple);
        Assert.True(selection.IsSelectionRequired);
    }
}
