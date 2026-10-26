using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls.Primitives;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests.Automation;

public class RadioButtonAutomationPeerTests : ScopedTestBase
{
    [Fact]
    public void Select_Preserves_Style_Binding()
    {
        var target = new RadioButton();
        var peer = (ISelectionItemProvider)ControlAutomationPeer.CreatePeerForElement(target);

        AutomationPeerAssert.PreservesStyleBinding(target, ToggleButton.IsCheckedProperty, false, true, peer.Select);
    }
}
