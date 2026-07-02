using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests.Automation
{
    public class AutomationFocusTargetTests : ScopedTestBase
    {
        private class RedirectingPeer : ControlAutomationPeer
        {
            public RedirectingPeer(Control owner, AutomationPeer? target)
                : base(owner)
            {
                Target = target;
            }

            public AutomationPeer? Target { get; set; }

            protected override AutomationPeer? GetFocusTargetCore() => Target;
        }

        [Fact]
        public void GetFocusTarget_Defaults_To_Self()
        {
            var peer = new RedirectingPeer(new Border(), null);

            Assert.Same(peer, peer.GetFocusTarget());
        }

        [Fact]
        public void GetFocusTarget_Follows_Redirection_Transitively()
        {
            var inner = new RedirectingPeer(new Border(), null);
            var middle = new RedirectingPeer(new Border(), inner);
            var outer = new RedirectingPeer(new Border(), middle);

            Assert.Same(inner, outer.GetFocusTarget());
        }

        [Fact]
        public void GetFocusTarget_Terminates_On_A_Cycle()
        {
            var a = new RedirectingPeer(new Border(), null);
            var b = new RedirectingPeer(new Border(), a);
            a.Target = b;

            Assert.NotNull(a.GetFocusTarget());
        }
    }
}
