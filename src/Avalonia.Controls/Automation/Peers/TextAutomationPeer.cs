using Avalonia.Automation.Platform;
using Avalonia.Controls;

#nullable enable

namespace Avalonia.Automation.Peers
{
    public class TextAutomationPeer : ControlAutomationPeer
    {
        public TextAutomationPeer(IAutomationNodeFactory factory, Control owner)
            : base(factory, owner)
        {
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Text;
        }

        protected override string? GetNameCore() => Owner.GetValue(TextBlock.TextProperty);

        protected override bool IsControlElementCore()
        {
            // Return false if the control is part of a control template.
            return Owner.TemplatedParent is null && base.IsControlElementCore();
        }
    }
}
