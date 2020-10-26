#nullable enable

namespace Avalonia.Controls.Automation.Peers
{
    public class WindowAutomationPeer : ControlAutomationPeer
    {
        public WindowAutomationPeer(Control owner): base(owner) { }

        protected override string GetNameCore() => Owner.GetValue(Window.TitleProperty);
        protected override AutomationPeer? GetParentCore() => null;
    }
}


