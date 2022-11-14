using Avalonia.Automation.Provider;
using Avalonia.Controls;

namespace Avalonia.Automation.Peers
{
    public class ButtonAutomationPeer : ContentControlAutomationPeer,
        IInvokeProvider
    {
        public ButtonAutomationPeer(Button owner)
            : base(owner) 
        {
        }

        public new Button Owner => (Button)base.Owner;

        public void Invoke()
        {
            EnsureEnabled();
            (Owner as Button)?.PerformClick();
        }

        protected override string? GetAcceleratorKeyCore()
        {
            var result = base.GetAcceleratorKeyCore();

            if (string.IsNullOrWhiteSpace(result))
            {
                result = Owner.HotKey?.ToString();
            }

            return result;
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Button;
        }

        protected override bool IsContentElementCore() => true;
        protected override bool IsControlElementCore() => true;
    }
}

