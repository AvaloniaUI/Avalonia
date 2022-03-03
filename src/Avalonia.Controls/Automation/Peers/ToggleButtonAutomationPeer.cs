using Avalonia.Automation.Provider;
using Avalonia.Controls.Primitives;

namespace Avalonia.Automation.Peers
{
    public class ToggleButtonAutomationPeer : ContentControlAutomationPeer, IToggleProvider
    {
        public ToggleButtonAutomationPeer(ToggleButton owner)
            : base(owner)
        {
        }

        public new ToggleButton Owner => (ToggleButton)base.Owner;

        ToggleState IToggleProvider.ToggleState
        {
            get => Owner.IsChecked switch
            {
                true => ToggleState.On,
                false => ToggleState.Off,
                null => ToggleState.Indeterminate,
            };
        }

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
