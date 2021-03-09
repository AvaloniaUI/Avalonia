using Avalonia.Automation.Platform;
using Avalonia.Automation.Provider;
using Avalonia.Controls;

#nullable enable

namespace Avalonia.Automation.Peers
{
    public class ButtonAutomationPeer : ContentControlAutomationPeer,
        IInvokeProvider
    {
        public ButtonAutomationPeer(IAutomationNodeFactory factory,  Button owner)
            : base(factory, owner) 
        {
        }
        
        public void Invoke()
        {
            EnsureEnabled();
            (Owner as Button)?.PerformClick();
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Button;
        }

        protected override bool IsContentElementCore() => true;
        protected override bool IsControlElementCore() => true;
    }
}

