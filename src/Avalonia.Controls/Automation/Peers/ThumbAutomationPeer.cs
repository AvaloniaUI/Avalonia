using Avalonia.Automation.Peers;
using Avalonia.Controls.Primitives;

namespace Avalonia.Controls.Automation.Peers
{
    public class ThumbAutomationPeer : ControlAutomationPeer
    {
        public ThumbAutomationPeer(Thumb owner) : base(owner) { }
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Thumb;
        protected override bool IsContentElementCore() => false;
    }
}
