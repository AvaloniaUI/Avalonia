using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls.Primitives;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests.Automation;

public class ScrollBarAutomationPeerTests : ScopedTestBase
{
    [Fact]
    public void SetValue_Preserves_Style_Binding()
    {
        var target = new ScrollBar();
        var peer = (IRangeValueProvider)ControlAutomationPeer.CreatePeerForElement(target);

        AutomationPeerAssert.PreservesStyleBinding(target, RangeBase.ValueProperty, 10.0, 42.0, () => peer.SetValue(42));
    }
}
