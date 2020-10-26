#nullable enable

namespace Avalonia.Controls.Automation.Peers
{
    public class TextAutomationPeer : ControlAutomationPeer
    {
        public TextAutomationPeer(Control owner) : base(owner) { }
        protected override string? GetNameCore() => Owner.GetValue(TextBlock.TextProperty);
    }
}
