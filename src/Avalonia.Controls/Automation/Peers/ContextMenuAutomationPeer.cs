using Avalonia.Controls;

#nullable enable

namespace Avalonia.Automation.Peers
{
    public class ContextMenuAutomationPeer : ControlAutomationPeer
    {
        public ContextMenuAutomationPeer(ContextMenu owner)
            : base(owner) 
        { 
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Menu;
        }

        protected override bool IsContentElementCore() => false;
    }
}
