using Avalonia.Controls;

namespace Avalonia.Automation.Peers
{
    public class TextBlockAutomationPeer : ControlAutomationPeer
    {
        public TextBlockAutomationPeer(TextBlock owner)
            : base(owner)
        {
            Owner.PropertyChanged += (a, e) =>
            {
                // The text is only the name when no explicit name is set.
                if (e.Property == TextBlock.TextProperty && string.IsNullOrWhiteSpace(base.GetNameCore()))
                {
                    RaisePropertyChangedEvent(
                        AutomationElementIdentifiers.NameProperty,
                        e.OldValue,
                        e.NewValue);
                }
            };
        }

        public new TextBlock Owner => (TextBlock)base.Owner;

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Text;
        }

        protected override string? GetNameCore()
        {
            var result = base.GetNameCore();
            return string.IsNullOrWhiteSpace(result) ? Owner.Inlines?.Text ?? Owner.Text : result;
        }

        protected override bool IsControlElementCore()
        {
            // Return false if the control is part of a control template.
            return Owner.TemplatedParent is null && base.IsControlElementCore();
        }
    }
}
