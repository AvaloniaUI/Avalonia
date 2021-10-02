using Avalonia.Controls;

#nullable enable

namespace Avalonia.Automation.Peers
{
    public class MenuAutomationPeer : ControlAutomationPeer
    {
        public MenuAutomationPeer(Menu owner)
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
