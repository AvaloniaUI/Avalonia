using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests.Automation;

public class ScrollViewerAutomationPeerTests : ScopedTestBase
{
    [Fact]
    public void SetScrollPercent_Preserves_Style_Binding()
    {
        var target = new ScrollViewer
        {
            Extent = new Size(100, 1000),
            Viewport = new Size(100, 100),
        };
        var peer = (IScrollProvider)ControlAutomationPeer.CreatePeerForElement(target);

        AutomationPeerAssert.PreservesStyleBinding(
            target,
            ScrollViewer.OffsetProperty,
            new Vector(0, 100),
            new Vector(0, 450),
            () => peer.SetScrollPercent(ScrollPatternIdentifiers.NoScroll, 50));
    }
}
