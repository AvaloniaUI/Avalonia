using Avalonia.Automation.Provider;
using Avalonia.Controls.Automation;
using Avalonia.Controls.Primitives;

namespace Avalonia.Automation.Peers
{
    public class ToggleButtonAutomationPeer : ContentControlAutomationPeer, IToggleProvider
    {
        public ToggleButtonAutomationPeer(ToggleButton owner)
            : base(owner)
        {
            Owner.PropertyChanged += (a, e) =>
            {
                if (e.Property == ToggleButton.IsCheckedProperty)
                {
                    RaisePropertyChangedEvent(
                        TogglePatternIdentifiers.ToggleStateProperty,
                        ToState((bool?)e.OldValue),
                        ToState((bool?)e.NewValue));
                }
            };
        }

        public new ToggleButton Owner => (ToggleButton)base.Owner;

        private ToggleState ToState(bool? value) => value switch
        {
            true => ToggleState.On,
            false => ToggleState.Off,
            null => ToggleState.Indeterminate,
        };

        ToggleState IToggleProvider.ToggleState => ToState(Owner.IsChecked);

        void IToggleProvider.Toggle()
        {
            EnsureEnabled();
            Owner.PerformClick();
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Button;
        }

        protected override bool IsContentElementCore() => true;
        protected override bool IsControlElementCore() => true;
    }
}
