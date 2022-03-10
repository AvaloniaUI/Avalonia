using Avalonia.Controls;

namespace Avalonia.Automation.Peers
{
    public class TextBlockAutomationPeer : ControlAutomationPeer
    {
        public TextBlockAutomationPeer(TextBlock owner)
            : base(owner)
        {
        }

        public new TextBlock Owner => (TextBlock)base.Owner;

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Text;
        }

        protected override string? GetNameCore() => Owner.Text;

        protected override bool IsControlElementCore()
        {
            // Return false if the control is part of a control template.
            return Owner.TemplatedParent is null && base.IsControlElementCore();
        }
    }
}
